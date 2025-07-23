# 技術スタック

## コア技術

- **.NET 9**: 最新版を使用、クロスプラットフォーム対応、Native AOT対応
- **C# 13**: 最新の言語機能を活用（record型、パターンマッチング等）
- **Native AOT**: 高速起動（実測15ms）とネイティブバイナリ（実測15MB）を実現
- **System.CommandLine** v2.0.0-beta6: Microsoftが提供する最新のCLIフレームワーク
- **Spectre.Console** v0.50.0: 美しいコンソール出力のためのライブラリ（AOT対応）
- **VYaml** v1.2.0: Native AOT対応の高速YAMLライブラリ（Source Generator使用）
- **System.IO.Abstractions** v22.0.15: ファイルシステムの抽象化（テスタビリティ向上）
- **Redmine REST API v3.0+**: チケット情報の取得・更新に使用するAPIインターフェース
- **APIキー認証**: Redmineサーバーへの安全な認証方式として採用
- **設定ファイル形式**: YAML形式（ghコマンドと同様）で設定情報を管理
- **出力形式**: 人間が読みやすいテーブル形式とプログラムが処理しやすいJSON形式をサポート

## 主要NuGetパッケージ

- **System.CommandLine** v2.0.0-beta6.25358.103
- **Spectre.Console** v0.50.0
- **VYaml** v1.2.0（Native AOT対応、[YamlObject]属性でSource Generator使用）
- **Microsoft.Extensions.DependencyInjection** v9.0.0
- **Microsoft.Extensions.Http** v9.0.0
- **Microsoft.Extensions.Logging** v9.0.0
- **Polly.Extensions.Http** v3.0.0
- **System.Text.Json** v9.0.0（Source Generator対応）
- **System.Security.Cryptography.ProtectedData** v9.0.7（Windows DPAPI）
- **System.IO.Abstractions** v22.0.15

## 開発ツール

- **Visual Studio 2022 / VS Code**: 推奨IDE
- **dotnet CLI**: プロジェクト管理とビルドツール

### テスト駆動開発（TDD）ツール
- **xUnit** v2.9.2: 単体テストフレームワーク（Native AOT対応）
- **FluentAssertions** v6.12.0: 読みやすいアサーションライブラリ（Should()構文）
- **NSubstitute** v5.1.0: シンプルで使いやすいモッキングライブラリ（AOT対応）
- **System.IO.Abstractions.TestingHelpers** v22.0.15: ファイルシステムのモック
- **WireMock.Net** v1.6.6: HTTP APIのモックサーバー（統合テスト用）
- **Spectre.Console.Testing** v0.50.0: Spectre.Consoleの出力テスト用ライブラリ

### 品質管理ツール
- **coverlet.collector** v6.0.2: コードカバレッジ収集ツール
- **dotnet-format**: コードフォーマッター
- **SonarAnalyzer.CSharp**: 静的解析ツール

### TDD開発プロセス
本プロジェクトでは和田卓人（t-wada）氏が推奨するテスト駆動開発手法を採用しています。
1. **Red**: 失敗するテストを先に作成
2. **Green**: テストを通す最小限の実装
3. **Refactor**: コードの改善と最適化
- テスト命名規則: `{Method}_Should_{ExpectedBehavior}_When_{Condition}`
- AAA（Arrange-Act-Assert）パターンの徹底
- テストピラミッド（単体テスト70%、統合テスト20%、E2Eテスト10%）

## System.CommandLineの使用方法

### コマンド構造パターン
System.CommandLine v2.0.0-beta6を使用して、GitHub CLIライクなコマンド構造を実装しています。

```csharp
// 基本的なコマンド構造
var rootCommand = new RootCommand("RedmineCLI");
var authCommand = new Command("auth", "認証管理");
var loginCommand = new Command("login", "ログイン");

// コマンドの階層構造
rootCommand.Add(authCommand);
authCommand.Add(loginCommand);
```

### オプションと引数の定義
```csharp
// オプションの定義
var urlOption = new Option<string>("--url") { Description = "Redmine server URL" };
var apiKeyOption = new Option<string>("--api-key") { Description = "API key" };

// デフォルト値の設定
var profileOption = new Option<string>("--profile") 
{ 
    Description = "Profile name",
    DefaultValueFactory = _ => "default" 
};
```

### コマンドアクションの実装
```csharp
// SetActionメソッドでコマンドの動作を定義
loginCommand.SetAction(async (parseResult) =>
{
    var url = parseResult.GetValue(urlOption);
    var apiKey = parseResult.GetValue(apiKeyOption);
    // 実際の処理
});
```

### Native AOT対応の考慮事項
- リフレクションを最小限に抑える
- Source Generatorを活用（System.Text.Json、VYaml）
- ILLink.Descriptors.xmlで型情報を保護
- サードパーティライブラリの警告は必要に応じて抑制（IL2104, IL3053）
- TrimmerRootAssemblyでアセンブリ全体を保護
- DynamicDependency属性でコマンドハンドラーの型情報を保護
- JsonSerializerContextでJSON型のメタデータを事前生成
- フォーマッターインターフェースで出力処理を抽象化

## データ＆状態管理

- **設定ファイル**
  - Windows: `%APPDATA%\redmine\config.yml`
  - macOS/Linux: `~/.config/redmine/config.yml`
- **認証情報**
  - Windows: DPAPI（Data Protection API）を使用してAPIキーを暗号化
  - macOS/Linux: Base64エンコーディング（注：将来的により安全な方式への移行を検討）
- **キャッシュ**: 頻繁にアクセスするデータ（プロジェクト一覧など）の一時保存
- **エラーログ**: デバッグ用のログファイル（オプション）

