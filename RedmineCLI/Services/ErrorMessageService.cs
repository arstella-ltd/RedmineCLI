using System.Net;

using RedmineCLI.Exceptions;

namespace RedmineCLI.Services;

public interface IErrorMessageService
{
    (string message, string? suggestion) GetUserFriendlyMessage(Exception exception);
}

public class ErrorMessageService : IErrorMessageService
{
    public (string message, string? suggestion) GetUserFriendlyMessage(Exception exception)
    {
        return exception switch
        {
            RedmineApiException apiEx => GetApiErrorMessage(apiEx),
            HttpRequestException httpEx => GetHttpErrorMessage(httpEx),
            InvalidOperationException invOpEx when invOpEx.Message.Contains("API key") =>
                ("API key is not configured", "Run 'redmine auth login' to authenticate"),
            InvalidOperationException invOpEx when invOpEx.Message.Contains("No active profile") =>
                ("No active profile is configured", "Run 'redmine config set active_profile <profile-name>' to set a profile"),
            ArgumentException argEx when argEx.Message.Contains("Project ID") =>
                ("Invalid project ID specified", "Run 'redmine project list' to see available projects"),
            TaskCanceledException =>
                ("Request timed out", "Check your network connection and try again"),
            _ => (exception.Message, null)
        };
    }

    private (string message, string? suggestion) GetApiErrorMessage(RedmineApiException exception)
    {
        return exception.StatusCode switch
        {
            (int)HttpStatusCode.Unauthorized =>
                ("Authentication failed", "Run 'redmine auth login' to authenticate again"),
            (int)HttpStatusCode.Forbidden =>
                ("Access denied", "Check your API key permissions"),
            (int)HttpStatusCode.NotFound when exception.Message.Contains("project", StringComparison.OrdinalIgnoreCase) =>
                ("Project not found", $"The specified project does not exist.\nRun 'redmine project list' to see available projects"),
            (int)HttpStatusCode.NotFound when exception.Message.Contains("issue", StringComparison.OrdinalIgnoreCase) =>
                ("Issue not found", "Check the issue ID"),
            (int)HttpStatusCode.UnprocessableEntity =>
                ("Invalid data submitted", "Check your input data"),
            (int)HttpStatusCode.TooManyRequests =>
                ("API rate limit exceeded", "Wait a while and try again"),
            _ => ($"API error: {exception.Message}", exception.ApiError)
        };
    }

    private (string message, string? suggestion) GetHttpErrorMessage(HttpRequestException exception)
    {
        if (exception.InnerException is System.Net.Sockets.SocketException)
        {
            return ("Cannot connect to server", "Check your network connection and Redmine URL");
        }

        if (exception.Message.Contains("SSL", StringComparison.OrdinalIgnoreCase) ||
            exception.Message.Contains("HTTPS", StringComparison.OrdinalIgnoreCase))
        {
            return ("SSL/TLS connection error occurred", "Check your certificate settings");
        }

        return ("Network error occurred", "Check your network connection");
    }
}
