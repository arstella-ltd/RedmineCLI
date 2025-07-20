# プロジェクト構造

## ルートディレクトリ構成

```
RedmineCLI/
├── RedmineCLI.sln                    # ソリューションファイル
├── README.md                         # プロジェクトの概要とクイックスタート
├── LICENSE                           # ライセンスファイル
├── .gitignore                        # Git除外設定
├── .editorconfig                     # エディタ設定
├── global.json                       # .NET SDKバージョン指定
├── Directory.Build.props             # 共通ビルド設定
├── CLAUDE.md                         # Claude Code向けガイドライン
├── RedmineCLI/                       # メインプロジェクト
├── RedmineCLI.Tests/                 # 単体テストプロジェクト
├── RedmineCLI.IntegrationTests/      # 統合テストプロジェクト
├── scripts/                          # ビルドやリリース用スクリプト
├── docs/                             # 詳細なドキュメント
└── samples/                          # 使用例とサンプル設定
```

## プロジェクト構成

```
RedmineCLI/                           # メインプロジェクトディレクトリ
├── RedmineCLI.csproj                 # プロジェクトファイル
├── Program.cs                        # エントリーポイント
├── Commands/                         # コマンド実装
│   ├── AuthCommand.cs                # 認証コマンド
│   ├── IssueCommand.cs               # チケットコマンド
│   ├── ConfigCommand.cs              # 設定コマンド
│   └── RootCommand.cs                # ルートコマンド
├── Services/                         # サービス層
│   ├── IRedmineService.cs            # Redmineサービスインターフェース
│   ├── RedmineService.cs             # Redmineサービス実装
│   ├── IConfigService.cs             # 設定サービスインターフェース
│   └── ConfigService.cs              # 設定サービス実装
├── Models/                           # データモデル
│   ├── Issue.cs                      # チケットモデル
│   ├── Project.cs                    # プロジェクトモデル
│   ├── User.cs                       # ユーザーモデル
│   └── Config.cs                     # 設定モデル
├── ApiClient/                        # API通信
│   ├── IRedmineApiClient.cs          # APIクライアントインターフェース
│   ├── RedmineApiClient.cs           # APIクライアント実装
│   └── ApiException.cs               # API例外定義
├── Formatters/                       # 出力フォーマッター
│   ├── IOutputFormatter.cs           # フォーマッターインターフェース
│   ├── TableFormatter.cs             # テーブル形式
│   └── JsonFormatter.cs              # JSON形式
└── Utils/                            # ユーティリティ
    ├── ConsoleHelper.cs              # コンソール補助
    └── CryptoHelper.cs               # 暗号化処理

RedmineCLI.Tests/                     # 単体テストプロジェクト
├── RedmineCLI.Tests.csproj
├── Commands/
│   ├── AuthCommandTests.cs
│   └── IssueCommandTests.cs
├── Services/
│   └── RedmineServiceTests.cs
└── ApiClient/
    └── RedmineApiClientTests.cs

RedmineCLI.IntegrationTests/          # 統合テストプロジェクト
├── RedmineCLI.IntegrationTests.csproj
├── Scenarios/
│   ├── AuthenticationScenarios.cs
│   └── IssueManagementScenarios.cs
└── Fixtures/
    └── TestDataFixture.cs
```

## コンポーネントアーキテクチャ

### 階層構造
1. **Program.cs**: アプリケーションのエントリーポイント、DIコンテナ設定
2. **Commands層**: System.CommandLineを使用したコマンド定義
3. **Services層**: ビジネスロジックの実装
4. **ApiClient層**: Redmine APIとの通信を抽象化
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
using YamlDotNet.Serialization;

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