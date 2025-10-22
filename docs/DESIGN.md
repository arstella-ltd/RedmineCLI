# 設計書

## 概要

RedmineCLIは、Redmine REST APIと通信するコマンドラインインターフェースツールです。
ghコマンドと同様の設計思想を採用し、直感的で一貫性のあるコマンド体系を提供します。
APIキーベースの認証により安全な通信を実現し、設定はYAML形式のファイルで永続化されます。

## アーキテクチャ

### 全体構成
```
┌─────────────────┐
│  CLI Interface  │  ← System.CommandLine（ショートハンド対応）
├─────────────────┤
│    Commands     │  ← コマンドハンドラー（@me処理、--web対応）
├─────────────────┤
│   Formatters    │  ← ITableFormatter / IJsonFormatter
├─────────────────┤
│    Services     │  ← ビジネスロジック（DI）
├─────────────────┤
│   API Client    │  ← HttpClient + Polly
├─────────────────┤
│ Config Manager  │  ← VYaml (AOT対応) + System.IO.Abstractions
└─────────────────┘
```

### レイヤー構成
- **プレゼンテーション層**: System.CommandLineによるコマンド解析、Spectre.Consoleによる出力
- **アプリケーション層**: サービス層によるビジネスロジック、DIコンテナによる依存性注入
- **インフラストラクチャ層**: HttpClientFactoryによるAPI通信、VYamlによる設定管理、System.Text.Json Source Generatorによるシリアライゼーション、System.IO.Abstractionsによるファイルシステム抽象化

### .NET固有の設計
- **依存性注入（DI）**: Microsoft.Extensions.DependencyInjectionを使用（AOT向け最適化）
- **設定管理**: VYaml + カスタムシリアライザー（AOT対応、Source Generator使用）
- **ログ**: Microsoft.Extensions.Loggingによる統一的なログ出力
- **HTTPクライアント**: HttpClientFactory + Pollyによるリトライポリシー
- **非同期処理**: async/awaitパターンの全面採用
- **JSON処理**: System.Text.Json Source Generatorによるコンパイル時生成
- **AOT制約**: リフレクションの最小化、動的コード生成の回避
- **AOT互換性**: ILLink.Descriptors.xmlによる型の保護、警告抑制（IL2104, IL3053）

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

### Authentication Command Design
- **責任**: ユーザー認証とプロファイル管理
- **主要コマンド**
  - `auth login`: 認証情報の登録（APIキー/ユーザー名・パスワード）
  - `auth login --save-password`: ユーザー名・パスワードをOSキーチェーンに保存
  - `auth status`: 現在の認証状態確認（キーチェーン保存状態を含む）
  - `auth logout`: 認証情報のクリア
  - `auth logout --clear-keychain`: OSキーチェーンから認証情報を削除
- **認証フロー**
  ```
  1. 認証方式の選択（APIキー / ユーザー名・パスワード）
     ↓
  2. URLとAPIキー/認証情報の入力（対話的/パラメータ指定）
     ↓
  3. URL形式バリデーション
     ↓
  4. Redmine APIへの接続テスト
     ↓
  5. 成功時：
     - APIキー：config.ymlに保存
     - パスワード（--save-password）：OSキーチェーンに保存
     失敗時：エラーメッセージ表示
  ```
- **OSキーチェーン統合**
  - Windows: Credential Manager API
  - macOS: Keychain Services
  - Linux: libsecret (Secret Service API)
  - 保存形式: `RedmineCLI:{serverUrl}` というキー名で保存
- **認証優先順位**
  1. APIキー（config.yml）
  2. セッションクッキー（キーチェーン）
  3. ユーザー名・パスワード（キーチェーン）
  4. 対話的入力
- **セキュリティ考慮事項**
  - APIキーは暗号化して保存（Windows: DPAPI、その他: Base64）
  - パスワードはOSキーチェーンで暗号化（環境変数経由では渡さない）
  - 接続テスト時のみ認証情報を使用
  - ログアウト時はAPIキーのみクリア（他の設定は保持）
  - --clear-keychainオプションでキーチェーンからも削除可能

### Issue Command Design
- **責任**: チケットの一覧表示、詳細表示、作成、編集
- **主要コマンド**
  - `issue list`: チケット一覧表示（デフォルト：全オープンチケット）
  - `issue view <ID>`: チケット詳細表示
  - `issue create`: 新規チケット作成
  - `issue edit <ID>`: チケット編集
  - `issue comment <ID>`: コメント追加
- **ショートハンドオプション**
  - `-a` / `--assignee`: 担当者フィルタ/指定
  - `-s` / `--status`: ステータスフィルタ
  - `-p` / `--project`: プロジェクトフィルタ/指定
  - `-L` / `--limit`: 表示件数制限
  - `-w` / `--web`: ブラウザで開く
  - `-t` / `--title`: タイトル指定
  - `-d` / `--description`: 説明指定
  - `-m` / `--message`: メッセージ指定
- **特殊値の処理**
  - `@me`: 現在の認証ユーザーを表す特殊値
  - `all`: 全てのステータスを表す特殊値
