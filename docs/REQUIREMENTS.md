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
1. WHEN `redmine issue list` または `redmine issue ls` を実行 THEN プロジェクトのオープンなチケット一覧が表示される（デフォルト30件） SHALL
2. WHEN `--assignee <user>` または `-a <user>` オプションを指定 THEN 特定のユーザーのチケットが表示される SHALL
3. WHEN `--assignee @me` または `-a @me` を指定 THEN 現在の認証ユーザーのチケットが表示される SHALL
4. WHEN `--status <status>` または `-s <status>` オプションを指定 THEN 特定のステータスのチケットが表示される SHALL
5. WHEN `--project <project>` または `-p <project>` オプションを指定 THEN 特定のプロジェクトのチケットが表示される SHALL
6. WHEN `--author <user>` オプションを指定 THEN 特定のユーザーが作成したチケットが表示される SHALL
7. WHEN `--author @me` を指定 THEN 現在の認証ユーザーが作成したチケットが表示される SHALL
8. WHEN `--limit <number>` または `-L <number>` オプションを指定 THEN 表示件数を制限できる SHALL
9. WHEN `--json` オプションを指定 THEN JSON形式で出力される SHALL
10. WHEN 複数のフィルタオプションを組み合わせる THEN AND条件で絞り込まれる SHALL
11. WHEN `--status all` を指定 THEN 全てのステータスのチケットが表示される SHALL
12. WHEN `--web` または `-w` オプションを指定 THEN 指定された条件でRedmineのチケット一覧ページをWebブラウザで開く SHALL
13. WHEN `--web` と他のフィルタオプションを組み合わせる THEN URLにクエリパラメータとして条件が反映される SHALL
14. WHEN チケット一覧を表示 THEN 更新日時はデフォルトで相対時刻（例：about 2 hours ago）で表示される SHALL
15. WHEN `--absolute-time` オプションを指定 THEN 日時はローカルタイムゾーンの絶対時刻で表示される SHALL
16. WHEN `--json` オプションを指定 THEN 日時はISO 8601形式のUTCで出力される SHALL
17. WHEN `config set time.format` で設定 THEN 指定された形式（relative/absolute/utc）で日時が表示される SHALL
18. WHEN チケット一覧を表示 THEN 期日（Due Date）が設定されている場合は表示される SHALL
19. WHEN `--sort <field>` オプションを指定 THEN 指定されたフィールドでチケットがソートされる SHALL
20. WHEN `--sort <field>:asc` または `--sort <field>:desc` を指定 THEN 指定された方向（昇順/降順）でソートされる SHALL
21. WHEN `--sort` で複数フィールドを指定（例：`--sort priority:desc,id`） THEN 複数条件でソートされる SHALL
22. WHEN `--sort` で無効なフィールドを指定 THEN エラーメッセージが表示される SHALL
23. WHEN `--sort` と他のフィルタオプションを組み合わせる THEN フィルタ後の結果がソートされる SHALL

### 要求 3
**ユーザーストーリー:** 開発者として、IDを指定して全情報を表示したいので、チケットの詳細情報を確認できる

