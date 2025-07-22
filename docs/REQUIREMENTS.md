# 要求定義書

## はじめに

RedmineCLIは、Redmineのチケット管理をコマンドラインから効率的に行うためのツールです。実行コマンド名は`redmine`で、プロジェクト名・名前空間は`RedmineCLI`を維持しています。
主な技術要素として、Redmine REST API（v3.0以上）との通信、APIキーベースの認証、ghコマンドライクなインターフェースを採用します。
ターゲットユーザーは、ターミナルでの作業を好む開発者やエンジニアであり、GUIを使わずに素早くチケット操作を行いたいというニーズに応えます。

## 要求一覧

### 要求 1
**ユーザーストーリー:** 開発者として、APIキーを使った認証を簡単に設定したいので、Redmineサーバーに安全に接続できる

#### 受け入れ基準
1. WHEN `redmine auth login` コマンドを実行 THEN 対話的にRedmineのURL、APIキーを入力できる SHALL
2. WHEN 認証情報を入力 THEN 設定ファイルに安全に保存される SHALL
3. WHEN 複数のRedmineインスタンスを使用 THEN プロファイルを切り替えられる SHALL
4. WHEN `redmine auth status` を実行 THEN 現在の接続状態を確認できる SHALL

### 要求 2
**ユーザーストーリー:** 開発者として、プロジェクト全体のチケット状況を把握し、必要に応じて様々な条件でフィルタリングしたいので、効率的にチケット管理ができる

#### 受け入れ基準
1. WHEN `redmine issue list` を実行 THEN プロジェクトのオープンなチケット一覧が表示される（デフォルト30件） SHALL
2. WHEN `--assignee <user>` または `-a <user>` オプションを指定 THEN 特定のユーザーのチケットが表示される SHALL
3. WHEN `--assignee @me` または `-a @me` を指定 THEN 現在の認証ユーザーのチケットが表示される SHALL
4. WHEN `--status <status>` または `-s <status>` オプションを指定 THEN 特定のステータスのチケットが表示される SHALL
5. WHEN `--project <project>` または `-p <project>` オプションを指定 THEN 特定のプロジェクトのチケットが表示される SHALL
6. WHEN `--limit <number>` または `-L <number>` オプションを指定 THEN 表示件数を制限できる SHALL
7. WHEN `--json` オプションを指定 THEN JSON形式で出力される SHALL
8. WHEN 複数のフィルタオプションを組み合わせる THEN AND条件で絞り込まれる SHALL
9. WHEN `--status all` を指定 THEN 全てのステータスのチケットが表示される SHALL
10. WHEN `--web` または `-w` オプションを指定 THEN 指定された条件でRedmineのチケット一覧ページをWebブラウザで開く SHALL
11. WHEN `--web` と他のフィルタオプションを組み合わせる THEN URLにクエリパラメータとして条件が反映される SHALL
12. WHEN チケット一覧を表示 THEN 更新日時はデフォルトで相対時刻（例：about 2 hours ago）で表示される SHALL
13. WHEN `--absolute-time` オプションを指定 THEN 日時はローカルタイムゾーンの絶対時刻で表示される SHALL
14. WHEN `--json` オプションを指定 THEN 日時はISO 8601形式のUTCで出力される SHALL
15. WHEN `config set time.format` で設定 THEN 指定された形式（relative/absolute/utc）で日時が表示される SHALL

### 要求 3
**ユーザーストーリー:** 開発者として、IDを指定して全情報を表示したいので、チケットの詳細情報を確認できる

#### 受け入れ基準
1. WHEN `redmine issue view <ID>` を実行 THEN チケットの詳細情報が表示される SHALL
2. WHEN チケットにコメントがある THEN 履歴も含めて表示される SHALL
3. WHEN `--json` オプションを指定 THEN JSON形式で出力される SHALL
4. WHEN 存在しないIDを指定 THEN エラーメッセージが表示される SHALL
5. WHEN `--web` または `-w` オプションを指定 THEN 該当チケットの詳細ページをWebブラウザで開く SHALL

### 要求 4
**ユーザーストーリー:** 開発者として、必要な情報を入力して登録したいので、新しいチケットを作成できる