- **フィルタリング設計**
  ```
  IssueFilter {
    AssignedToId: string?  // ユーザーID or @me
    ProjectId: string?     // プロジェクトID
    StatusId: string?      // ステータスID or all
    Limit: int?            // 表示件数
    Offset: int?           // オフセット
  }
  ```
- **--webオプションの動作**
  - 条件をURLクエリパラメータに変換（`set_filter=1`を含む）
  - ブラウザ起動の優先順位
    1. $BROWSER環境変数（%sプレースホルダ対応）
    2. プラットフォーム固有コマンド（Windows: start、macOS: open、Linux: xdg-open）
  - 例: `/issues?set_filter=1&assigned_to_id=me&status_id=o`

#### Issue Edit Command 詳細設計
- **実行モード**
  - 対話的モード：オプション無しで起動した場合
  - 非対話的モード：更新するオプションを指定した場合
- **対話的編集フロー**
  ```
  1. 現在のチケット情報の取得・表示
     ↓
  2. 編集項目選択（SelectionPrompt）
     - Status / Assignee / Progress / Done (save changes) / Cancel
     ↓
  3. 選択された項目の編集
     - Status: 利用可能ステータス一覧から選択
     - Assignee: 現在ユーザーに割り当て or ユーザー選択
     - Progress: 0-100の数値入力（バリデーション付き）
     ↓
  4. 変更内容の確認・ループ継続
     ↓
  5. チケット更新API呼び出し（部分更新）
     ↓
  6. 成功時：更新サマリー/URL表示
  ```
- **部分更新（PATCH）設計**
  ```
  IssueUpdateData {
    subject: string?      // 必須（現在値を設定）
    status_id: int?       // ステータス更新時のみ
    assigned_to_id: int?  // 担当者更新時のみ
    done_ratio: int?      // 進捗率更新時のみ
  }
  ```
- **Redmine API制約への対応**
  - 部分更新でもsubjectフィールドが必須
  - 更新前に現在のissueを取得してsubjectを設定
  - 空レスポンス対応：レスポンスが空の場合は更新後のissueを再取得
- **ステータス名解決フロー**
  - `/issue_statuses.json`エンドポイントから利用可能ステータスを取得
  - 名前による大文字小文字を無視した検索でIDに解決
  - 数値の場合はIDとして直接使用
- **@me処理フロー**（共通処理）
  - 担当者に`@me`が指定された場合、現在のユーザー情報を取得
  - `/users/current.json`エンドポイントを使用
  - 取得したユーザーIDを`assigned_to_id`に設定
- **進捗率バリデーション**
  - 0-100の範囲チェック
  - 数値以外の入力に対するエラーハンドリング
- **更新サマリー表示**
  - 何のフィールドを何に変更したかを明確に表示
  - 例：`status → In Progress, assignee → John Doe, progress → 75%`
- **エラーハンドリング**
  - 存在しないチケットID（404エラー）
  - 無効なステータス名
  - API応答のエラーメッセージを適切に表示
  - 空レスポンス時の自動再取得処理

#### Issue Create Command 詳細設計
- **実行モード**
  - 対話的モード：オプション無しで起動した場合
  - 非対話的モード：必要なオプションを指定した場合
- **対話的入力フロー**
  ```
  1. プロジェクト選択（SelectionPrompt）
     ↓
  2. タイトル入力（TextPrompt、必須）
     ↓
  3. 説明入力（TextPrompt、任意）
     ↓
  4. 自分に割り当てるか確認（Confirm）
     ↓
  5. チケット作成API呼び出し
     ↓
  6. 成功時：チケットID/URL表示
  ```
- **APIリクエスト形式**
  ```
  IssueCreateData {
    project_id: int?      // プロジェクトID
    subject: string       // タイトル
    description: string?  // 説明
    assigned_to_id: int?  // 担当者ID
  }
  ```
- **@me処理フロー**
  - 担当者に`@me`が指定された場合、現在のユーザー情報を取得
  - `/users/current.json`エンドポイントを使用
  - 取得したユーザーIDを`assigned_to_id`に設定
- **プロジェクト/担当者の解析**
  - 数値の場合：IDとして処理
  - 文字列の場合：識別子/ユーザー名として処理
- **エラーハンドリング**
  - 必須フィールド（タイトル、プロジェクト）の検証
  - API応答のエラーメッセージを適切に表示
  - ValidationExceptionとRedmineApiExceptionの使い分け

#### Issue Comment Command 詳細設計
- **実行モード**
  - エディタモード：メッセージオプション無しで起動した場合
  - 直接入力モード：-m/--messageオプションを指定した場合
- **エディタ統合フロー**
  ```
  1. 一時ファイル作成とコメントテンプレート書き込み
     ↓
  2. $EDITOR環境変数またはプラットフォーム規定エディタ起動
     - Windows: notepad
     - Unix系: nano
     ↓
  3. エディタでのコメント編集
     ↓
  4. ファイル読み込みと#で始まる行の除外
     ↓
  5. 一時ファイルの削除
  ```