#### 受け入れ基準
1. WHEN `redmine issue view <ID>` を実行 THEN チケットの詳細情報が表示される SHALL
2. WHEN チケットにコメントがある THEN 最新のコメントのみが表示され、残りのコメント数が表示される SHALL
3. WHEN `--comments` または `-c` オプションを指定 THEN 全ての履歴とコメントが表示される SHALL
4. WHEN `--json` オプションを指定 THEN JSON形式で出力される SHALL
5. WHEN 存在しないIDを指定 THEN エラーメッセージが表示される SHALL
6. WHEN `--web` または `-w` オプションを指定 THEN 該当チケットの詳細ページをWebブラウザで開く SHALL
7. WHEN チケットに期日（Due Date）が設定されている THEN 詳細情報に期日が表示される SHALL

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
1. WHEN `redmine --license` を実行 THEN RedmineCLI本体のライセンスとすべてのサードパーティライブラリのライセンス情報が表示される SHALL
2. WHEN `redmine --version` を実行 THEN バージョン情報と共に主要な依存ライブラリとそのライセンス種別（MIT、Apache 2.0等）が簡潔に表示される SHALL
3. WHEN バイナリ配布パッケージをダウンロード THEN THIRD-PARTY-NOTICES.txtファイルが実行ファイルと同じディレクトリに含まれている SHALL
4. WHEN オフライン環境で `redmine --license` を実行 THEN ビルド時に埋め込まれたライセンス情報が表示される SHALL
5. WHEN 新しい依存関係を追加してビルド THEN THIRD-PARTY-NOTICES.txtが自動的に更新される SHALL
6. WHEN ライセンス情報を表示 THEN 各ライブラリの著作権表示、ライセンス全文、プロジェクトURLが含まれる SHALL
7. WHEN 企業のセキュリティ監査で確認 THEN すべての依存関係とそのライセンスが明確に識別できる SHALL

### 要求 10（未実装）
**ユーザーストーリー:** 開発者として、RedmineCLIにない機能を追加したいので、プラグイン形式で独自の拡張機能を開発・実行できる

#### 受け入れ基準
1. WHEN `redmine <extension-name> <args>` を実行 THEN `redmine-<extension-name>` 実行ファイルが検索され実行される SHALL
2. WHEN 拡張機能が実行される THEN 環境変数経由でRedmineのURL、APIキー、現在のユーザー情報が渡される SHALL
3. WHEN 拡張機能をビルド THEN Native AOTまたはSelf-Containedとしてコンパイルできる SHALL
4. WHEN Native AOTでビルドされた拡張機能を実行 THEN 高速起動（< 100ms）を維持する SHALL
5. WHEN 拡張機能を配置 THEN 所定のディレクトリ（~/.local/share/redmine/extensions/）に置くことで利用可能になる SHALL
6. WHEN 存在しない拡張機能コマンドを実行 THEN 適切なエラーメッセージが表示される SHALL
7. WHEN 拡張機能がエラーで終了 THEN RedmineCLI本体は影響を受けずエラーを適切に報告する SHALL
8. WHEN 拡張機能がJSON出力形式を要求される THEN 環境変数REDMINE_OUTPUT_FORMATで通知される SHALL

### 要求 11
**ユーザーストーリー:** 開発者として、Redmineチケットに添付されたファイルをCLIから直接ダウンロードしたいので、GUIを使わずに効率的にファイルを取得できる

