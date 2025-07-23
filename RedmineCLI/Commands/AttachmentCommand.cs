using System.CommandLine;
using System.IO.Abstractions;
using RedmineCLI.ApiClient;
using RedmineCLI.Exceptions;
using RedmineCLI.Formatters;
using RedmineCLI.Services;
using Spectre.Console;

namespace RedmineCLI.Commands;

public class AttachmentCommand
{
    public Command CreateCommand(
        IConfigService configService,
        IRedmineApiClient apiClient,
        ITableFormatter tableFormatter,
        IJsonFormatter jsonFormatter,
        IFileSystem? fileSystem = null,
        IAnsiConsole? console = null)
    {
        var attachmentCommand = new Command("attachment", "Manage attachments");
        
        // attachment download <id>
        var downloadCommand = CreateDownloadCommand(configService, apiClient, fileSystem ?? new FileSystem(), console ?? AnsiConsole.Console);
        attachmentCommand.Add(downloadCommand);
        
        // attachment view <id>
        var viewCommand = CreateViewCommand(configService, apiClient, tableFormatter, jsonFormatter);
        attachmentCommand.Add(viewCommand);
        
        return attachmentCommand;
    }

    private Command CreateDownloadCommand(
        IConfigService configService,
        IRedmineApiClient apiClient,
        IFileSystem fileSystem,
        IAnsiConsole console)
    {
        var command = new Command("download", "Download an attachment");
        
        var idArg = new Argument<int>("id");
        idArg.Description = "Attachment ID";
        command.Add(idArg);
        
        var outputOption = new Option<string?>("--output") { Description = "Output directory path" };
        outputOption.Aliases.Add("-o");
        command.Add(outputOption);
        
        var forceOption = new Option<bool>("--force") { Description = "Overwrite existing file" };
        forceOption.Aliases.Add("-f");
        command.Add(forceOption);

        command.SetAction(async (parseResult) =>
        {
            var attachmentId = parseResult.GetValue(idArg);
            var outputPath = parseResult.GetValue(outputOption);
            var force = parseResult.GetValue(forceOption);
            
            try
            {
                var profile = await configService.GetActiveProfileAsync();
                if (profile == null)
                {
                    console.MarkupLine("[red]Error: No active profile. Run 'redmine auth login' first.[/]");
                    Environment.ExitCode = 1;
                    return;
                }

                // Get attachment metadata
                var attachment = await apiClient.GetAttachmentAsync(attachmentId);
                
                // Sanitize filename
                var sanitizedFilename = SanitizeFilename(attachment.Filename);
                
                // Determine output path
                string fullPath;
                if (!string.IsNullOrEmpty(outputPath))
                {
                    var directory = fileSystem.Path.GetFullPath(outputPath);
                    if (!fileSystem.Directory.Exists(directory))
                    {
                        console.MarkupLine($"[red]Error: Directory '{directory}' does not exist.[/]");
                        Environment.ExitCode = 1;
                    return;
                    }
                    fullPath = fileSystem.Path.Combine(directory, sanitizedFilename);
                }
                else
                {
                    fullPath = fileSystem.Path.Combine(fileSystem.Directory.GetCurrentDirectory(), sanitizedFilename);
                }
                
                // Check if file exists
                if (fileSystem.File.Exists(fullPath) && !force)
                {
                    console.MarkupLine($"[red]Error: File '{fullPath}' already exists. Use --force to overwrite.[/]");
                    Environment.ExitCode = 1;
                    return;
                }
                
                // Download with progress
                await console.Progress()
                    .StartAsync(async ctx =>
                    {
                        var task = ctx.AddTask($"Downloading {sanitizedFilename}", new ProgressTaskSettings
                        {
                            AutoStart = true
                        });
                        
                        try
                        {
                            // Download the file
                            using var stream = await apiClient.DownloadAttachmentAsync(attachmentId);
                            using var fileStream = fileSystem.File.Create(fullPath);
                            
                            // Copy with progress tracking if size is known
                            if (attachment.Filesize > 0)
                            {
                                task.MaxValue = attachment.Filesize;
                                var buffer = new byte[8192];
                                var totalRead = 0L;
                                int read;
                                
                                while ((read = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                                {
                                    await fileStream.WriteAsync(buffer, 0, read);
                                    totalRead += read;
                                    task.Value = totalRead;
                                }
                            }
                            else
                            {
                                // Size unknown, just copy
                                task.IsIndeterminate = true;
                                await stream.CopyToAsync(fileStream);
                            }
                            
                            task.Value = task.MaxValue;
                        }
                        catch (Exception)
                        {
                            task.StopTask();
                            throw;
                        }
                    });
                
                console.MarkupLine($"[green]✓[/] Downloaded to: {fullPath}");
            }
            catch (HttpRequestException ex)
            {
                console.MarkupLine($"[red]Error: {ex.Message}[/]");
                Environment.ExitCode = 1;
            }
            catch (Exception ex)
            {
                console.MarkupLine($"[red]Error: {ex.Message}[/]");
                Environment.ExitCode = 1;
            }
        });
        
        return command;
    }

    private Command CreateViewCommand(
        IConfigService configService,
        IRedmineApiClient apiClient,
        ITableFormatter tableFormatter,
        IJsonFormatter jsonFormatter)
    {
        var command = new Command("view", "View attachment metadata");
        
        var idArg = new Argument<int>("id");
        idArg.Description = "Attachment ID";
        command.Add(idArg);
        
        var jsonOption = new Option<bool>("--json") { Description = "Format output as JSON" };
        command.Add(jsonOption);

        command.SetAction(async (parseResult) =>
        {
            var attachmentId = parseResult.GetValue(idArg);
            var isJson = parseResult.GetValue(jsonOption);
            
            try
            {
                var profile = await configService.GetActiveProfileAsync();
                if (profile == null)
                {
                    AnsiConsole.MarkupLine("[red]Error: No active profile. Run 'redmine auth login' first.[/]");
                    Environment.ExitCode = 1;
                    return;
                }

                var attachment = await apiClient.GetAttachmentAsync(attachmentId);
                
                if (isJson)
                {
                    jsonFormatter.FormatAttachmentDetails(attachment);
                }
                else
                {
                    tableFormatter.FormatAttachmentDetails(attachment);
                }
            }
            catch (RedmineApiException ex) when (ex.StatusCode == 404)
            {
                AnsiConsole.MarkupLine($"[red]Error: Attachment #{attachmentId} not found[/]");
                Environment.ExitCode = 1;
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
                Environment.ExitCode = 1;
            }
        });
        
        return command;
    }

    private static string SanitizeFilename(string filename)
    {
        // First, extract just the filename from any path
        var baseName = Path.GetFileName(filename);
        
        // If GetFileName returns empty (e.g., for paths ending with separator), use the original
        if (string.IsNullOrEmpty(baseName))
        {
            baseName = filename;
        }
        
        // Remove invalid characters
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = string.Join("", baseName.Split(invalidChars));
        
        // Remove any remaining path traversal attempts
        sanitized = sanitized.Replace("..", "");
        
        // If filename is empty after sanitization, use a default
        if (string.IsNullOrWhiteSpace(sanitized))
        {
            sanitized = "attachment";
        }
        
        return sanitized;
    }
}