#### 受け入れ基準
1. WHEN `redmine issue create` を実行 THEN 対話的にチケット情報を入力できる SHALL
2. WHEN `--project <project>` または `-p <project>` オプションを指定 THEN プロジェクトを事前に選択できる SHALL
3. WHEN `--title <title>` または `-t <title>` と `--description <desc>` または `-d <desc>` を指定 THEN 非対話的に作成できる SHALL
4. WHEN チケットが作成される THEN 作成されたチケットのIDとURLが表示される SHALL
5. WHEN `--assignee <user>` または `-a <user>` を指定 THEN 担当者を事前に設定できる SHALL
6. WHEN `--web` または `-w` オプションを指定 THEN 新規チケット作成ページをWebブラウザで開く SHALL
7. WHEN `--web` と `--project` を組み合わせる THEN 指定プロジェクトの新規チケット作成ページを開く SHALL

### 要求 5
**ユーザーストーリー:** 開発者として、ステータスや担当者を変更したいので、チケットの状態を更新できる

#### 受け入れ基準
1. WHEN `redmine issue edit <ID>` を実行 THEN 対話的に更新内容を選択できる SHALL
2. WHEN `--status <status>` または `-s <status>` オプションを指定 THEN ステータスを直接更新できる SHALL
3. WHEN `--assignee <user>` または `-a <user>` オプションを指定 THEN 担当者を変更できる SHALL
4. WHEN `--assignee @me` または `-a @me` を指定 THEN 現在の認証ユーザーに担当者を変更できる SHALL
5. WHEN `--done-ratio <percent>` オプションを指定 THEN 進捗率を更新できる SHALL
6. WHEN 更新が成功 THEN 確認メッセージが表示される SHALL
7. WHEN `--web` または `-w` オプションを指定 THEN 該当チケットの編集ページをWebブラウザで開く SHALL

### 要求 6
**ユーザーストーリー:** 開発者として、コメントを追加したいので、チケットに進捗を記録できる

#### 受け入れ基準
1. WHEN `redmine issue comment <ID>` を実行 THEN コメントを入力できる SHALL
2. WHEN `--message <text>` または `-m <text>` オプションを指定 THEN 直接コメントを追加できる SHALL
3. WHEN エディタが設定されている THEN エディタでコメントを編集できる SHALL
4. WHEN コメントが追加される THEN 確認メッセージが表示される SHALL

### 要求 7
**ユーザーストーリー:** 開発者として、各種設定を管理したいので、ツールの動作をカスタマイズできる

#### 受け入れ基準
1. WHEN `redmine config set` を実行 THEN 設定値を変更できる SHALL
2. WHEN `redmine config get` を実行 THEN 現在の設定値を確認できる SHALL
3. WHEN `redmine config list` を実行 THEN すべての設定項目が表示される SHALL
4. WHEN デフォルトプロジェクトを設定 THEN `--project` の指定を省略できる SHALL

### 要求 8
**ユーザーストーリー:** 開発者として、高速に起動する軽量なCLIツールを使いたいので、効率的に作業を進められる

#### 受け入れ基準
1. WHEN Windows/macOS/Linux環境で実行 THEN 各OSネイティブの最適化されたバイナリが動作する SHALL
2. WHEN コマンドを実行 THEN 100ms以内に起動して応答する SHALL
3. WHEN 配布ファイルをダウンロード THEN 単一の実行可能ファイルとして10MB以下のサイズである SHALL
4. WHEN 実行環境にインストール THEN .NETランタイムや追加の依存関係なしに動作する SHALL
5. WHEN Native AOTでビルド THEN メモリ使用量が従来のJITビルドより削減される SHALL

### 要求 9
**ユーザーストーリー:** 開発者として、使用しているオープンソースライブラリのライセンス情報を確認したいので、法的コンプライアンスを確保し、自分のプロジェクトでの使用可否を判断できる

#### 受け入れ基準
1. WHEN `redmine --licenses` を実行 THEN RedmineCLI本体のライセンスとすべてのサードパーティライブラリのライセンス情報が表示される SHALL
2. WHEN `redmine --version` を実行 THEN バージョン情報と共に主要な依存ライブラリとそのライセンス種別（MIT、Apache 2.0等）が簡潔に表示される SHALL
3. WHEN バイナリ配布パッケージをダウンロード THEN THIRD-PARTY-NOTICES.txtファイルが実行ファイルと同じディレクトリに含まれている SHALL
4. WHEN オフライン環境で `redmine --licenses` を実行 THEN ビルド時に埋め込まれたライセンス情報が表示される SHALL
5. WHEN 新しい依存関係を追加してビルド THEN THIRD-PARTY-NOTICES.txtが自動的に更新される SHALL
6. WHEN ライセンス情報を表示 THEN 各ライブラリの著作権表示、ライセンス全文、プロジェクトURLが含まれる SHALL
7. WHEN 企業のセキュリティ監査で確認 THEN すべての依存関係とそのライセンスが明確に識別できる SHALL