- **エラー処理とフォールバック**
  - エディタ起動失敗時：コンソール入力フォールバック
  - 空コメント検証：トリム後の文字数チェック
  - テンポラリファイル管理：finally句での確実な削除
- **APIリクエスト形式**
  ```
  CommentRequest {
    issue: CommentData {
      notes: string  // コメント内容
    }
  }
  ```
- **成功時の表示**
  - 確認メッセージ：「Comment added to issue #123」
  - チケットURL表示：プロファイルURL + /issues/{id}
- **エラーハンドリング**
  - 404エラー：「Issue #{id} not found」
  - 空コメントエラー：「Comment cannot be empty」
  - その他APIエラー：レスポンスメッセージをそのまま表示

### Attachment Command Design
- **責任**: チケット添付ファイルの表示とダウンロード
- **主要コマンド**
  - `issue attachment list <issue-id>`: チケットの添付ファイル一覧表示
  - `issue attachment download <issue-id>`: 対話的な選択ダウンロード（デフォルト動作）
  - `issue attachment download <issue-id> --all`: 全添付ファイルの一括ダウンロード
  - `issue attachment download <issue-id> --output <directory>`: 出力ディレクトリ指定
- **API統合**
  - `GetIssueAsync`メソッドに`include=attachments`パラメータを自動追加
  - チケット取得時に添付ファイル情報も同時に取得
  - `/attachments/:id.json`エンドポイントでメタデータ取得（GetAttachmentAsync）
- **ダウンロード処理フロー**
  ```
  1. チケット情報の取得（添付ファイル含む）
     ↓
  2. 添付ファイルの存在確認
     ↓
  3. ダウンロードモードの決定（対話的/全選択）
     ↓
  4. 出力ディレクトリの決定（--outputまたはカレント）
     ↓
  5. 並行ダウンロード処理（Progress表示付き）
     ↓
  6. ファイル名の重複回避処理（スレッドセーフ）
     ↓
  7. 成功/エラーメッセージの表示
  ```
- **対話的選択フロー**（`issue attachment download`のデフォルト動作）
  ```
  1. チケットの添付ファイル一覧取得
     ↓
  2. MultiSelectionPromptで複数選択可能
     - ファイル名とサイズを表示
     - チェックボックス形式で複数選択
     ↓
  3. 選択されたファイルを並行ダウンロード
     ↓
  4. 各ファイルのダウンロード進捗表示
  ```
- **ファイル名重複処理**
  ```csharp
  // スレッドセーフな重複ファイル名処理
  lock (filenameLock)
  {
      fileName = attachment.Filename;
      filePath = Path.Combine(outputDirectory, fileName);
      
      var counter = 1;
      while (File.Exists(filePath) || usedFilenames.Contains(filePath))
      {
          fileName = $"{fileNameWithoutExt}_{counter}{extension}";
          filePath = Path.Combine(outputDirectory, fileName);
          counter++;
      }
      
      usedFilenames.Add(filePath);
      fileStream = File.Create(filePath); // lockブロック内でファイル作成
  }
  ```
- **表示フォーマット**
  - テーブル形式：Spectre.Consoleのテーブルで添付ファイル情報を表示
  - JSON形式：添付ファイルを含む完全なチケット情報をJSON出力
  - issue viewコマンドでも添付ファイル一覧を表示
- **エラーハンドリング**
  - 401 Unauthorized：認証エラー
  - 403 Forbidden：アクセス権限なし
  - 404 Not Found：チケットまたは添付ファイルが存在しない
  - ネットワークエラー：Pollyによるリトライ処理
  - ファイルアクセスエラー：適切なエラーメッセージと続行処理

### Llms Command Design
- **責任**: AIエージェント向けのLLMs.txt標準形式での情報出力
- **主要コマンド**
  - `llms`: RedmineCLIの使用方法をLLMs.txt形式で出力
- **出力内容**
  - インストール方法（Homebrew）
  - 認証方法（auth login）
  - 主要コマンドとその使用例
  - 利用可能なオプション
  - 特殊機能（@me、Native AOT、Sixel対応）
  - 設定ファイルパス
  - API要件
  - 一般的なワークフロー例
- **実装設計**
  - AnsiConsole.WriteLineを使用した直接出力
  - マークダウン形式でのフォーマット
  - 実際のコマンド例を含む実用的な内容
  - AIエージェントが理解しやすい構造化された情報
- **エラーハンドリング**
  - 単純な出力処理のため、基本的なエラーハンドリングのみ
  - ログ出力によるデバッグサポート

### MCP Command Design
- **責任**: Model Context Protocol（MCP）サーバーとして動作し、AIエージェントからのRedmine操作を可能にする
- **主要コマンド**
  - `mcp`: MCPサーバーを起動（stdio通信）
  - `mcp --debug`: デバッグログ付きで起動
- **プロトコル仕様**
  - JSON-RPC 2.0ベースの通信
  - stdio（標準入出力）による双方向通信
  - リクエスト/レスポンス形式のメッセージ交換
