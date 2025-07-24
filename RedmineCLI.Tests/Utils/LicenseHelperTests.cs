using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using FluentAssertions;

using RedmineCLI.Models;
using RedmineCLI.Utils;

using Xunit;

namespace RedmineCLI.Tests.Utils;

public class LicenseHelperTests
{
    [Fact]
    public async Task ShowLicense_Should_DisplayAllLicense_When_LicenseOptionProvided()
    {
        // Arrange
        var licenseHelper = new LicenseHelper();

        // Act
        var result = await licenseHelper.GetLicenseInfoAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().ContainKey("RedmineCLI");
        result["RedmineCLI"].Name.Should().Be("RedmineCLI");
        result["RedmineCLI"].License.Should().Contain("MIT");
    }

    [Fact]
    public async Task ShowVersion_Should_IncludeLicenseInfo_When_VersionOptionProvided()
    {
        // Arrange
        var licenseHelper = new LicenseHelper();

        // Act
        var versionInfo = await licenseHelper.GetVersionInfoAsync();

        // Assert
        versionInfo.Should().NotBeNull();
        versionInfo.Version.Should().NotBeNullOrEmpty();
        versionInfo.Dependencies.Should().NotBeEmpty();
        versionInfo.Dependencies.Should().ContainKey("System.CommandLine");
        versionInfo.Dependencies["System.CommandLine"].Should().Contain("MIT");
    }

    [Fact]
    public async Task GenerateNotices_Should_CreateFile_When_BuildExecuted()
    {
        // Arrange
        var licenseHelper = new LicenseHelper();
        var tempPath = Path.GetTempFileName();

        try
        {
            // Act
            await licenseHelper.GenerateThirdPartyNoticesAsync(tempPath);

            // Assert
            File.Exists(tempPath).Should().BeTrue();
            var content = await File.ReadAllTextAsync(tempPath);
            content.Should().Contain("THIRD-PARTY SOFTWARE NOTICES");
            content.Should().Contain("RedmineCLI");
            content.Should().Contain("MIT License");
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    [Fact]
    public async Task EmbedLicense_Should_IncludeInBinary_When_AotBuild()
    {
        // Arrange
        var licenseHelper = new LicenseHelper();

        // Act
        var embeddedLicenses = await licenseHelper.GetEmbeddedLicensesAsync();

        // Assert
        embeddedLicenses.Should().NotBeNull();
        embeddedLicenses.Should().NotBeEmpty();
        embeddedLicenses.Should().ContainKey("RedmineCLI");
    }
}
