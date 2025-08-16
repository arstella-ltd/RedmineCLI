using System.Runtime.InteropServices;

using RedmineCLI.Common.Services;

namespace RedmineCLI.Common.Tests.Services;

public class CredentialStoreFactoryTests
{
    [Fact]
    public void Create_Should_ReturnCorrectImplementation_ForCurrentPlatform()
    {
        // Act
        using var store = CredentialStore.Create();

        // Assert
        store.Should().NotBeNull();
        store.Should().BeAssignableTo<ICredentialStore>();

        // プラットフォーム固有の実装を確認
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            store.Should().BeOfType<WindowsCredentialStore>();
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            store.Should().BeOfType<MacOSCredentialStore>();
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            // Linux環境では、secret-toolが利用可能な場合はLinuxCredentialStore、
            // そうでない場合はFileBasedCredentialStoreが返される
            if (LinuxCredentialStore.IsSecretToolAvailable())
            {
                store.Should().BeOfType<LinuxCredentialStore>();
            }
            else
            {
                store.Should().BeOfType<FileBasedCredentialStore>();
            }
        }
        else
        {
            store.Should().BeOfType<FileBasedCredentialStore>();
        }
    }

    [Fact]
    public void Create_Should_ReturnDisposableInstance()
    {
        // Act
        var store = CredentialStore.Create();

        // Assert
        store.Should().BeAssignableTo<IDisposable>();

        // Clean up
        store.Dispose();
    }
}