- **提供ツール（Tools）**
  - `get_issues`: チケット一覧取得（assignee、status、project等でフィルタリング可能）
  - `get_issue`: チケット詳細取得（ID指定）
  - `create_issue`: 新規チケット作成（project、subject、description等を指定）
  - `update_issue`: チケット更新（status、assignee、done_ratio等を変更）
  - `add_comment`: コメント追加（チケットIDとメッセージを指定）
  - `get_projects`: プロジェクト一覧取得
  - `get_users`: ユーザー一覧取得
  - `get_statuses`: ステータス一覧取得
  - `search`: 全文検索（キーワード指定）
- **提供リソース（Resources）**
  - `issue://{id}`: 特定チケットの詳細情報
  - `issues://`: 自分に割り当てられたチケット一覧
  - `project://{id}/issues`: プロジェクトのチケット一覧
- **認証処理**
  - 起動時に設定ファイル（config.yml）から認証情報を読み込み
  - 既存のIRedmineApiClientを使用してAPI通信を実行
  - 認証情報が未設定の場合はエラーを返す
- **実装アーキテクチャ**
  ```
  MCPコマンド起動
    ↓
  設定ファイル読み込み（ConfigService）
    ↓
  MCPサーバー初期化（stdio通信開始）
    ↓
  JSON-RPCメッセージループ
    ├─ initialize: サーバー情報返却
    ├─ tools/list: ツール一覧返却
    ├─ tools/call: ツール実行（RedmineApiClient呼び出し）
    ├─ resources/list: リソース一覧返却
    └─ resources/read: リソース取得（RedmineApiClient呼び出し）
  ```
- **エラーハンドリング**
  - JSON-RPC Error形式でエラーを返却
  - エラーコード定義（-32700: Parse error、-32600: Invalid Request等）
  - Redmine API エラーの適切なマッピング
  - デバッグモード時は詳細なログを標準エラー出力に出力
- **Native AOT対応**
  - JSON-RPC メッセージの処理にSource Generatorを使用
  - リフレクション不使用の実装
  - 高速起動（< 100ms）を維持
- **Claude Code統合**
  - 設定ファイル形式: `{"command": "redmine", "args": ["mcp"]}`
  - MCPサーバー名: `redmine`
  - MCPサーバー説明: `Redmine ticket management via MCP`

### List Commands Design (User/Project/Status)
- **責任**: Redmineに登録されているマスターデータの一覧表示
- **主要コマンド**
  - `user list` / `user ls`: ユーザー一覧表示
  - `project list` / `project ls`: プロジェクト一覧表示
  - `status list` / `status ls`: チケットステータス一覧表示
- **共通オプション**
  - `--json`: JSON形式で出力
  - `--limit <number>` / `-L <number>`: 表示件数制限（ユーザー一覧のみ）
- **ユーザー一覧コマンド詳細**
  - **表示項目**: ID、ログイン名、氏名、メールアドレス、作成日時
  - **APIエンドポイント**: `/users.json`
  - **ページネーション対応**: limit/offsetパラメータ使用
- **プロジェクト一覧コマンド詳細**
  - **表示項目**: ID、識別子、名前、説明、作成日時
  - **追加オプション**: `--public` - 公開プロジェクトのみ表示
  - **APIエンドポイント**: `/projects.json`
- **ステータス一覧コマンド詳細**
  - **表示項目**: ID、名前、終了ステータス、デフォルトステータス
  - **APIエンドポイント**: `/issue_statuses.json`
- **表示フォーマット設計**
  ```
  # ユーザー一覧（テーブル形式）
  ID    LOGIN       NAME            EMAIL                   CREATED
  1     admin       Administrator   admin@example.com       2024-01-01 10:00
  2     johndoe     John Doe        john@example.com        2024-01-15 14:30
  
  # プロジェクト一覧（テーブル形式）
  ID    IDENTIFIER      NAME                    DESCRIPTION                             CREATED
  1     main-project    Main Project            Main development project                2024-01-01 10:00
  2     sub-project     Sub Project             Sub project for testing                 2024-02-01 09:00
  
  # ステータス一覧（テーブル形式）
  ID    NAME            CLOSED    DEFAULT
  1     New             No        Yes
  2     In Progress     No        No
  3     Resolved        No        No
  4     Closed          Yes       No
  ```
- **実装上の考慮事項**
  - 既存のIRedmineApiClientインターフェースを使用
  - TableFormatterとJsonFormatterを活用した出力
  - 権限エラー（403 Forbidden）の適切なハンドリング
  - ユーザー一覧は管理者権限が必要な場合があることを考慮

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
currentProfile: default
profiles:
  default:
    name: default
    url: https://redmine.example.com
    apiKey: <encrypted_key>
    defaultProject: myproject
    preferences: null
  staging:
    name: staging
    url: https://redmine-staging.example.com
    apiKey: <encrypted_key>
    defaultProject: null
    preferences: null
