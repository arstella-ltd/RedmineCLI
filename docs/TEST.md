# RedmineCLI ユニットテストガイド

このドキュメントは、RedmineCLIプロジェクトのユニットテストに関するガイドラインと、テストの安定性を確保するための手順を説明します。

## テスト方針

### t-wada流のテスト手法

このプロジェクトでは、和田卓人（t-wada）氏が推奨するテスト手法に従います。

#### 1. テスト駆動開発（TDD）

**Red → Green → Refactor** のサイクルを徹底します。

```bash
# 1. Red: 失敗するテストを書く
dotnet test --filter "NewTestMethodName"

# 2. Green: テストを通す最小限の実装
dotnet test --filter "NewTestMethodName"

# 3. Refactor: コードを改善
dotnet test
```

#### 2. テストのAAAパターン

すべてのテストはArrange-Act-Assertパターンに従います。

```csharp
[Fact]
public async Task TestMethod_Should_ExpectedBehavior_When_Condition()
{
    // Arrange: テストの準備
    var service = new RedmineService(mockApiClient.Object);
    var expectedIssues = new List<Issue> { ... };
    
    // Act: テスト対象の実行
    var result = await service.GetIssuesAsync();
    
    // Assert: 結果の検証
    result.Should().BeEquivalentTo(expectedIssues);
}
```

#### 3. テストコードの品質基準

- プロダクションコードと同等の品質を保つ
- テストの意図が明確になる命名
- 1テスト1アサーション原則
- DRY原則の適用（共通処理は基底クラスやヘルパーメソッドへ）

#### 4. テストダブルの使い分け

- **スタブ**: 依存オブジェクトの代替（状態の検証）
- **モック**: 振る舞いの検証が必要な場合のみ使用
- できるだけ本物のオブジェクトを使用

#### 5. テストピラミッド

```
         /\
        /  \  E2Eテスト（少）
       /____\
      /      \  統合テスト（中）
     /________\
    /          \  単体テスト（多）
   /____________\
```

## テストの実行

### 基本的なテストコマンド

```bash
# 全テストの実行
dotnet test

# カバレッジ付きで実行
dotnet test --collect:"XPlat Code Coverage"

# 特定のテストカテゴリの実行
dotnet test --filter Category=Unit
dotnet test --filter FullyQualifiedName~IssueCommandTests

# 単一のテストメソッドの実行
dotnet test --filter "FullyQualifiedName=RedmineCLI.Tests.Commands.IssueCommandTests.List_Should_ReturnFilteredIssues_When_StatusIsSpecified"

# テストログを詳細に出力
dotnet test --logger "console;verbosity=detailed"
```

### テストの並行実行制御

このプロジェクトでは、AnsiConsoleの競合を防ぐため、テストの並行実行を制限しています。

`RedmineCLI.Tests/xunit.runner.json`:
```json
{
  "$schema": "https://xunit.net/schema/current/xunit.runner.schema.json",
  "maxParallelThreads": 1,
  "parallelizeTestCollections": false,
  "parallelizeAssembly": false,
  "diagnosticMessages": true
}
```

## AnsiConsoleを使用するテスト

### 問題の背景

AnsiConsoleはグローバルな静的インスタンスを使用するため、並行実行されるテストで競合状態が発生する可能性があります。

### 解決策：AnsiConsoleTestFixture

`TestInfrastructure/AnsiConsoleTestFixture.cs`を使用して、AnsiConsoleの状態を適切に管理します。

```csharp
[Collection("AnsiConsole")]
public class MyCommandTests
{
    private readonly AnsiConsoleTestFixture _consoleFixture;

    public MyCommandTests()
    {
        _consoleFixture = new AnsiConsoleTestFixture();
    }

    [Fact]
    public async Task MyTest()
    {
        // Arrange
        var command = new MyCommand();

        // Act & Assert
        var result = await _consoleFixture.ExecuteWithTestConsoleAsync(async console =>
        {
            var actualResult = await command.ExecuteAsync();
            
            // コンソール出力の検証
            console.Output.Should().Contain("Expected output");
            
            return actualResult;
        });

        // Assert
        result.Should().Be(0);
    }
}
```

