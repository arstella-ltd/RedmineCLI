# 要求定義書

## はじめに

redmine-cliは、Redmineのチケット管理をコマンドラインから効率的に行うためのツールです。主な技術要素として、Redmine REST API（v3.0以上）との通信、APIキーベースの認証、ghコマンドライクなインターフェースを採用します。ターゲットユーザーは、ターミナルでの作業を好む開発者やエンジニアであり、GUIを使わずに素早くチケット操作を行いたいというニーズに応えます。

## 要求一覧

### 要求 1
**ユーザーストーリー:** 開発者として、APIキーを使った認証を簡単に設定したいので、Redmineサーバーに安全に接続できる

#### 受け入れ基準
1. WHEN `redmine auth login` コマンドを実行 THEN 対話的にRedmineのURL、APIキーを入力できる SHALL
2. WHEN 認証情報を入力 THEN 設定ファイルに安全に保存される SHALL
3. WHEN 複数のRedmineインスタンスを使用 THEN プロファイルを切り替えられる SHALL
4. WHEN `redmine auth status` を実行 THEN 現在の接続状態を確認できる SHALL

### 要求 2
**ユーザーストーリー:** 開発者として、様々な条件でフィルタリングして一覧表示したいので、担当チケットを素早く確認できる

#### 受け入れ基準
1. WHEN `redmine issue list` を実行 THEN 自分が担当のオープンなチケット一覧が表示される SHALL
2. WHEN `--assignee` オプションを指定 THEN 特定のユーザーのチケットが表示される SHALL
3. WHEN `--status` オプションを指定 THEN 特定のステータスのチケットが表示される SHALL
4. WHEN `--project` オプションを指定 THEN 特定のプロジェクトのチケットが表示される SHALL
5. WHEN `--limit` オプションを指定 THEN 表示件数を制限できる SHALL
6. WHEN `--json` オプションを指定 THEN JSON形式で出力される SHALL

### 要求 3
**ユーザーストーリー:** 開発者として、IDを指定して全情報を表示したいので、チケットの詳細情報を確認できる

#### 受け入れ基準
1. WHEN `redmine issue view <ID>` を実行 THEN チケットの詳細情報が表示される SHALL
2. WHEN チケットにコメントがある THEN 履歴も含めて表示される SHALL
3. WHEN `--json` オプションを指定 THEN JSON形式で出力される SHALL
4. WHEN 存在しないIDを指定 THEN エラーメッセージが表示される SHALL

### 要求 4
**ユーザーストーリー:** 開発者として、必要な情報を入力して登録したいので、新しいチケットを作成できる

#### 受け入れ基準
1. WHEN `redmine issue create` を実行 THEN 対話的にチケット情報を入力できる SHALL
2. WHEN `--project` オプションを指定 THEN プロジェクトを事前に選択できる SHALL
3. WHEN `--title` と `--description` を指定 THEN 非対話的に作成できる SHALL
4. WHEN チケットが作成される THEN 作成されたチケットのIDとURLが表示される SHALL

### 要求 5
**ユーザーストーリー:** 開発者として、ステータスや担当者を変更したいので、チケットの状態を更新できる

#### 受け入れ基準
1. WHEN `redmine issue edit <ID>` を実行 THEN 対話的に更新内容を選択できる SHALL
2. WHEN `--status` オプションを指定 THEN ステータスを直接更新できる SHALL
3. WHEN `--assignee` オプションを指定 THEN 担当者を変更できる SHALL
4. WHEN `--done-ratio` オプションを指定 THEN 進捗率を更新できる SHALL
5. WHEN 更新が成功 THEN 確認メッセージが表示される SHALL

### 要求 6
**ユーザーストーリー:** 開発者として、コメントを追加したいので、チケットに進捗を記録できる

#### 受け入れ基準
1. WHEN `redmine issue comment <ID>` を実行 THEN コメントを入力できる SHALL
2. WHEN `--message` オプションを指定 THEN 直接コメントを追加できる SHALL
3. WHEN エディタが設定されている THEN エディタでコメントを編集できる SHALL
4. WHEN コメントが追加される THEN 確認メッセージが表示される SHALL

### 要求 7
**ユーザーストーリー:** 開発者として、各種設定を管理したいので、ツールの動作をカスタマイズできる

#### 受け入れ基準
1. WHEN `redmine config set` を実行 THEN 設定値を変更できる SHALL
2. WHEN `redmine config get` を実行 THEN 現在の設定値を確認できる SHALL
3. WHEN `redmine config list` を実行 THEN すべての設定項目が表示される SHALL
4. WHEN デフォルトプロジェクトを設定 THEN `--project` の指定を省略できる SHALL