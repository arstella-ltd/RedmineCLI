# 設計書

## 概要

RedmineCLIは、Redmine REST APIと通信するコマンドラインインターフェースツールです。
ghコマンドと同様の設計思想を採用し、直感的で一貫性のあるコマンド体系を提供します。
APIキーベースの認証により安全な通信を実現し、設定はYAML形式のファイルで永続化されます。

## アーキテクチャ

### 全体構成
```
┌─────────────────┐
│  CLI Interface  │  ← System.CommandLine
├─────────────────┤
│    Commands     │  ← コマンドハンドラー
├─────────────────┤
│    Services     │  ← ビジネスロジック（DI）
├─────────────────┤
│   API Client    │  ← HttpClient + Polly
├─────────────────┤
│ Config Manager  │  ← VYaml (AOT対応)
└─────────────────┘
```

### レイヤー構成
- **プレゼンテーション層**: System.CommandLineによるコマンド解析、Spectre.Consoleによる出力
- **アプリケーション層**: サービス層によるビジネスロジック、DIコンテナによる依存性注入
- **インフラストラクチャ層**: HttpClientFactoryによるAPI通信、VYamlによる設定管理、System.Text.Json Source Generatorによるシリアライゼーション

### .NET固有の設計
- **依存性注入（DI）**: Microsoft.Extensions.DependencyInjectionを使用（AOT向け最適化）
- **設定管理**: VYaml + カスタムシリアライザー（AOT対応、Source Generator使用）
- **ログ**: Microsoft.Extensions.Loggingによる統一的なログ出力
- **HTTPクライアント**: HttpClientFactory + Pollyによるリトライポリシー
- **非同期処理**: async/awaitパターンの全面採用
- **JSON処理**: System.Text.Json Source Generatorによるコンパイル時生成
- **AOT制約**: リフレクションの最小化、動的コード生成の回避

## コンポーネントとインターフェース

### CLI Interface
- **責任**: ユーザー入力の受付と結果の表示
- **主要機能**
  - コマンドライン引数のパース
  - テーブル形式/JSON形式での出力
  - 対話的入力の処理
  - エラーメッセージの表示

### Command Parser
- **責任**: コマンドの解析と適切なハンドラーへのルーティング
- **主要機能**
  - サブコマンドの識別（auth, issue, config）
  - オプションとフラグの解析
  - バリデーション
  - ヘルプメッセージの生成

### API Client
- **責任**: Redmine REST APIとの通信
- **主要機能**
  - HTTP リクエストの送信（GET, POST, PUT）
  - APIキーによる認証ヘッダーの付与
  - レスポンスのパース
  - エラーハンドリング
  - リトライロジック

### Config Manager
- **責任**: 設定ファイルの読み書きと管理
- **主要機能**
  - YAML形式での設定の永続化
  - 複数プロファイルの管理
  - デフォルト値の提供
  - 設定の暗号化（APIキー等）

## データモデル

### 設定ファイル構造
```yaml
current_profile: default
profiles:
  default:
    url: https://redmine.example.com
    api_key: <encrypted_key>
    default_project: myproject
  staging:
    url: https://redmine-staging.example.com
    api_key: <encrypted_key>
preferences:
  output_format: table
  page_size: 20
```

### C#モデル定義
```csharp
// Issue.cs
public class Issue : IEquatable<Issue>
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    
    [JsonPropertyName("subject")]
    public string Subject { get; set; } = string.Empty;
    
    [JsonPropertyName("description")]
    public string? Description { get; set; }
    
    [JsonPropertyName("project")]
    public Project? Project { get; set; }
    
    [JsonPropertyName("status")]
    public IssueStatus? Status { get; set; }
    
    [JsonPropertyName("priority")]
    public Priority? Priority { get; set; }
    
    [JsonPropertyName("assigned_to")]
    public User? AssignedTo { get; set; }
    
    [JsonPropertyName("created_on")]
    public DateTime CreatedOn { get; set; }
    
    [JsonPropertyName("updated_on")]
    public DateTime UpdatedOn { get; set; }
    
    [JsonPropertyName("done_ratio")]
    public int? DoneRatio { get; set; }
}

// 基本エンティティ
public class Project { public int Id { get; set; } public string Name { get; set; } }
public class IssueStatus { public int Id { get; set; } public string Name { get; set; } }
public class Priority { public int Id { get; set; } public string Name { get; set; } }
public class User { public int Id { get; set; } public string Name { get; set; } }

// Config.cs (VYaml対応)
[YamlObject]
public partial class Config
{
    public string CurrentProfile { get; set; } = "default";
    public Dictionary<string, Profile> Profiles { get; set; } = new();
    public Preferences Preferences { get; set; } = new();
    
    // APIキーの暗号化・復号化メソッド
    private static string EncryptApiKey(string apiKey);
    private static string DecryptApiKey(string encryptedApiKey);
}
```

### JSONシリアライゼーション設計

Native AOT対応のため、System.Text.Json Source Generatorを使用します。

```csharp
// JsonSerializerContext.cs
[JsonSerializable(typeof(Issue))]
[JsonSerializable(typeof(IssueResponse))]
[JsonSerializable(typeof(IssuesResponse))]
[JsonSerializable(typeof(Project))]
[JsonSerializable(typeof(User))]
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
public partial class RedmineJsonContext : JsonSerializerContext { }

// APIレスポンスラッパー
public class IssuesResponse
{
    [JsonPropertyName("issues")]
    public List<Issue> Issues { get; set; } = new();
    
    [JsonPropertyName("total_count")]
    public int TotalCount { get; set; }
    
    [JsonPropertyName("offset")]
    public int Offset { get; set; }
    
    [JsonPropertyName("limit")]
    public int Limit { get; set; }
}
```