preferences:
  defaultFormat: table
  pageSize: 20
  useColors: true
  dateFormat: "yyyy-MM-dd HH:mm:ss"
  editor: null
  timeFormat: "HH:mm:ss"
  time:
    format: relative  # relative | absolute | utc
    timezone: system  # system | UTC | Asia/Tokyo など
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
    
    [JsonPropertyName("due_date")]
    public DateTime? DueDate { get; set; }
    
    [JsonPropertyName("journals")]
    public List<Journal>? Journals { get; set; }
    
    [JsonPropertyName("attachments")]
    public List<Attachment>? Attachments { get; set; }
}

// Journal.cs
public class Journal
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("user")]
    public User? User { get; set; }

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }

    [JsonPropertyName("created_on")]
    public DateTime CreatedOn { get; set; }

    [JsonPropertyName("details")]
    public List<JournalDetail>? Details { get; set; }
}

public class JournalDetail
{
    [JsonPropertyName("property")]
    public string Property { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("old_value")]
    public string? OldValue { get; set; }

    [JsonPropertyName("new_value")]
    public string? NewValue { get; set; }
}

// 基本エンティティ
public class Project { public int Id { get; set; } public string Name { get; set; } }
public class IssueStatus { public int Id { get; set; } public string Name { get; set; } }
public class Priority { public int Id { get; set; } public string Name { get; set; } }
public class User { public int Id { get; set; } public string Name { get; set; } }

// Attachment.cs (添付ファイル)
public class Attachment
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    
    [JsonPropertyName("filename")]
    public string Filename { get; set; } = string.Empty;
    
    [JsonPropertyName("filesize")]
    public long Filesize { get; set; }
    
    [JsonPropertyName("content_type")]
    public string ContentType { get; set; } = string.Empty;
    
    [JsonPropertyName("description")]
    public string? Description { get; set; }
    
    [JsonPropertyName("content_url")]
    public string ContentUrl { get; set; } = string.Empty;
    
    [JsonPropertyName("author")]
    public User? Author { get; set; }
    
    [JsonPropertyName("created_on")]
    public DateTime CreatedOn { get; set; }
}

// SearchResponse.cs (検索APIレスポンス)
public class SearchResponse
{
    [JsonPropertyName("results")]
    public List<SearchResult> Results { get; set; } = new();
    
    [JsonPropertyName("total_count")]
    public int TotalCount { get; set; }
    
    [JsonPropertyName("offset")]
    public int Offset { get; set; }
    
    [JsonPropertyName("limit")]
    public int Limit { get; set; }
}

// SearchResult.cs (検索結果アイテム)
public class SearchResult
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;
    
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
    
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;
    
    [JsonPropertyName("description")]
    public string? Description { get; set; }
    
    [JsonPropertyName("datetime")]
    public DateTime? DateTime { get; set; }
}

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

// Profile.cs
[YamlObject]
public partial class Profile
{
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string? DefaultProject { get; set; }
    public Preferences? Preferences { get; set; }
}

// Preferences.cs
[YamlObject]
public partial class Preferences
{
    public string DefaultFormat { get; set; } = "table";
    public int PageSize { get; set; } = 20;
    public bool UseColors { get; set; } = true;
    public string DateFormat { get; set; } = "yyyy-MM-dd HH:mm:ss";
    public string? Editor { get; set; }
    public string TimeFormat { get; set; } = "HH:mm:ss";
    public TimeSettings Time { get; set; } = new();
}

// TimeSettings.cs
[YamlObject]
public partial class TimeSettings
{
    public string Format { get; set; } = "relative"; // relative | absolute | utc
    public string Timezone { get; set; } = "system"; // system | UTC | Asia/Tokyo など
}
```

### JSONシリアライゼーション設計

Native AOT対応のため、System.Text.Json Source Generatorを使用します。
VYamlについては、[YamlObject]属性による自動生成フォーマッターを使用し、ILLink.Descriptors.xmlで型情報を保護します。

```csharp
// JsonSerializerContext.cs
[JsonSerializable(typeof(Issue))]
[JsonSerializable(typeof(IssueResponse))]
[JsonSerializable(typeof(IssuesResponse))]
[JsonSerializable(typeof(Project))]
[JsonSerializable(typeof(User))]
[JsonSerializable(typeof(Journal))]
[JsonSerializable(typeof(List<Journal>))]
[JsonSerializable(typeof(JournalDetail))]
[JsonSerializable(typeof(List<JournalDetail>))]
[JsonSerializable(typeof(Attachment))]
[JsonSerializable(typeof(List<Attachment>))]
[JsonSerializable(typeof(SearchResponse))]
[JsonSerializable(typeof(SearchResult))]
[JsonSerializable(typeof(List<SearchResult>))]
[JsonSerializable(typeof(AttachmentResponse))]
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

// 部分更新用リクエスト（チケット編集）
public class IssueUpdateRequest
{
    [JsonPropertyName("issue")]
    public IssueUpdateData Issue { get; set; } = new();
}

