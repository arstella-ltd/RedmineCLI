# プロジェクト構造

## ルートディレクトリ構成

```
RedmineCLI/
├── RedmineCLI.sln                    # ソリューションファイル
├── README.md                         # プロジェクトの概要とクイックスタート
├── LICENSE                           # プロジェクトのライセンス（MIT）
├── THIRD-PARTY-NOTICES.txt           # サードパーティライブラリのライセンス
├── .gitignore                        # Git除外設定
├── .editorconfig                     # エディタ設定
├── global.json                       # .NET SDKバージョン指定
├── Directory.Build.props             # 共通ビルド設定（未作成）
├── CLAUDE.md                         # Claude Code向けガイドライン
├── RedmineCLI/                       # メインプロジェクト
├── RedmineCLI.Tests/                 # 単体テストプロジェクト
├── RedmineCLI.IntegrationTests/      # 統合テストプロジェクト
├── redmine-example/                   # サンプル拡張機能
├── scripts/                          # ビルドやリリース用スクリプト
├── docs/                             # 詳細なドキュメント
│   ├── PRODUCT.md                    # 製品概要書
│   ├── REQUIREMENTS.md               # 要求定義書
│   ├── TECH.md                       # 技術仕様書
│   ├── DESIGN.md                     # 設計書
│   ├── STRUCTURE.md                  # プロジェクト構造書
│   ├── TODO.md                       # 実装計画書
│   ├── RULES.md                      # ドキュメント作成ルール
│   └── EXTENSION.md                  # 拡張機能開発ガイド
└── samples/                          # 使用例とサンプル設定
```

## プロジェクト構成