#### 受け入れ基準
1. WHEN `redmine issue view <ID>` を実行 THEN チケットに添付されているファイルの一覧（ファイル名、サイズ、作成者、アップロード日時）が表示される SHALL
2. WHEN `redmine issue attachment list <issue-id>` を実行 THEN 指定チケットの添付ファイル一覧が詳細に表示される SHALL
3. WHEN `redmine issue attachment download <issue-id>` を実行 THEN チケットの添付ファイル一覧から対話的に選択してダウンロードできる SHALL
4. WHEN `redmine issue attachment download <issue-id> --all` を実行 THEN チケットのすべての添付ファイルが一括でダウンロードされる SHALL
5. WHEN `redmine issue attachment download <issue-id> --output <directory>` または `-o <directory>` を指定 THEN 指定したディレクトリにファイルが保存される SHALL
6. WHEN ダウンロード中 THEN プログレスバーまたは進捗インジケーターが表示される SHALL
7. WHEN 複数ファイルの対話的選択時 THEN MultiSelectionPrompt（チェックボックス形式）で複数選択できる SHALL
8. WHEN ダウンロード先に同名ファイルが存在する THEN 自動的に連番付きのファイル名（例：report_1.pdf）で保存される SHALL
9. WHEN 複数ファイルを並行ダウンロード中に同名ファイルが存在する THEN スレッドセーフに一意のファイル名が生成される SHALL
10. WHEN 権限がない添付ファイルをダウンロードしようとする THEN 適切なエラーメッセージ（403 Forbidden）が表示される SHALL
11. WHEN ネットワークエラーが発生 THEN Pollyによるリトライ処理が実行され、最終的に失敗した場合はエラーメッセージが表示される SHALL
12. WHEN `--json` オプションを指定 THEN 添付ファイル情報がJSON形式で出力される SHALL
13. WHEN `GetIssueAsync` を呼び出す THEN `include=attachments` パラメータが自動的に含まれる SHALL
14. WHEN `attachment view <attachment-id>` を実行 THEN 添付ファイルのメタデータ（ファイル名、サイズ、種類、作成者、作成日時、説明）が表示される SHALL
15. WHEN `attachment download <attachment-id>` を実行 THEN 添付ファイルIDを直接指定してダウンロードできる SHALL
16. WHEN `issue view <ID>` を実行し、チケットの説明文に画像参照（`![](filename.png)` または `{{thumbnail(filename.png)}}`）が含まれる場合 THEN 該当する画像添付ファイルの情報（ファイル名、サイズ、タイプ）が "Inline Images" セクションに表示される SHALL
17. WHEN 説明文で参照されている画像ファイルが添付ファイルに存在しない場合 THEN 何も表示されずエラーも発生しない SHALL
18. WHEN ターミナルがSixelプロトコルをサポートしている場合 THEN 画像添付ファイルがSixelプロトコルを使用してターミナル内に表示される SHALL
19. WHEN 環境変数 `SIXEL_SUPPORT=1` または `SIXEL_SUPPORT=true` が設定されている場合 THEN Sixelサポートが有効になる SHALL

### 要求 12
**ユーザーストーリー:** AIエージェントの開発者として、RedmineCLIの使い方を理解しやすい形式で取得したいので、LLMs.txt標準に準拠した情報を出力できる

#### 受け入れ基準
1. WHEN `redmine llms` コマンドを実行 THEN LLMs.txt形式でRedmineCLIの情報が出力される SHALL
2. WHEN 情報を出力 THEN インストール方法、認証方法、主要コマンド、オプション、機能の説明が含まれる SHALL
3. WHEN コマンド例を表示 THEN 実際に使用可能な具体的なコマンド例が提供される SHALL
4. WHEN 出力形式 THEN マークダウン形式で構造化された読みやすい情報が表示される SHALL
5. WHEN AIエージェントが情報を取得 THEN RedmineCLIの全機能を理解し適切に使用できる SHALL

### 要求 13
**ユーザーストーリー:** 開発者として、Redmineに登録されているユーザー、プロジェクト、ステータスの一覧を取得したいので、チケット操作の際に必要な情報を確認できる

#### 受け入れ基準
1. WHEN `redmine user list` または `redmine user ls` を実行 THEN ユーザー一覧が表示される SHALL
2. WHEN ユーザー一覧を表示 THEN ID、ログイン名、氏名、作成日時が表示される SHALL
3. WHEN `--limit <number>` または `-L <number>` オプションを指定 THEN 表示件数を制限できる SHALL
4. WHEN `--all` または `-a` オプションを指定 THEN メールアドレスを含む詳細情報が表示される SHALL
5. WHEN `--json` オプションを指定 THEN JSON形式で出力される SHALL
6. WHEN `redmine project list` または `redmine project ls` を実行 THEN プロジェクト一覧が表示される SHALL
7. WHEN プロジェクト一覧を表示 THEN ID、識別子、名前、説明、作成日時が表示される SHALL
8. WHEN `--public` オプションを指定 THEN 公開プロジェクトのみが表示される SHALL
9. WHEN `--json` オプションを指定 THEN JSON形式で出力される SHALL
10. WHEN `redmine status list` または `redmine status ls` を実行 THEN チケットステータス一覧が表示される SHALL
11. WHEN ステータス一覧を表示 THEN ID、名前、終了ステータスかどうか、デフォルトステータスかどうかが表示される SHALL
12. WHEN `--json` オプションを指定 THEN JSON形式で出力される SHALL
13. WHEN 権限がない情報を取得しようとする THEN 適切なエラーメッセージが表示される SHALL
14. WHEN API接続エラーが発生 THEN 適切なエラーメッセージが表示される SHALL

