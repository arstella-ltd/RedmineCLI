# RedmineCLI

Redmineチケットをコマンドラインから管理するための、GitHub CLI (`gh`) ライクなツールです。

## 特徴

- 🚀 **高速起動**: Native AOTコンパイルにより100ms以下での起動を実現
- 🎯 **シンプルな操作**: `gh`コマンドと同様の直感的なコマンド体系
- 🌍 **クロスプラットフォーム**: Windows、macOS、Linux対応
- 🔒 **セキュア**: APIキーによる安全な認証
- 📦 **軽量**: 10MB以下の単一実行ファイル（.NETランタイム不要）
- 🖼️ **画像表示**: Sixelプロトコル対応ターミナルでの画像インライン表示

## インストール

### Homebrew (macOS/Linux)

```bash
brew tap arstella-ltd/homebrew-tap
brew install redmine
```

### Scoop (Windows)

```bash
# Scoopバケットを追加
scoop bucket add arstella https://github.com/arstella-ltd/scoop-bucket

# RedmineCLIをインストール
scoop install redmine
```

### mise

[mise](https://mise.jdx.dev/)はasdfプラグインと互換性のある高速なランタイムバージョン管理ツールです。

```bash
# プラグインを追加してインストール
mise plugin add redmine https://github.com/arstella-ltd/asdf-redmine.git
mise install redmine@latest
mise use -g redmine@latest

# または.mise.tomlに記載
[tools]
redmine = "latest"
```

### バイナリから

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

# プロジェクトでフィルタ（プロジェクト名、識別子、IDが使用可能）
redmine issue list --project "管理"
redmine issue list -p my-project

# 優先度でフィルタ（優先度名またはIDが使用可能）
redmine issue list --priority "高"
redmine issue list --priority 5

# キーワードでチケットを検索
redmine issue list --search "会議"
redmine issue list -q "バグ修正"

# 複合検索（検索と他のフィルターの組み合わせ）
redmine issue list --search "会議" --status open --assignee @me --project "開発"

# 複数のフィルターを組み合わせ
redmine issue list --status open --priority "高" --assignee @me

# チケットの詳細を表示
redmine issue view 12345

# チケットの詳細を表示（全てのコメントを表示）
redmine issue view 12345 --comments
# または
redmine issue view 12345 -c

# 新しいチケットを作成
redmine issue create

# チケットを更新
redmine issue edit 12345 --status=closed

# チケットのタイトルを更新
redmine issue edit 12345 --title "新しいタイトル"
redmine issue edit 12345 -t "新しいタイトル"

# チケットの説明を更新
redmine issue edit 12345 --body "新しい説明文"
redmine issue edit 12345 -b "新しい説明文"

# ファイルから説明を読み込んで更新
redmine issue edit 12345 --body-file description.md
redmine issue edit 12345 -F description.md

# 標準入力から説明を読み込んで更新
echo "新しい説明文" | redmine issue edit 12345 -F -

# 複数のフィールドを同時に更新
redmine issue edit 12345 --status=resolved --add-assignee=@me --body "問題を解決しました"

# 担当者を設定
redmine issue edit 12345 --add-assignee=@me
redmine issue edit 12345 --add-assignee=john.doe

# 担当者を削除
redmine issue edit 12345 --remove-assignee

# コメントを追加
redmine issue comment 12345 --message "作業を開始しました"
redmine issue comment 12345 -m "確認しました"

# チケットの説明欄を更新（コメント追加なし）
redmine issue comment 12345 --body "更新された説明文"
redmine issue comment 12345 -b "更新された説明文"

# ファイルから説明欄を読み込んで更新
redmine issue comment 12345 --body-file description.md
redmine issue comment 12345 -F description.md

# 標準入力から説明欄を読み込んで更新
echo "新しい説明" | redmine issue comment 12345 --body-file -
cat description.md | redmine issue comment 12345 -F -

# コメント追加と説明欄更新を同時に実行
redmine issue comment 12345 -m "説明を更新しました" --body "新しい説明文"
```

### 優先度管理

```bash
# 優先度一覧を表示
redmine priority list

# JSON形式で出力
redmine priority list --json
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

## Sixelプロトコルサポート

RedmineCLIは、Sixelプロトコル対応ターミナルでチケットに添付された画像をインライン表示できます。

### 対応ターミナル

- Windows Terminal (v1.22以降)
- iTerm2
- WezTerm
- mlterm
- xterm (sixel有効化時)

### 有効化方法

Sixelサポートは自動的に検出されますが、手動で有効化する場合は以下の環境変数を設定してください：

```bash
export SIXEL_SUPPORT=1
```

## 設定ファイル

設定ファイルは以下の場所に保存されます。

- Windows: `%APPDATA%\redmine\config.yml`
- macOS/Linux: `~/.config/redmine/config.yml`

## その他のコマンド

```bash
# ヘルプを表示
redmine --help

# バージョンとライセンス情報を表示
redmine --version
redmine --license

# 詳細なエラー情報（スタックトレース）を表示
redmine --verbose <command>

# AIエージェント向けの使用方法情報を出力
redmine llms
```

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