```
RedmineCLI/                           # メインプロジェクトディレクトリ
├── RedmineCLI.csproj                 # プロジェクトファイル
├── Program.cs                        # エントリーポイント
├── Commands/                         # コマンド実装
│   ├── AuthCommand.cs                # 認証コマンド
│   ├── IssueCommand.cs               # チケットコマンド（list、view、create、edit、comment、attachment サブコマンド含む）
│   ├── AttachmentCommand.cs          # 添付ファイルコマンド（download、view）
│   ├── ConfigCommand.cs              # 設定コマンド
│   ├── LlmsCommand.cs                # AIエージェント向け情報出力コマンド
│   ├── UserCommand.cs                # ユーザーコマンド（list）
│   ├── ProjectCommand.cs             # プロジェクトコマンド（list）
│   └── StatusCommand.cs              # ステータスコマンド（list）
├── Services/                         # サービス層
│   ├── IRedmineService.cs            # Redmineサービスインターフェース
│   ├── RedmineService.cs             # Redmineサービス実装（今後実装）
│   ├── IConfigService.cs             # 設定サービスインターフェース
│   └── ConfigService.cs              # 設定サービス実装
├── Models/                           # データモデル
│   ├── Issue.cs                      # チケットモデル
│   ├── IssueFilter.cs                # チケット検索フィルタモデル
│   ├── IssueStatus.cs                # チケットステータスモデル
│   ├── Priority.cs                   # 優先度モデル
│   ├── Project.cs                    # プロジェクトモデル
│   ├── User.cs                       # ユーザーモデル
│   ├── Journal.cs                    # チケット履歴モデル
│   ├── Attachment.cs                 # 添付ファイルモデル
│   ├── AttachmentResponse.cs         # 添付ファイルAPIレスポンスモデル
│   ├── Config.cs                     # 設定モデル
│   ├── Profile.cs                    # プロファイルモデル
│   ├── Preferences.cs                # ユーザー設定モデル
│   ├── TimeFormat.cs                 # 時刻表示形式列挙型
│   ├── TimeSettings.cs               # 時刻設定モデル
│   └── LicenseInfo.cs                # ライセンス情報モデル
├── ApiClient/                        # API通信
│   ├── IRedmineApiClient.cs          # APIクライアントインターフェース
│   ├── RedmineApiClient.cs           # APIクライアント実装
│   ├── ApiResponses.cs               # APIレスポンスラッパー
│   ├── JsonSerializerContext.cs      # AOT対応JSONコンテキスト
│   └── DateTimeConverter.cs          # 日時変換カスタムコンバーター
├── Exceptions/                       # カスタム例外
│   ├── ValidationException.cs        # バリデーション例外
│   └── RedmineApiException.cs        # Redmine API例外
├── Formatters/                       # 出力フォーマッター
│   ├── ITableFormatter.cs            # テーブルフォーマッターインターフェース
│   ├── IJsonFormatter.cs             # JSONフォーマッターインターフェース
│   ├── TableFormatter.cs             # テーブル形式出力実装
│   └── JsonFormatter.cs              # JSON形式出力実装
├── Utils/                            # ユーティリティ
│   ├── ITimeHelper.cs                # 時刻変換インターフェース
│   ├── TimeHelper.cs                 # 時刻変換と相対表示実装
│   ├── ILicenseHelper.cs             # ライセンス情報管理インターフェース
│   ├── LicenseHelper.cs              # ライセンス情報管理実装
│   ├── ImageReferenceDetector.cs     # 画像参照検出ユーティリティ
│   ├── TerminalCapabilityDetector.cs # ターミナル機能検出ユーティリティ
│   └── SixelImageRenderer.cs         # Sixelプロトコル画像レンダリング
├── Extensions/                       # 拡張機能サポート
│   ├── IExtensionExecutor.cs         # 拡張機能実行インターフェース
│   └── ExtensionExecutor.cs          # 拡張機能実行実装
└── Resources/                        # 埋め込みリソース（未実装）
    └── THIRD-PARTY-NOTICES.txt       # ビルド時埋め込み用

RedmineCLI.Tests/                     # 単体テストプロジェクト
├── RedmineCLI.Tests.csproj
├── ApiClient/
│   ├── RedmineApiClientTests.cs      # APIクライアントのテスト  
│   ├── SerializationTests.cs         # JSONシリアライゼーションテスト
│   └── ApiResponsesTests.cs          # APIレスポンスモデルのテスト
├── Models/
│   ├── ConfigTests.cs                # 設定モデルのテスト
│   ├── IssueTests.cs                 # Issueモデルのテスト
│   └── ModelTests.cs                 # その他モデルのテスト
├── Commands/
│   ├── AuthCommandTests.cs           # 認証コマンドのテスト
│   ├── IssueCommandTests.cs          # チケット一覧・詳細コマンドのテスト
│   ├── IssueCreateCommandTests.cs    # チケット作成コマンドのテスト
│   ├── IssueEditCommandTests.cs      # チケット編集コマンドのテスト（12テストケース）
│   ├── IssueCommentCommandTests.cs   # チケットコメントコマンドのテスト
│   ├── IssueAttachmentCommandTests.cs # チケット添付ファイルコマンドのテスト
│   ├── AttachmentCommandTests.cs     # 添付ファイルコマンドのテスト
│   ├── LlmsCommandTests.cs           # AIエージェント向け情報出力コマンドのテスト
│   ├── UserCommandTests.cs           # ユーザー一覧コマンドのテスト
│   ├── ProjectCommandTests.cs        # プロジェクト一覧コマンドのテスト
│   └── StatusCommandTests.cs         # ステータス一覧コマンドのテスト
├── Formatters/
│   ├── TableFormatterTests.cs        # テーブルフォーマッターのテスト
│   └── JsonFormatterTests.cs         # JSONフォーマッターのテスト
├── Services/
│   ├── ConfigServiceTests.cs         # 設定サービスのテスト
│   ├── ErrorMessageServiceTests.cs   # エラーメッセージサービスのテスト
│   ├── ExtensionExecutorTests.cs     # 拡張機能実行のテスト
│   └── RedmineServiceTests.cs        # Redmineサービスのテスト
├── Exceptions/
│   └── ExceptionTests.cs             # カスタム例外のテスト
├── Utils/
│   ├── TimeHelperTests.cs            # 時刻変換ユーティリティのテスト
│   ├── LicenseHelperTests.cs         # ライセンス情報管理のテスト
│   ├── ImageReferenceDetectorTests.cs # 画像参照検出のテスト
│   └── TerminalCapabilityDetectorTests.cs # ターミナル機能検出のテスト
└── ProgramTests.cs                   # メインプログラムのテスト

RedmineCLI.IntegrationTests/          # 統合テストプロジェクト
├── RedmineCLI.IntegrationTests.csproj
├── Scenarios/
│   ├── AuthenticationScenarios.cs
│   └── IssueManagementScenarios.cs
└── Fixtures/
    └── TestDataFixture.cs

redmine-example/                       # サンプル拡張機能プロジェクト
├── redmine-example.csproj            # Native AOT対応プロジェクトファイル
├── Program.cs                        # 拡張機能のエントリーポイント
└── README.md                         # 拡張機能の使用方法とビルド手順
```

## コンポーネントアーキテクチャ

### 階層構造
1. **Program.cs**: アプリケーションのエントリーポイント、DIコンテナ設定
2. **Commands層**: System.CommandLineを使用したコマンド定義
   - AuthCommand: 認証コマンド（実装済み）
   - IssueCommand: チケット管理（list、view、create、edit、comment、attachment サブコマンド実装済み）
   - AttachmentCommand: 添付ファイル管理（download、view実装済み）
   - ConfigCommand: 設定管理（実装済み）
   - LlmsCommand: AIエージェント向け情報出力（実装済み）
   - UserCommand: ユーザー一覧（list実装予定）
   - ProjectCommand: プロジェクト一覧（list実装予定）
   - StatusCommand: ステータス一覧（list実装予定）
3. **Services層**: ビジネスロジックの実装
   - ConfigService: 設定管理（実装済み）
   - RedmineService: チケット操作（今後実装）
4. **ApiClient層**: Redmine APIとの通信を抽象化（実装済み）
5. **Models層**: データ転送オブジェクト（DTO）
6. **Formatters層**: 出力の整形
7. **Utils層**: 共通ユーティリティ