### テストコレクション

AnsiConsoleを使用するテストクラスには、`[Collection("AnsiConsole")]`属性を付与します。

```csharp
[Collection("AnsiConsole")]
public class AuthCommandTests
{
    // テストコード
}
```

## モックとサブスティテュート

### NSubstituteの使用

このプロジェクトではNSubstituteを使用してモックオブジェクトを作成します。

```csharp
// インターフェースのモック作成
var configService = Substitute.For<IConfigService>();
var apiClient = Substitute.For<IRedmineApiClient>();

// 戻り値の設定
configService.GetActiveProfileAsync()
    .Returns(Task.FromResult<Profile?>(testProfile));

// 呼び出しの検証
await configService.Received(1).SaveConfigAsync(Arg.Any<Config>());

// 特定の条件での検証
await configService.Received(1).SaveConfigAsync(Arg.Is<Config>(c =>
    c.Profiles.ContainsKey("default") &&
    c.Profiles["default"].ApiKey == "test-key"));
```

## テストの命名規則

### メソッド名

```
[UnitOfWork]_Should_[ExpectedBehavior]_When_[Condition]
```

例：
- `Login_Should_SaveCredentials_When_ValidInput`
- `Status_Should_ShowConnectionState_When_Authenticated`
- `Logout_Should_ReturnError_When_NoActiveProfile`

### テストクラス名

```
[TargetClass]Tests
```

例：
- `AuthCommandTests`
- `ConfigServiceTests`
- `RedmineApiClientTests`

## テストのデバッグ

### Visual Studio / Visual Studio Code

1. テストエクスプローラーからデバッグ実行
2. ブレークポイントの設定
3. デバッグコンソールで変数の確認

### コマンドライン

```bash
# 特定のテストのみ実行（デバッグしやすい）
dotnet test --filter "FullyQualifiedName=特定のテスト名" --logger "console;verbosity=detailed"

# テスト結果をファイルに出力
dotnet test --logger "trx;LogFileName=test_results.trx"
```

## テスト安定性のチェックリスト

### 新しいテストを追加する前に

- [ ] AnsiConsoleを使用する場合は`AnsiConsoleTestFixture`を使用
- [ ] 非同期処理は適切にawaitされているか確認
- [ ] テストデータは他のテストと競合しないか確認
- [ ] ファイルシステムを使用する場合は一時ディレクトリを使用
- [ ] テスト後のクリーンアップ処理が実装されているか確認

### テストが不安定な場合

1. **並行実行の問題を確認**
   ```bash
   # 10回繰り返して実行
   for i in {1..10}; do
     echo "Run $i"
     dotnet test --filter "特定のテスト名"
   done
   ```

2. **グローバル状態の確認**
   - AnsiConsole.Console
   - 静的変数
   - 環境変数

3. **タイミングの問題を確認**
   - 非同期処理の待機不足
   - リトライロジックの不足

## 継続的改善

### カバレッジ目標

- 単体テスト: 80%以上
- 重要なビジネスロジック: 90%以上
- エラーハンドリング: 100%

### テストの保守

- 定期的にテストの実行時間を確認
- 不要なテストの削除
- テストコードのリファクタリング
- 新しいパターンの文書化

## トラブルシューティング

### よくある問題と解決策

#### 1. "Error: URL cannot be empty" エラー

**原因**: AnsiConsoleが適切に初期化されていない

**解決策**: `AnsiConsoleTestFixture`を使用してテストコンソールを設定

#### 2. テストが時々失敗する

**原因**: 並行実行による競合状態

**解決策**: 
- `xunit.runner.json`で並行実行を無効化
- `[Collection("Sequential")]`属性を使用

#### 3. ファイルアクセスエラー

**原因**: 複数のテストが同じファイルにアクセス

**解決策**: 
- 各テストで一意の一時ファイルを使用
- `Path.GetTempFileName()`または`Guid.NewGuid()`を使用

## 参考資料

- [xUnit Documentation](https://xunit.net/)
- [NSubstitute Documentation](https://nsubstitute.github.io/)
- [FluentAssertions Documentation](https://fluentassertions.com/)
- [t-wada's Testing Philosophy](https://github.com/t-wada)