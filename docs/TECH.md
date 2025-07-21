# 技術スタック

## コア技術

- **.NET 9**: 最新版を使用、クロスプラットフォーム対応、Native AOT対応
- **C# 13**: 最新の言語機能を活用（record型、パターンマッチング等）
- **Native AOT**: 高速起動（実測7ms）と小サイズバイナリ（実測4.9MB）を実現
- **System.CommandLine** v2.0.0-beta5: Microsoftが提供する最新のCLIフレームワーク
- **Spectre.Console** v0.49.1: 美しいコンソール出力のためのライブラリ（AOT対応）
- **VYaml** v0.29.0: Native AOT対応の高速YAMLライブラリ（Source Generator使用）
- **System.IO.Abstractions** v22.0.15: ファイルシステムの抽象化（テスタビリティ向上）
- **Redmine REST API v3.0+**: チケット情報の取得・更新に使用するAPIインターフェース
- **APIキー認証**: Redmineサーバーへの安全な認証方式として採用
- **設定ファイル形式**: YAML形式（ghコマンドと同様）で設定情報を管理
- **出力形式**: 人間が読みやすいテーブル形式とプログラムが処理しやすいJSON形式をサポート

## 主要NuGetパッケージ

- **System.CommandLine** v2.0.0-beta5.25277.114
- **Spectre.Console** v0.49.1
- **VYaml** v0.29.0（Native AOT対応）
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
- **xUnit** v2.9.2: 単体テストフレームワーク
- **FluentAssertions** v6.12.0: 読みやすいアサーションライブラリ
- **NSubstitute** v5.1.0: シンプルで使いやすいモッキングライブラリ（AOT対応）
- **System.IO.Abstractions.TestingHelpers** v22.0.15: ファイルシステムのモック
- **coverlet.collector** v6.0.2: コードカバレッジ収集ツール
- **dotnet-format**: コードフォーマッター
- **SonarAnalyzer.CSharp**: 静的解析ツール
- **VYaml**: Native AOT対応のYAMLライブラリ（設定ファイル用）
- **System.Text.Json**: 高速なJSON処理（Source Generator使用）

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
dotnet pack
dotnet tool install --global RedmineCLI

# 使用例
redmine auth login
redmine auth status
redmine auth logout

# チケット操作
redmine issue list [--assignee=USER] [--status=STATUS] [--project=PROJECT] [--limit=N] [--json]
redmine issue view <ID> [--json]
redmine issue create [--project=PROJECT] [--title=TITLE] [--description=DESC]
redmine issue edit <ID> [--status=STATUS] [--assignee=USER] [--done-ratio=N]
redmine issue comment <ID> [--message=MESSAGE]

# 設定管理
redmine config set <KEY> <VALUE>
redmine config get <KEY>
redmine config list

# ヘルプ
redmine --help
redmine <COMMAND> --help
```

## ブラウザサポート

本ツールはCLIアプリケーションのため、ブラウザサポートは不要です。
ただし、以下の環境要件があります。

- ターミナルエミュレータ: ANSI エスケープシーケンスをサポートする標準的なターミナル
- 文字エンコーディング: UTF-8対応
- OS: Windows, macOS, Linux（主要なディストリビューション）
- ネットワーク: HTTPS通信が可能な環境
- 言語: 英語のみ対応（ghコマンドと同様）