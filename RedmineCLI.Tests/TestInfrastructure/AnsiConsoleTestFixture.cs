using Spectre.Console;
using Spectre.Console.Testing;

using Xunit;

namespace RedmineCLI.Tests.TestInfrastructure;

/// <summary>
/// テストにおけるAnsiConsoleの状態管理を行うためのフィクスチャ
/// </summary>
public class AnsiConsoleTestFixture : IDisposable
{
    private readonly IAnsiConsole _originalConsole;
    private readonly object _consoleLock = new();

    public AnsiConsoleTestFixture()
    {
        _originalConsole = AnsiConsole.Console;
    }

    /// <summary>
    /// AnsiConsoleをテスト用のTestConsoleに置き換える
    /// </summary>
    public TestConsole CreateTestConsole()
    {
        var testConsole = new TestConsole();
        testConsole.Profile.Capabilities.Interactive = true;
        return testConsole;
    }

    /// <summary>
    /// スレッドセーフなコンソール操作を実行
    /// </summary>
    public T ExecuteWithTestConsole<T>(Func<TestConsole, T> action)
    {
        lock (_consoleLock)
        {
            var testConsole = CreateTestConsole();
            var originalConsole = AnsiConsole.Console;
            try
            {
                AnsiConsole.Console = testConsole;
                return action(testConsole);
            }
            finally
            {
                AnsiConsole.Console = originalConsole;
            }
        }
    }

    /// <summary>
    /// スレッドセーフなコンソール操作を実行（非同期版）
    /// </summary>
    public async Task<T> ExecuteWithTestConsoleAsync<T>(Func<TestConsole, Task<T>> action)
    {
        // 非同期操作でもロックを維持するため、SemaphoreSlimを使用
        using var semaphore = new SemaphoreSlim(1, 1);
        await semaphore.WaitAsync();

        var testConsole = CreateTestConsole();
        var originalConsole = AnsiConsole.Console;
        try
        {
            AnsiConsole.Console = testConsole;
            return await action(testConsole);
        }
        finally
        {
            AnsiConsole.Console = originalConsole;
            semaphore.Release();
        }
    }

    public void Dispose()
    {
        // 元のコンソールを確実に復元
        AnsiConsole.Console = _originalConsole;
    }
}

/// <summary>
/// AnsiConsoleを使用するテストのコレクション定義
/// </summary>
[CollectionDefinition("AnsiConsole")]
public class AnsiConsoleTestCollection : ICollectionFixture<AnsiConsoleTestFixture>
{
    // このクラスは単にコレクションを定義するためのマーカー
}

/// <summary>
/// 順次実行が必要なテストのコレクション定義
/// </summary>
[CollectionDefinition("Sequential", DisableParallelization = true)]
public class SequentialTestCollection
{
    // このクラスは単にコレクションを定義するためのマーカー
}