public class IssueUpdateData
{
    [JsonPropertyName("subject")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Subject { get; set; }

    [JsonPropertyName("status_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? StatusId { get; set; }

    [JsonPropertyName("assigned_to_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? AssignedToId { get; set; }

    [JsonPropertyName("done_ratio")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? DoneRatio { get; set; }
}
```

### サービスインターフェース

```csharp
// IErrorMessageService.cs
public interface IErrorMessageService
{
    string GetUserFriendlyMessage(Exception ex);
    string GetRecoveryHint(Exception ex);
}

// IConfigService.cs
public interface IConfigService
{
    Task<Config> LoadConfigAsync();
    Task SaveConfigAsync(Config config);
    Task<Profile?> GetActiveProfileAsync();
    Task SwitchProfileAsync(string profileName);
    Task CreateProfileAsync(Profile profile);
    Task DeleteProfileAsync(string profileName);
    Task UpdatePreferencesAsync(string key, string value);
}

// IRedmineApiClient.cs
public interface IRedmineApiClient
{
    Task<IssuesResponse> GetIssuesAsync(
        int? assignedToId = null,
        int? projectId = null,
        string? status = null,
        int? limit = null,
        int? offset = null,
        CancellationToken cancellationToken = default);
    
    Task<List<Issue>> GetIssuesAsync(IssueFilter filter, CancellationToken cancellationToken = default);
    
    Task<User> GetCurrentUserAsync(CancellationToken cancellationToken = default);
        
    Task<Issue> GetIssueAsync(int id, CancellationToken cancellationToken = default);
    
    Task<Issue> GetIssueAsync(int id, bool includeJournals, CancellationToken cancellationToken = default);
    
    Task<Issue> CreateIssueAsync(Issue issue, CancellationToken cancellationToken = default);
    
    Task<Issue> UpdateIssueAsync(int id, Issue issue, CancellationToken cancellationToken = default);
    
    Task<List<Project>> GetProjectsAsync(CancellationToken cancellationToken = default);
    
    Task<List<User>> GetUsersAsync(CancellationToken cancellationToken = default);
    
    Task<List<IssueStatus>> GetIssueStatusesAsync(CancellationToken cancellationToken = default);
    
    Task AddCommentAsync(int issueId, string comment, CancellationToken cancellationToken = default);
    
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);
    
    Task<bool> TestConnectionAsync(string url, string apiKey, CancellationToken cancellationToken = default);
    
    Task<Attachment> GetAttachmentAsync(int id, CancellationToken cancellationToken = default);
    
    Task<Stream> DownloadAttachmentAsync(int id, CancellationToken cancellationToken = default);
    
    Task<Stream> DownloadAttachmentAsync(string contentUrl, CancellationToken cancellationToken = default);
}

// ITableFormatter.cs
public interface ITableFormatter
{
    void FormatIssues(List<Issue> issues);
    void FormatIssueDetails(Issue issue);
    void FormatAttachments(List<Attachment> attachments);
    void SetTimeFormat(TimeFormat format);
}

// IJsonFormatter.cs
public interface IJsonFormatter
{
    void FormatIssues(List<Issue> issues);
    void FormatIssueDetails(Issue issue);
    void FormatAttachments(List<Attachment> attachments);
    void FormatObject<T>(T obj);
}

// ITimeHelper.cs
public interface ITimeHelper
{
    string GetRelativeTime(DateTime utcTime);
    string GetLocalTime(DateTime utcTime, string format = "yyyy-MM-dd HH:mm");
    string GetUtcTime(DateTime utcTime, string format = "yyyy-MM-dd HH:mm:ss");
    DateTime ConvertToLocalTime(DateTime utcTime);
    string FormatTime(DateTime dateTime, TimeFormat format);
}

// TimeFormat.cs
public enum TimeFormat
{
    Relative,    // "about 2 hours ago"
    Absolute,    // Local time "2025-01-22 09:30"
    Utc          // UTC time "2025-01-22 00:30 UTC"
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

## データフロー

### 認証フロー
```
ユーザー入力
    ↓
System.CommandLine (コマンド解析)
    ↓
AuthCommand (認証コマンドハンドラー)
    ↓
ConfigService (設定管理)
    ↓
RedmineApiClient (接続テスト)
    ↓
プロファイル保存 (成功時) / エラー表示 (失敗時)
```

### チケット操作フロー
```
コマンド入力 → 認証確認 → API呼び出し → レスポンス処理 → フォーマット → 出力
                    ↓                          ↓
                プロファイル             エラーハンドリング
                  読み込み               (リトライ/エラー表示)
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

## 実装状況の注記

### 未実装の機能
1. **拡張機能システム**: ExtensionExecutorと関連機能は未実装
2. **RedmineService**: ビジネスロジック層は省略され、コマンドが直接APIClientを使用

### 追加実装
1. **検索機能**: `--search`オプションによるチケット検索
2. **エラーメッセージサービス**: ユーザーフレンドリーなエラーメッセージの提供

## テスト戦略

### テストレベル
1. **単体テスト**: 各コンポーネントの個別機能
2. **統合テスト**: API通信のモック、設定ファイルの読み書き
3. **E2Eテスト**: 実際のコマンド実行シナリオ

### テスト対象
- コマンドパーサーの正確性
- API通信のエラーハンドリング
- 設定ファイルの互換性（暗号化/復号化を含む）
- 出力フォーマットの正確性
- ファイルシステム操作（System.IO.Abstractionsを使用）

### .NETテストツール
- **xUnit**: 単体テストフレームワーク
- **NSubstitute**: シンプルで使いやすいモッキングライブラリ（AOT対応）
- **System.IO.Abstractions.TestingHelpers**: ファイルシステムのモック
- **FluentAssertions**: 読みやすいアサーション
- **WireMock.Net**: HTTPモックサーバー（統合テスト用）
- **Coverlet**: コードカバレッジ測定

## UI/UXデザイン

### コマンド体系
- 動詞-名詞の構造（`redmine issue list`）
- 一貫したオプション名（`--json`, `--limit`）
- ショートハンドオプションの体系的な提供
  - `-a` for `--assignee` （assignee）
  - `-s` for `--status` （status）
  - `-p` for `--project` （project）
  - `-L` for `--limit` （Limit - 大文字で`-l`との競合回避）
  - `-w` for `--web` （web）
  - `-t` for `--title` （title）
  - `-m` for `--message` （message）
- 特殊値の採用
  - `@me`: 現在の認証ユーザー
  - `all`: 全ての値（例: `--status all`）

### 出力デザイン
```
# テーブル形式の例（英語表示、デフォルト相対時刻）
ID     SUBJECT                           PRIORITY  STATUS        ASSIGNEE      PROJECT           DUE DATE      UPDATED
#1234  Implement login functionality     High      New           John Doe      Test Project      2024-12-31    about 2 hours ago
#1235  Review database design            Normal    In Progress   Jane Smith    Development       Not set       about 3 days ago

# テーブル形式の例（--absolute-time オプション使用時）
ID     SUBJECT                           PRIORITY  STATUS        ASSIGNEE      PROJECT           DUE DATE      UPDATED
#1234  Implement login functionality     High      New           John Doe      Test Project      2024-12-31    2024-01-15 14:30
#1235  Review database design            Normal    In Progress   Jane Smith    Development       Not set       2024-01-12 09:15

# 成功メッセージ
✓ Issue #1234 created successfully
View it at: https://redmine.example.com/issues/1234

# チケット詳細表示の例（issue view）
╭────────────── Issue #1234 ──────────────╮
│ Implement login functionality           │
╰─────────────────────────────────────────╯

Status:     New
Priority:   Normal
Assignee:   John Doe
Project:    Test Project
Progress:   50%
Created:    2024-01-01 10:00
Updated:    2024-01-02 15:30

Description:
This is a detailed description of the issue...

History:
#1 - Jane Smith - 2024-01-02 14:00
  Changed status_id from 'New' to 'In Progress'
  Updated the status

# エラーメッセージ
✗ Error: Authentication failed
Please run 'redmine auth login' to set up your credentials

# バージョン情報（--version）
redmine v0.8.0 (RedmineCLI)
Built with .NET 9.0 (Native AOT)

# ライセンス情報（--license）
RedmineCLI - MIT License
Copyright (c) 2025 Arstella ltd.

Third-party Dependencies:
- System.CommandLine v2.0.0-beta6.25358.103 (MIT)
- Spectre.Console v0.50.0 (MIT)
- VYaml v1.2.0 (MIT)
- StbImageSharp v2.30.15 (Public Domain)
- System.IO.Abstractions v22.0.15 (MIT)
- System.Security.Cryptography.ProtectedData v9.0.7 (MIT)
- Microsoft.Extensions.Http v9.0.7 (MIT)
- Polly.Extensions.Http v3.0.0 (BSD-3-Clause)
- Microsoft.Extensions.DependencyInjection v9.0.7 (MIT)
- Microsoft.Extensions.Configuration v9.0.7 (MIT)
- Microsoft.Extensions.Configuration.Binder v9.0.7 (MIT)
- Microsoft.Extensions.Logging v9.0.7 (MIT)
- Microsoft.Extensions.Logging.Console v9.0.7 (MIT)
- System.Text.Json v9.0.7 (MIT)

See THIRD-PARTY-NOTICES.txt for full license texts.
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
- Native AOTコンパイルによるJIT不要の即時起動（実測値: 15ms）
- 遅延初期化による起動高速化
- 必要最小限のモジュールのみロード
- 設定ファイルの効率的な読み込み
- ランタイム依存の排除による軽量化（バイナリサイズ: 約7MB、要件の10MB以下を達成）

## CI/CDアーキテクチャ

### GitHub Actions構成
- **build-and-test.yml**: PRとプッシュ時の自動テストとビルド
  - マルチプラットフォームテスト（Windows、macOS、Linux）
  - Native AOTビルドの検証（全プラットフォーム）
  - コードカバレッジの収集とレポート
  - パフォーマンスベンチマーク（起動時間測定）
- **release.yml**: タグプッシュ時の自動リリース
  - マルチプラットフォームバイナリの生成
  - 自動リリースノートの作成
  - Homebrew formulaの更新（安定版のみ）

### ビルドマトリックス
- **Windows x64**: `win-x64`（Windows Server）
- **macOS x64**: `osx-x64`（macOS）
- **macOS ARM64**: `osx-arm64`（macOS）
- **Linux x64**: `linux-x64`（Ubuntu）
- **Linux ARM64**: `linux-arm64`（Ubuntu ARM64ネイティブランナー）

### Native AOT最適化設定
- **StripSymbols=true**: シンボル情報を削除してバイナリサイズを削減
- **OptimizationPreference=Size**: サイズ最適化を優先
- **InvariantGlobalization=false**: ローカライゼーション機能を保持
- **IlcGenerateStackTraceData=false**: スタックトレース情報を削除

### Linux ARM64ビルドの特別対応
- GitHub Actions ARM64ランナー（`ubuntu-24.04-arm`）を使用したネイティブビルド
- クロスコンパイル不要による安定性とパフォーマンスの向上
- StripSymbolsを有効化してバイナリサイズを統一（約7MB）

## 拡張機能システム

### 概要
RedmineCLIは、GitHub CLIの拡張機能モデルに基づいた拡張機能システムを提供します。拡張機能は独立した実行ファイルとして実装され、RedmineCLI本体とはプロセス分離により疎結合を実現します。

### アーキテクチャ
```
┌──────────────────┐
│ RedmineCLI本体   │
├──────────────────┤
│ Command Parser   │ ← サブコマンドを拡張機能として認識
├──────────────────┤
│ Extension Loader │ ← 拡張機能の検索と実行
├──────────────────┤
│ Process Manager  │ ← 子プロセスとして拡張機能を起動
└──────────────────┘
         ↓ 環境変数経由で情報伝達
┌──────────────────┐
│ 拡張機能         │
│ (redmine-forum)  │ ← 独立したNative AOT実行ファイル
└──────────────────┘
```

### 拡張機能の検出と実行フロー
1. **コマンド解析**: `redmine forum list` → `redmine-forum` を検索
2. **検索パス**:
   - `~/.local/share/redmine/extensions/` (Linux/macOS)
   - `%LOCALAPPDATA%\redmine\extensions\` (Windows)
   - RedmineCLI実行ファイルと同じディレクトリ
   - PATH環境変数
3. **環境変数設定**: REDMINE_URL、REDMINE_API_KEY等を設定
4. **プロセス起動**: 引数を渡して拡張機能を実行
5. **結果処理**: 標準出力/エラー出力をそのまま表示

### 拡張機能への環境変数
```csharp
// ExtensionExecutor.cs
public class ExtensionExecutor
{
    private readonly IConfigService _configService;
    
    public async Task<int> ExecuteAsync(string extensionName, string[] args)
    {
        var profile = await _configService.GetActiveProfileAsync();
        var preferences = await _configService.GetPreferencesAsync();
        
        var env = new Dictionary<string, string>
        {
            ["REDMINE_URL"] = profile.Url,
            ["REDMINE_API_KEY"] = profile.ApiKey,
            ["REDMINE_USER"] = profile.Username,
            ["REDMINE_PROJECT"] = profile.DefaultProject ?? "",
            ["REDMINE_CONFIG_DIR"] = _configService.GetConfigDirectory(),
            ["REDMINE_OUTPUT_FORMAT"] = preferences.DefaultFormat,
            ["REDMINE_TIME_FORMAT"] = preferences.Time.Format
        };
        
        var processInfo = new ProcessStartInfo
        {
            FileName = $"redmine-{extensionName}",
            Arguments = string.Join(" ", args),
            UseShellExecute = false
        };
        
        foreach (var (key, value) in env)
            processInfo.Environment[key] = value;
            
        using var process = Process.Start(processInfo);
        await process.WaitForExitAsync();
        return process.ExitCode;
    }
}
```

### 拡張機能のNative AOT対応
拡張機能もRedmineCLI本体と同様にNative AOTでコンパイルすることを推奨：

```xml
<!-- 拡張機能のプロジェクトファイル -->
<PropertyGroup>
    <PublishAot>true</PublishAot>
    <StripSymbols>true</StripSymbols>
    <InvariantGlobalization>true</InvariantGlobalization>
    <JsonSerializerIsReflectionEnabledByDefault>false</JsonSerializerIsReflectionEnabledByDefault>
</PropertyGroup>
```

### セキュリティとプロセス分離
- **プロセス分離**: 各拡張機能は独立したプロセスで実行
- **権限分離**: 拡張機能はRedmineCLI本体とは異なる権限で実行可能
- **環境変数のみ**: APIキー等の機密情報は環境変数経由でのみ渡される
- **標準入出力**: 通信は標準入出力のみ（ソケットやIPCなし）

### 拡張機能の開発ガイドライン
1. **命名規則**: 実行ファイル名は `redmine-<name>` 形式
2. **引数処理**: System.CommandLineまたは同等のライブラリを使用
3. **エラー処理**: 適切な終了コードを返す（0=成功、非0=エラー）
4. **JSON出力**: REDMINE_OUTPUT_FORMAT=jsonの場合はJSON形式で出力
5. **AOT互換性**: リフレクション最小化、Source Generator使用