### APIクライアントインターフェース

```csharp
public interface IRedmineApiClient
{
    Task<IssuesResponse> GetIssuesAsync(
        int? assignedToId = null,
        int? projectId = null,
        string? status = null,
        int? limit = null,
        int? offset = null,
        CancellationToken cancellationToken = default);
        
    Task<Issue> GetIssueAsync(int id, CancellationToken cancellationToken = default);
    
    Task<Issue> CreateIssueAsync(Issue issue, CancellationToken cancellationToken = default);
    
    Task<Issue> UpdateIssueAsync(int id, Issue issue, CancellationToken cancellationToken = default);
    
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);
}
```

### 設定管理の暗号化

APIキーの保護のため、プラットフォーム固有の暗号化を実装します。

- **Windows**: Data Protection API (DPAPI) を使用
- **macOS/Linux**: Base64エンコーディング（将来的にKeychain/Secret Serviceに対応予定）

```csharp
// Windows環境での暗号化例
var encrypted = ProtectedData.Protect(
    Encoding.UTF8.GetBytes(apiKey), 
    _entropy, 
    DataProtectionScope.CurrentUser);
```

## エラーハンドリング

### エラーの分類
1. **認証エラー**: APIキーが無効または期限切れ
2. **ネットワークエラー**: 接続タイムアウト、DNS解決失敗
3. **APIエラー**: 404 Not Found、422 Unprocessable Entity
4. **入力エラー**: 無効なコマンド、必須パラメータ不足
5. **設定エラー**: 設定ファイルの破損、権限不足

### エラー処理方針
- ユーザーフレンドリーなエラーメッセージを表示
- 可能な場合は回復方法を提案
- デバッグモードで詳細なスタックトレースを提供
- 非ゼロの終了コードを返す

### .NETでの実装例
```csharp
// カスタム例外
public class RedmineApiException : Exception
{
    public int StatusCode { get; }
    public string? ApiError { get; }
    
    public RedmineApiException(int statusCode, string message, string? apiError = null) 
        : base(message)
    {
        StatusCode = statusCode;
        ApiError = apiError;
    }
}

// Pollyによるリトライポリシー
services.AddHttpClient<IRedmineApiClient, RedmineApiClient>()
    .AddPolicyHandler(HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(3, retryAttempt => 
            TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));
```

## テスト戦略

### テストレベル
1. **単体テスト**: 各コンポーネントの個別機能
2. **統合テスト**: API通信のモック、設定ファイルの読み書き
3. **E2Eテスト**: 実際のコマンド実行シナリオ

### テスト対象
- コマンドパーサーの正確性
- API通信のエラーハンドリング
- 設定ファイルの互換性
- 出力フォーマットの正確性

### .NETテストツール
- **xUnit**: 単体テストフレームワーク
- **NSubstitute**: シンプルで使いやすいモッキングライブラリ（AOT対応）
- **FluentAssertions**: 読みやすいアサーション
- **WireMock.Net**: HTTPモックサーバー（統合テスト用）
- **Coverlet**: コードカバレッジ測定

## UI/UXデザイン

### コマンド体系
- 動詞-名詞の構造（`redmine issue list`）
- 一貫したオプション名（`--json`, `--limit`）
- 短縮オプションの提供（`-p` for `--project`）

### 出力デザイン
```
# テーブル形式の例（英語表示）
ID     STATUS        SUBJECT                           ASSIGNEE      UPDATED
#1234  New           Implement login functionality     John Doe      2024-01-15
#1235  In Progress   Review database design            Jane Smith    2024-01-14

# 成功メッセージ
✓ Issue #1234 created successfully
View it at: https://redmine.example.com/issues/1234

# エラーメッセージ
✗ Error: Authentication failed
Please run 'redmine auth login' to set up your credentials
```

### 対話的入力
- 選択肢の提示にはラジオボタン風の表示
- 必須項目には * マークを付与
- デフォルト値を括弧内に表示

### Spectre.Consoleによる実装例
```csharp
// プロンプト
var projectName = AnsiConsole.Ask<string>("Enter [green]project name[/]:");

// 選択
var status = AnsiConsole.Prompt(
    new SelectionPrompt<string>()
        .Title("Select [green]issue status[/]")
        .AddChoices(new[] { "New", "In Progress", "Resolved", "Closed" }));

// テーブル表示
var table = new Table();
table.AddColumn("ID");
table.AddColumn("Subject");
table.AddColumn("Status");
table.AddRow("#1234", "Fix login bug", "[red]Open[/]");
AnsiConsole.Write(table);
```

## パフォーマンス考慮事項

### API通信の最適化
- HTTP Keep-Aliveによる接続の再利用
- 並列リクエストの制限（デフォルト: 5並列）
- レスポンスのページネーション対応

### キャッシュ戦略
- プロジェクト一覧、ユーザー一覧の短期キャッシュ（5分）
- キャッシュの無効化オプション（`--no-cache`）
- メモリ使用量の上限設定

### 起動時間の最適化
- Native AOTコンパイルによるJIT不要の即時起動
- 遅延初期化による起動高速化
- 必要最小限のモジュールのみロード
- 設定ファイルの効率的な読み込み
- ランタイム依存の排除による軽量化