## 共通コマンド

```bash
# 開発環境のセットアップ
dotnet new console -n RedmineCLI
dotnet add package System.CommandLine --prerelease
dotnet add package Spectre.Console
dotnet add package VYaml

# ビルドとテスト
dotnet build
dotnet test
dotnet run -- --help

# 発行（Native AOTによる最適化されたバイナリ）
# Windows向け
dotnet publish -c Release -r win-x64 -p:PublishAot=true -p:StripSymbols=true

# macOS向け 
dotnet publish -c Release -r osx-x64 -p:PublishAot=true -p:StripSymbols=true
dotnet publish -c Release -r osx-arm64 -p:PublishAot=true -p:StripSymbols=true

# Linux向け
dotnet publish -c Release -r linux-x64 -p:PublishAot=true -p:StripSymbols=true
dotnet publish -c Release -r linux-arm64 -p:PublishAot=true -p:StripSymbols=true

# 従来のシングルファイル発行（互換性のため）
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true

# グローバルツールとしてインストール
# 注: .csprojファイルで<PackageId>RedmineCLI</PackageId>と<ToolCommandName>redmine</ToolCommandName>を設定
# インストール後は 'redmine' コマンドとして使用可能
dotnet pack
dotnet tool install --global RedmineCLI

# 使用例
redmine auth login
redmine auth status
redmine auth logout

# チケット操作（ショートハンドオプション対応）
redmine issue list                              # プロジェクトの全オープンチケット（30件、相対時刻表示）
redmine issue list -a @me                       # 自分に割り当てられたチケット
redmine issue list --assignee john.doe          # 特定ユーザーのチケット（または -a john.doe）
redmine issue list --status closed              # クローズドチケット（または -s closed）
redmine issue list --status all                 # 全ステータスのチケット（または -s all）
redmine issue list --project myproject          # 特定プロジェクト（または -p myproject）
redmine issue list --limit 50                   # 表示件数指定（または -L 50）
redmine issue list --json                       # JSON形式で出力（ISO 8601 UTC時刻）
redmine issue list --absolute-time              # ローカル時刻で表示
redmine issue list -a @me -s open -p myproject # 複数条件の組み合わせ
redmine issue list --web                        # ブラウザで開く（または -w）
redmine issue list -a @me --web                # 条件付きでブラウザで開く

redmine issue view <ID> [--json] [--web]
redmine issue create [-p PROJECT] [-t TITLE] [--description DESC] [-a USER] [--web]
redmine issue edit <ID> [-s STATUS] [-a USER/@me] [--done-ratio N] [--web]  # オプション指定時
redmine issue edit <ID>                                                     # 対話的編集モード
redmine issue comment <ID> [-m MESSAGE]

# チケットコメント追加の詳細例
redmine issue comment 123                           # エディタで長文コメント作成
redmine issue comment 123 -m "作業完了しました"      # 直接コメント入力
redmine issue comment 456 --message "テスト結果OK"   # --messageでも指定可能

# 添付ファイル操作
redmine attachment download <ATTACHMENT-ID>                              # 添付ファイルをダウンロード
redmine attachment download 789 --output ~/Downloads/                    # 出力先を指定（または -o）
redmine attachment download 789 --force                                  # 既存ファイルを上書き（または -f）
redmine attachment view <ATTACHMENT-ID>                                  # 添付ファイルのメタデータ表示

# チケットの添付ファイル管理
redmine issue list-attachments <ISSUE-ID>                               # 添付ファイル一覧表示
redmine issue attachments <ISSUE-ID>                                     # list-attachmentsのエイリアス
redmine issue download-attachments <ISSUE-ID> --interactive             # 対話的に選択してダウンロード
redmine issue download-attachments <ISSUE-ID> --all                     # すべての添付ファイルをダウンロード
redmine issue download-attachments <ISSUE-ID> --all --output ./files/   # 出力先を指定して一括ダウンロード

# 設定管理
redmine config set <KEY> <VALUE>
redmine config get <KEY>
redmine config list

# 時刻表示設定の例
redmine config set time.format relative    # 相対時刻表示（デフォルト）
redmine config set time.format absolute    # ローカル時刻表示
redmine config set time.format utc         # UTC時刻表示
redmine config set time.timezone system    # システムのタイムゾーン使用（デフォルト）
redmine config set time.timezone "Asia/Tokyo"  # 特定のタイムゾーン指定

# ヘルプ
redmine --help
redmine <COMMAND> --help

# バージョンとライセンス情報
redmine --version
redmine --licenses
```

## ブラウザサポート

本ツールはCLIアプリケーションですが、--webオプションによりWebブラウザとの連携が可能です。

### CLI環境要件
- ターミナルエミュレータ: ANSI エスケープシーケンスをサポートする標準的なターミナル
- 文字エンコーディング: UTF-8対応
- OS: Windows, macOS, Linux（主要なディストリビューション）
- ネットワーク: HTTPS通信が可能な環境
- 言語: 英語のみ対応（ghコマンドと同様）

### --webオプション利用時の要件
- デフォルトブラウザが設定されていること
- ブラウザ起動コマンド
  - $BROWSER環境変数: 設定されている場合は最優先（%sプレースホルダ対応）
    - 例: `export BROWSER="firefox %s"`
    - 例: `export BROWSER="google-chrome --incognito %s"`
  - Windows: `start` コマンド（$BROWSER未設定時のフォールバック）
  - macOS: `open` コマンド（$BROWSER未設定時のフォールバック）
  - Linux: `xdg-open` コマンド（$BROWSER未設定時のフォールバック）
- Redmine WebUIへの別途ログインが必要（APIキー認証とは独立）