### データフロー
```
User Input → CLI Parser → Command Handler → API Client → Redmine Server
                ↓                               ↓
            Config Manager                 Response Model
                ↓                               ↓
            Formatter ← ← ← ← ← ← ← ← ← ← ← ← ↓
                ↓
            Terminal Output
```

### 責務の分離
- **Commands/**: ユーザーインタラクションとコマンド定義
- **Services/**: ビジネスロジックとオーケストレーション
- **ApiClient/**: HTTP通信とデータ変換
- **Models/**: データ構造の定義
- **Formatters/**: プレゼンテーション層
- **Utils/**: 横断的関心事

## ファイル命名規則

### ソースファイル
- PascalCaseを使用（C#標準の命名規則）
- 機能を表す明確な名前を付ける
- インターフェースは `I` プレフィックスを付ける
- テストファイルは `*Tests.cs` の形式

### 設定ファイル
- YAMLファイルは `.yml` 拡張子を使用
- 環境別設定は `config.{env}.yml` の形式

### ドキュメント
- Markdownファイルは大文字で開始（README.md, CONTRIBUTING.md）
- 技術文書は小文字（architecture.md, api-reference.md）

### 例
```
# ソースファイル
IssueCommand.cs          # チケットコマンド実装
IssueCommandTests.cs     # チケットコマンドのテスト
IRedmineApiClient.cs     # APIクライアントインターフェース
RedmineApiClient.cs      # APIクライアント実装
TableFormatter.cs        # テーブル形式フォーマッター

# 設定ファイル
config.yml              # デフォルト設定
config.example.yml      # サンプル設定

# プロジェクトファイル
RedmineCLI.csproj       # メインプロジェクト
RedmineCLI.Tests.csproj # テストプロジェクト
```

## インポート構成

### using文の順序（C#標準）
1. System名前空間
2. その他の.NET名前空間
3. サードパーティライブラリ
4. 自プロジェクトの名前空間

### グループ分け例
```csharp
// System名前空間
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Threading.Tasks;

// その他の.NET名前空間
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// サードパーティライブラリ
using Spectre.Console;
using VYaml.Annotations;
using VYaml.Serialization;

// 自プロジェクトの名前空間
using RedmineCLI.Commands;
using RedmineCLI.Models;
using RedmineCLI.Services;
```

## テスト構造

### テストファイル配置
```
# 単体テストは同じ階層の別プロジェクトで同じ構造を維持
RedmineCLI/Commands/IssueCommand.cs
RedmineCLI.Tests/Commands/IssueCommandTests.cs

# テストデータ配置
RedmineCLI.Tests/
├── TestData/
│   ├── ApiResponses/       # APIレスポンスのモックJSON
│   │   ├── issues.json
│   │   └── projects.json
│   ├── Configs/            # テスト用設定ファイル
│   │   └── test-config.yml
│   └── Expected/           # 期待される出力データ
│       └── formatted-output.txt
└── Fixtures/
    └── TestDataFixture.cs

RedmineCLI.IntegrationTests/
├── TestData/
│   └── IntegrationConfigs/
│       └── integration-test-config.yml
└── Fixtures/
    └── IntegrationTestFixture.cs
```

### 命名規則
- テストクラス: `{ClassName}Tests`
- テストメソッド: `{MethodName}_Should{ExpectedBehavior}_When{Condition}`
- 例: `GetIssues_ShouldReturnFilteredList_WhenStatusIsSpecified`
- テストデータ: `Arrange-Act-Assert` パターンを使用
- ヘルパーメソッド: `Create{ObjectName}`, `Setup{Scenario}`

## 拡張機能ディレクトリ

### 拡張機能の配置場所
拡張機能は以下の優先順位で検索されます：

1. **ユーザー拡張機能ディレクトリ**
   - Windows: `%LOCALAPPDATA%\redmine\extensions\`
   - macOS: `~/.local/share/redmine/extensions/`
   - Linux: `~/.local/share/redmine/extensions/`

2. **RedmineCLI実行ファイルと同じディレクトリ**
   - 開発時やポータブル版で便利

3. **PATH環境変数に含まれるディレクトリ**
   - システム全体で利用可能な拡張機能

### 拡張機能プロジェクトの構造例
```
RedmineCLI.Extension.Forum/              # 拡張機能プロジェクト
├── RedmineCLI.Extension.Forum.csproj    # プロジェクトファイル（Native AOT設定）
├── Program.cs                           # エントリーポイント
├── ForumExtension.cs                    # メインロジック
├── Models/                              # データモデル
│   ├── Forum.cs
│   └── ForumPost.cs
├── Services/                            # サービス層
│   └── ForumApiClient.cs
├── README.md                            # 拡張機能の説明
└── LICENSE                              # ライセンス

# ビルド後の配置例
~/.local/share/redmine/extensions/
├── redmine-forum                        # 実行ファイル（Native AOT）
├── redmine-report                       # 別の拡張機能
└── redmine-backup                       # さらに別の拡張機能
```