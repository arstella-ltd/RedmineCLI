# RedmineCLI

Redmineチケットをコマンドラインから管理するための、GitHub CLI (`gh`) ライクなツールです。

## 特徴

- 🚀 **高速起動**: Native AOTコンパイルにより100ms以下での起動を実現
- 🎯 **シンプルな操作**: `gh`コマンドと同様の直感的なコマンド体系
- 🌍 **クロスプラットフォーム**: Windows、macOS、Linux対応
- 🔒 **セキュア**: APIキーによる安全な認証
- 📦 **軽量**: 10MB以下の単一実行ファイル（.NETランタイム不要）

## インストール

### バイナリから（推奨）

各プラットフォーム向けのバイナリをダウンロードして、パスの通った場所に配置してください。

```bash
# Linux/macOS
chmod +x redmine
sudo mv redmine /usr/local/bin/

# Windows
# redmine.exe をパスの通ったフォルダに配置
```

### ソースからビルド

```bash
git clone https://github.com/yourname/RedmineCLI.git
cd RedmineCLI
dotnet publish -c Release -r <RID> -p:PublishAot=true -p:StripSymbols=true
```

RID (Runtime Identifier)
- Windows: `win-x64`
- macOS: `osx-x64` または `osx-arm64`
- Linux: `linux-x64` または `linux-arm64`

## 使い方

### 認証

```bash
# Redmineサーバーに接続
redmine auth login

# 接続状態を確認
redmine auth status

# ログアウト
redmine auth logout
```

### チケット管理

```bash
# チケット一覧を表示（デフォルト：自分に割り当てられたもの）
redmine issue list

# 特定のステータスでフィルタ
redmine issue list --status=open

# チケットの詳細を表示
redmine issue view 12345

# 新しいチケットを作成
redmine issue create

# チケットを更新
redmine issue edit 12345 --status=closed
```

### 設定

```bash
# 設定値を変更
redmine config set default-project myproject

# 設定値を確認
redmine config get default-project

# すべての設定を表示
redmine config list
```

## 設定ファイル

設定ファイルは以下の場所に保存されます。

- Windows: `%APPDATA%\redmine\config.yml`
- macOS/Linux: `~/.config/redmine/config.yml`

## 開発

### 必要な環境

- .NET 9 SDK
- Visual Studio 2022 または VS Code

### ビルド

```bash
dotnet build
dotnet test
dotnet run -- --help
```

## ライセンス

[MIT License](LICENSE)