### 要求 14
**ユーザーストーリー:** 開発者として、gh issue closeと同様の使い勝手でRedmineチケットをクローズしたいので、効率的にチケットを終了できる

#### 受け入れ基準
1. WHEN `redmine issue close <ID>` を実行 THEN チケットがクローズステータスに変更される SHALL
2. WHEN `--message <text>` または `-m <text>` オプションを指定 THEN クローズ時にコメントが追加される SHALL
3. WHEN `--status <status>` または `-s <status>` オプションを指定 THEN 指定したステータスでクローズされる SHALL
4. WHEN ステータスが指定されない場合 THEN システムで定義された最初のクローズステータス（IsClosed = true）が自動選択される SHALL
5. WHEN `--done-ratio <percent>` または `-d <percent>` オプションを指定 THEN 進捗率が指定値に設定される SHALL
6. WHEN 進捗率が指定されない場合 THEN 進捗率が100%に設定される SHALL
7. WHEN チケットが既にクローズされている場合 THEN 警告メッセージが表示される（エラーにはしない） SHALL
8. WHEN クローズステータスが存在しない場合 THEN 利用可能なステータス一覧を表示してエラーになる SHALL
9. WHEN 指定されたステータスがクローズステータスでない場合 THEN 警告メッセージが表示されるが処理は続行される SHALL
10. WHEN クローズが成功 THEN 確認メッセージが表示される SHALL
11. WHEN 複数のチケットIDを指定 THEN 各チケットが順番にクローズされる SHALL
12. WHEN `--json` オプションを指定 THEN 結果がJSON形式で出力される SHALL

### 要求 15
**ユーザーストーリー:** 拡張機能の開発者として、APIキーが使用できない環境でもユーザー名/パスワード認証を使いたいので、OSのキーチェーンを使用して安全に認証情報を管理できる

#### 受け入れ基準
1. WHEN `redmine auth login --save-password` を実行 THEN ユーザー名とパスワードがOSのキーチェーンに安全に保存される SHALL
2. WHEN Windows環境で実行 THEN Windows Credential Managerに認証情報が保存される SHALL
3. WHEN macOS環境で実行 THEN macOS Keychainに認証情報が保存される SHALL
4. WHEN Linux環境で実行 THEN libsecret（Secret Service API）に認証情報が保存される SHALL
5. WHEN `redmine auth status` を実行 THEN OSキーチェーンにパスワードが保存されているかどうかが表示される SHALL
6. WHEN 拡張機能が実行される THEN RedmineCLI.Commonライブラリを使用してOSキーチェーンから認証情報を取得できる SHALL
7. WHEN 拡張機能がキーチェーンから認証情報を取得 THEN 環境変数経由ではなく直接アクセスすることでセキュリティが確保される SHALL
8. WHEN `redmine auth logout --clear-keychain` を実行 THEN OSキーチェーンから認証情報が削除される SHALL
9. WHEN APIキーが設定されている場合 THEN APIキー認証が優先され、キーチェーンの認証情報は使用されない SHALL
10. WHEN キーチェーンアクセスでエラーが発生 THEN 適切なエラーメッセージが表示される SHALL
11. WHEN RedmineCLI.Commonライブラリをビルド THEN Native AOT互換（IsAotCompatible: true）として設定される SHALL
12. WHEN 複数のRedmineサーバーを使用 THEN サーバーURLごとに異なる認証情報をキーチェーンに保存できる SHALL