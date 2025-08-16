# RedmineCLI Board Extension

RedmineCLIのBoard拡張機能です。Redmineのボード管理機能を提供し、OSキーチェーン連携による安全な認証とWebスクレイピングによるボード一覧取得をサポートします。

## 機能

- OSキーチェーン連携による安全な認証（RedmineCLI本体の認証情報を使用）
- 自動セッション作成（フォームログイン）
- Webスクレイピングによるボード一覧取得
- プロジェクトごとのボード検索
- 詳細なデバッグログ出力
- RedmineCLI本体との完全な統合

## ビルド方法

```bash
# 通常のビルド
dotnet build

# Native AOTパブリッシュ
dotnet publish -c Release -r linux-x64 -p:PublishAot=true -p:StripSymbols=true
```

## 使用方法

### 認証方法

Board拡張機能は以下の方法で認証情報を取得します：

1. **環境変数から** (RedmineCLI本体から渡される場合)
   - `REDMINE_URL`: RedmineサーバーのURL
   - `REDMINE_API_KEY`: APIキー（オプション）
   - `REDMINE_CONFIG_DIR`: 設定ディレクトリ（オプション）

2. **OSキーチェーンから** (ユーザー名とパスワード)
   ```bash
   # 事前にRedmineCLI本体でパスワード認証情報を保存
   redmine auth login --save-password
   ```
   
   OSキーチェーンに保存されたユーザー名とパスワードを使用して、自動的にフォームログインを行いセッションを作成します。

3. **設定ファイルから** (`~/.config/redmine/config.yml`のURL情報)

### インストール

ビルドした実行ファイル`redmine-board`をPATHの通った場所に配置します。

### ボード一覧表示

```bash
# RedmineCLI本体経由で実行（環境変数でURLが渡される）
redmine board list

# スタンドアロンで実行（OSキーチェーンから認証）
redmine-board list

# 特定のプロジェクトのボードのみ表示
redmine-board list --project myproject

# 別のRedmineサーバーを指定
redmine-board list --url https://other-redmine.example.com

# 環境変数でURLを指定して実行（認証情報はOSキーチェーンから）
REDMINE_URL=https://redmine.example.com redmine-board list
```

### 情報表示

```bash
redmine board info
```

## デバッグ

この拡張機能は詳細なデバッグログを出力します。ログレベルはDebugに設定されており、以下の情報が出力されます：

- OSキーチェーンからの認証情報取得
- 自動フォームログインプロセス
- HTTPリクエスト/レスポンスの詳細
- 認証トークンとセッションクッキーの取得状況
- ボードのWebスクレイピング詳細
- 環境変数と設定ファイルの状態

## 今後の実装予定

- ボード詳細表示
- ボード作成/編集/削除
- カード管理機能（作成、移動、削除）
- APIキーの自動生成（ユーザー名/パスワードから）
- オフラインモード（キャッシュ機能）