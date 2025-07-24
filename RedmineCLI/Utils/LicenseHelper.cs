using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

using RedmineCLI.Models;

namespace RedmineCLI.Utils;

public class LicenseHelper : ILicenseHelper
{
    private static Dictionary<string, LicenseData>? _cachedLicenses;
    private static LicenseInfo? _cachedVersionInfo;

    private static readonly Dictionary<string, LicenseData> _embeddedLicenses = new()
    {
        ["RedmineCLI"] = new LicenseData
        {
            Name = "RedmineCLI",
            Version = GetAssemblyVersion(),
            License = "MIT License\n\nCopyright (c) 2025 Arstella ltd.\n\nPermission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the \"Software\"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:\n\nThe above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.\n\nTHE SOFTWARE IS PROVIDED \"AS IS\", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.",
            ProjectUrl = "https://github.com/example/RedmineCLI"
        },
        ["System.CommandLine"] = new LicenseData
        {
            Name = "System.CommandLine",
            Version = "2.0.0-beta6",
            License = "MIT License\n\nCopyright (c) .NET Foundation and Contributors\n\nPermission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the \"Software\"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:\n\nThe above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.\n\nTHE SOFTWARE IS PROVIDED \"AS IS\", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.",
            ProjectUrl = "https://github.com/dotnet/command-line-api"
        },
        ["Spectre.Console"] = new LicenseData
        {
            Name = "Spectre.Console",
            Version = "0.50.0",
            License = "MIT License\n\nCopyright © Patrik Svensson, Phil Scott, Nils Andresen, Cédric Luthi, Frank Ray\n\nPermission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the \"Software\"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:\n\nThe above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.\n\nTHE SOFTWARE IS PROVIDED \"AS IS\", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.",
            ProjectUrl = "https://github.com/spectreconsole/spectre.console"
        },
        ["VYaml"] = new LicenseData
        {
            Name = "VYaml",
            Version = "1.2.0",
            License = "MIT License\n\nCopyright (c) hadashiA\n\nPermission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the \"Software\"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:\n\nThe above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.\n\nTHE SOFTWARE IS PROVIDED \"AS IS\", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.",
            ProjectUrl = "https://github.com/hadashiA/VYaml"
        },
        ["StbImageSharp"] = new LicenseData
        {
            Name = "StbImageSharp",
            Version = "2.30.15",
            License = "Public Domain\n\nThis is free and unencumbered software released into the public domain.\n\nAnyone is free to copy, modify, publish, use, compile, sell, or distribute this software, either in source code form or as a compiled binary, for any purpose, commercial or non-commercial, and by any means.\n\nIn jurisdictions that recognize copyright laws, the author or authors of this software dedicate any and all copyright interest in the software to the public domain. We make this dedication for the benefit of the public at large and to the detriment of our heirs and successors. We intend this dedication to be an overt act of relinquishment in perpetuity of all present and future rights to this software under copyright law.\n\nTHE SOFTWARE IS PROVIDED \"AS IS\", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.",
            ProjectUrl = "https://github.com/StbSharp/StbImageSharp"
        }
    };

    public Task<Dictionary<string, LicenseData>> GetLicenseInfoAsync()
    {
        _cachedLicenses ??= new Dictionary<string, LicenseData>(_embeddedLicenses);
        return Task.FromResult(_cachedLicenses);
    }

    public Task<LicenseInfo> GetVersionInfoAsync()
    {
        if (_cachedVersionInfo == null)
        {
            _cachedVersionInfo = new LicenseInfo
            {
                Name = "RedmineCLI",
                Version = GetAssemblyVersion(),
                License = "MIT",
                Dependencies = GetDetectedDependencies()
            };
        }

        return Task.FromResult(_cachedVersionInfo);
    }

    public async Task GenerateThirdPartyNoticesAsync(string filePath)
    {
        var content = GenerateNoticesContent();
        await File.WriteAllTextAsync(filePath, content);
    }

    public Task<Dictionary<string, LicenseData>> GetEmbeddedLicensesAsync()
    {
        return Task.FromResult(_embeddedLicenses);
    }

    private static string GetAssemblyVersion()
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;
            return version?.ToString(3) ?? "1.0.0";
        }
        catch
        {
            return "1.0.0";
        }
    }

    private static Dictionary<string, string> GetDetectedDependencies()
    {
        try
        {
            // Try to detect dependencies dynamically from loaded assemblies
            var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            var dependencies = new Dictionary<string, string>();

            var knownDependencies = new Dictionary<string, string>
            {
                { "System.CommandLine", "MIT" },
                { "Spectre.Console", "MIT" },
                { "VYaml", "MIT" },
                { "Microsoft.Extensions.DependencyInjection", "MIT" },
                { "Microsoft.Extensions.Http", "MIT" },
                { "Microsoft.Extensions.Logging", "MIT" },
                { "System.Text.Json", "MIT" },
                { "System.IO.Abstractions", "MIT" },
                { "Polly", "BSD-3-Clause" },
                { "FluentAssertions", "Apache-2.0" },  // for testing
                { "StbImageSharp", "Public Domain" }
            };

            foreach (var assembly in loadedAssemblies)
            {
                var name = assembly.GetName().Name;
                if (name != null && knownDependencies.ContainsKey(name))
                {
                    dependencies[name] = knownDependencies[name];
                }
            }

            // Ensure core dependencies are always included
            foreach (var coreDep in new[] { "System.CommandLine", "Spectre.Console", "VYaml", "StbImageSharp" })
            {
                if (knownDependencies.ContainsKey(coreDep))
                {
                    dependencies[coreDep] = knownDependencies[coreDep];
                }
            }

            return dependencies;
        }
        catch
        {
            // Fallback to static list if dynamic detection fails
            return new Dictionary<string, string>
            {
                { "System.CommandLine", "MIT" },
                { "Spectre.Console", "MIT" },
                { "VYaml", "MIT" },
                { "Microsoft.Extensions.DependencyInjection", "MIT" },
                { "Microsoft.Extensions.Http", "MIT" },
                { "Microsoft.Extensions.Logging", "MIT" },
                { "System.Text.Json", "MIT" },
                { "System.IO.Abstractions", "MIT" },
                { "StbImageSharp", "Public Domain" }
            };
        }
    }

    private string GenerateNoticesContent()
    {
        var content = "THIRD-PARTY SOFTWARE NOTICES AND INFORMATION\n";
        content += "==============================================\n\n";
        content += "This file contains third-party software notices and/or additional terms for licensed third-party software components included within RedmineCLI.\n\n";

        foreach (var license in _embeddedLicenses)
        {
            content += $"-------------------------------------------------------------------------------\n";
            content += $"{license.Value.Name} v{license.Value.Version}\n";
            content += $"Project: {license.Value.ProjectUrl}\n";
            content += $"-------------------------------------------------------------------------------\n";
            content += $"{license.Value.License}\n\n";
        }

        return content;
    }
}
