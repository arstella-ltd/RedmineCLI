# RedmineCLI Example Extension

これは、RedmineCLIの拡張機能システムを実演するサンプル拡張機能です。

## 概要

`redmine-example`は、以下を示します：
- 拡張機能の基本構造
- RedmineCLIから渡される環境変数の使用方法
- Native AOTでのコンパイル設定

## ビルド方法

### 開発用ビルド
```bash
dotnet build
```

### Native AOTでのパブリッシュ
```bash
# Windows
dotnet publish -c Release -r win-x64

# macOS (Intel)
dotnet publish -c Release -r osx-x64

# macOS (Apple Silicon)
dotnet publish -c Release -r osx-arm64

# Linux
dotnet publish -c Release -r linux-x64
```

## インストール

ビルドした実行ファイルを以下のいずれかの場所に配置します：

1. ユーザー拡張機能ディレクトリ
   - Windows: `%LOCALAPPDATA%\redmine\extensions\`
   - macOS/Linux: `~/.local/share/redmine/extensions/`

2. RedmineCLI実行ファイルと同じディレクトリ

3. PATH環境変数に含まれるディレクトリ

## 使用方法

```bash
# 拡張機能の情報を表示
redmine example info

# API接続テスト
redmine example test
```

## 利用可能な環境変数

RedmineCLIは以下の環境変数を拡張機能に渡します：

- `REDMINE_URL`: RedmineサーバーのURL
- `REDMINE_API_KEY`: API認証キー
- `REDMINE_USER`: 現在のユーザー名
- `REDMINE_PROJECT`: デフォルトプロジェクト
- `REDMINE_CONFIG_DIR`: 設定ディレクトリパス
- `REDMINE_TIME_FORMAT`: 時間表示形式（Relative/Absolute/Utc）
- `REDMINE_OUTPUT_FORMAT`: 出力形式（table/json）

## 拡張機能の作成

1. 新しい.NETプロジェクトを作成
2. 実行ファイル名を`redmine-<name>`形式に設定
3. Native AOT設定を追加（推奨）
4. 環境変数を使用してRedmine APIにアクセス

## セキュリティ考慮事項

- APIキーは環境変数経由でのみ渡されます
- ログやコンソール出力にAPIキーを含めないでください
- 信頼できるソースからのみ拡張機能をインストールしてください