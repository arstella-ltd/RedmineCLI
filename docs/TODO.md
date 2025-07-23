# 実装計画

このプロジェクトではt-wada流のテスト駆動開発（TDD）を採用します。
各機能の実装は以下のサイクルで進めます。
1. **Red**: まず失敗するテストを書く
2. **Green**: テストを通す最小限の実装を行う
3. **Refactor**: コードを改善する

## 重要な実装ルール

**⚠️ タスク完了時の必須手順**

各タスクが完了したら、必ずTODO.mdを以下の手順で更新してください

1. 該当タスクを `- [ ]` から `- [x]` に変更
2. 実装メモセクションに完了記録を追加（日付、TDD手法、テスト件数、達成事項）
3. 変更をコミットする前にTODO.mdの更新が完了していることを確認

この手順により、プロジェクトの進捗が正確に記録され、実装品質が維持されます。

## TDD実践のポイント

### 基本原則
- テストファーストで開発を進める
- 1テスト1アサーション原則を守る
- AAA（Arrange-Act-Assert）パターンでテストを構造化
- テストコードはプロダクションコードと同等の品質を保つ
- 可能な限り本物のオブジェクトを使用し、必要な場合のみモックを使用

### テスト命名規則
```
{Method}_Should_{ExpectedBehavior}_When_{Condition}
```
例: `List_Should_ReturnFilteredIssues_When_StatusIsSpecified`

### テストピラミッド戦略
- **単体テスト（70%）**: 高速実行（< 10ms/test）、ビジネスロジックの検証
- **統合テスト（20%）**: 中速実行（< 100ms/test）、コンポーネント間連携の検証
- **E2Eテスト（10%）**: 低速実行（< 1s/test）、ユーザーシナリオの検証

### Native AOT対応のTDD考慮事項
- リフレクションを使用するテストフレームワークの機能を避ける
- Source Generatorベースのモック生成を優先
- AOT互換性のあるアサーションライブラリ（FluentAssertions）を使用
- テスト実行時もAOTコンパイルで動作確認

### テストダブルの使い分け
- **スタブ**: 外部APIやファイルシステムからの応答を固定値で返す場合
- **モック**: 呼び出し回数や引数を検証する必要がある場合のみ使用
- **フェイク**: インメモリ実装で高速なテストが可能な場合（例: インメモリ設定ストア）

## 実装タスク一覧

- [x] 1. プロジェクトのセットアップと基本構造の作成
  - .NET 9のコンソールアプリケーションプロジェクトを作成
  - ソリューションファイルの作成とプロジェクト構成
  - Native AOT対応の設定（PublishAot=true、必要なアナライザーの追加）
  - AOT互換性を考慮したNuGetパッケージの選定とインストール
  - プロジェクト構造（フォルダ階層）の作成
  - AOT制約を考慮したDIコンテナの設定とProgram.csの基本実装
  - global.jsonによる.NET SDKバージョンの固定
  - .editorconfigによるコードスタイルの統一
  - Native AOTビルドの動作確認（各プラットフォーム）
  - _要件: 8, 全体的な基盤_

- [x] 2. データモデルとインターフェースの定義（TDD）
  - **Red（〜30分）**: 各モデルクラスのテストを先に作成
    - IssueTests（以下のテストケースを作成）
      - `Constructor_Should_InitializeProperties_When_ValidDataProvided`
      - `Validation_Should_ThrowException_When_RequiredFieldsAreMissing`
      - `Equals_Should_ReturnTrue_When_IdsAreEqual`
    - ConfigTests（以下のテストケースを作成）
      - `Save_Should_PersistSettings_When_ValidConfigProvided`
      - `Load_Should_ReturnDefaultConfig_When_FileNotExists`
    - DTOシリアライゼーションテスト（以下のテストケースを作成）
      - `Serialize_Should_ProduceValidJson_When_ModelIsValid`
      - `Deserialize_Should_CreateObject_When_JsonIsValid`
  - **Green（〜1時間）**: テストを通すための最小限のモデル実装
    - Issue、Project、User等の基本モデルクラスを作成（AOT向けにSource Generator対応）
    - IRedmineApiClient、IConfigService等のインターフェース定義
    - Config、Profile、Preferences等の設定モデルを作成
    - APIレスポンス用のDTOクラスを定義（System.Text.Json Source Generator使用）
  - **Refactor（〜30分）**: モデルの改善
    - 共通バリデーションロジックの抽出
    - 不変性（Immutability）の確保
    - パフォーマンス最適化（構造体の検討）
  - _要件: 全体的な基盤_

- [x] 3. 設定管理機能の実装（TDD）
  - **Red（〜45分）**: ConfigServiceのテストを先に作成
    - ファイルI/Oテスト（以下のテストケースを作成）
      - `LoadConfig_Should_ReturnDefaultConfig_When_FileNotExists`
      - `SaveConfig_Should_CreateFile_When_FirstTimeSave`
      - `LoadConfig_Should_ThrowException_When_FileIsCorrupted`
    - プロファイル管理テスト（以下のテストケースを作成）
      - `SwitchProfile_Should_ChangeActiveProfile_When_ProfileExists`
      - `CreateProfile_Should_AddNewProfile_When_NameIsUnique`
    - セキュリティテスト（以下のテストケースを作成）
      - `EncryptApiKey_Should_NotStorePlainText_When_Saving`
      - `DecryptApiKey_Should_ReturnOriginalValue_When_Loading`
  - **Green（〜1.5時間）**: テストを通すための実装
    - AOT互換のYAMLライブラリ選定（YamlDotNetのAOT対応確認）
    - ConfigServiceの実装（リフレクション最小化）
    - 設定ファイルの読み込み・保存機能
    - 複数プロファイルの管理機能
    - APIキーの暗号化/復号化処理（Data Protection API使用）
  - **Refactor（〜45分）**: エラーハンドリングとテスタビリティの改善
    - ファイルシステム抽象化（IFileSystem）の導入
    - 設定検証ロジックの分離
    - エラーメッセージの改善
    - 非同期処理の最適化
  - _要件: 1, 7_

- [x] 4. Redmine APIクライアントの実装（TDD）
  - **Red**: APIクライアントのテストを先に作成
    - HTTPリクエストのモックテスト
    - 認証ヘッダーの検証テスト
    - リトライポリシーのテスト
    - エラーレスポンスの処理テスト
  - **Green**: テストを通すための実装
    - HttpClientFactoryを使用したRedmineApiClientの実装
    - Pollyによるリトライポリシーの設定
    - 認証ヘッダー（APIキー）の自動付与
    - エラーハンドリングとカスタム例外の実装
  - **Refactor**: 共通処理の抽出とコードの最適化
  - _要件: 1_

- [x] 5. 認証コマンドの実装（TDD）
  - **Red**: AuthCommandのテストを先に作成
    - Login_Should_SaveCredentials_When_ValidInput
    - Status_Should_ShowConnectionState_When_Authenticated
    - Logout_Should_ClearCredentials_When_Called
    - Login_Should_HandleInvalidUrl_When_MalformedInput
  - **Green**: テストを通すための実装
    - `auth login`コマンド：対話的な認証情報入力
    - `auth status`コマンド：接続状態の確認
    - `auth logout`コマンド：認証情報のクリア
    - Spectre.Consoleを使用した対話的UI
  - **Refactor**: UIロジックの分離とテスタビリティの向上
  - _要件: 1_

- [x] 6. チケット一覧表示コマンドの実装（TDD）
  - **Red**: IssueListCommandのテストを先に作成
    - List_Should_ReturnAssignedIssues_When_NoOptionsSpecified
    - List_Should_ReturnFilteredIssues_When_StatusIsSpecified
    - List_Should_LimitResults_When_LimitOptionIsSet
    - List_Should_FormatAsJson_When_JsonOptionIsSet
  - **Green**: テストを通すための実装
    - `issue list`コマンドの基本実装（デフォルトで全オープンチケット表示）
    - フィルタリングオプション（assignee、status、project）の実装
    - ページネーション対応（limit、offset）
    - TableFormatterとJsonFormatterの実装
  - **Refactor**: フォーマッターの抽象化とコードの整理
  - _要件: 2_

- [x] 7. チケット詳細表示コマンドの実装（TDD）
  - **Red**: IssueViewCommandのテストを先に作成
    - View_Should_ShowIssueDetails_When_ValidIdProvided
    - View_Should_ShowHistory_When_JournalsExist
    - View_Should_ReturnError_When_IssueNotFound
    - View_Should_FormatAsJson_When_JsonOptionIsSet
    - View_Should_OpenBrowser_When_WebOptionIsSet
  - **Green**: テストを通すための実装
    - `issue view`コマンドの実装
    - チケット履歴（journals）の表示機能
    - Spectre.Consoleによる整形された出力
    - JSON出力オプションの実装
    - --webオプションによるブラウザ起動機能
  - **Refactor**: 出力ロジックの共通化、ブラウザ起動処理の抽象化
  - _要件: 3_

- [x] 8. チケット作成コマンドの実装（TDD）
  - **Red**: IssueCreateCommandのテストを先に作成
    - Create_Should_CreateIssue_When_InteractiveMode
    - Create_Should_CreateIssue_When_OptionsProvided
    - Create_Should_ShowUrl_When_IssueCreated
    - Create_Should_ValidateInput_When_RequiredFieldsMissing
    - Create_Should_OpenBrowser_When_WebOptionIsSet
    - Create_Should_SetAssigneeToMe_When_AssigneeIsAtMe
  - **Green**: テストを通すための実装
    - `issue create`コマンドの実装
    - 対話的な入力モード（プロジェクト選択、タイトル、説明入力）
    - コマンドラインオプションによる非対話的作成（-p, -t, -a）
    - @me特殊値の処理（担当者指定時）
    - 作成成功時のURL表示
    - --webオプションによる新規作成ページのブラウザ起動
  - **Refactor**: 入力検証ロジックの改善、@me処理の共通化
  - _要件: 4_

- [x] 9. チケット更新コマンドの実装（TDD）
  - **Red**: IssueEditCommandのテストを先に作成
    - Edit_Should_UpdateStatus_When_StatusOptionProvided
    - Edit_Should_UpdateAssignee_When_AssigneeOptionProvided
    - Edit_Should_UpdateProgress_When_DoneRatioProvided
    - Edit_Should_ShowConfirmation_When_UpdateSuccessful
    - Edit_Should_OpenBrowser_When_WebOptionIsSet
    - Edit_Should_SetAssigneeToMe_When_AssigneeIsAtMe
  - **Green**: テストを通すための実装
    - `issue edit`コマンドの実装
    - ステータス、担当者、進捗率の更新機能（-s, -a, --done-ratio）
    - @me特殊値の処理（担当者変更時）
    - 対話的な更新内容選択UI
    - 部分的な更新（PATCH）の実装
    - --webオプションによる編集ページのブラウザ起動
  - **Refactor**: 更新ロジックの統合と最適化、@me処理の共通化
  - _要件: 5_

- [x] 10. コメント追加コマンドの実装（TDD）
  - **Red**: IssueCommentCommandのテストを先に作成
    - Comment_Should_AddComment_When_MessageProvided
    - Comment_Should_OpenEditor_When_NoMessageOption
    - Comment_Should_ShowConfirmation_When_CommentAdded
    - Comment_Should_HandleEmptyComment_When_NoTextEntered
  - **Green**: テストを通すための実装
    - `issue comment`コマンドの実装
    - エディタ統合（$EDITOR環境変数の利用）
    - メッセージオプションによる直接入力（-m）
    - コメント追加確認メッセージ
  - **Refactor**: エディタ統合ロジックの改善
  - _要件: 6_

- [x] 11. ライセンス情報管理機能の実装（TDD）
  - **Red（〜30分）**: ライセンス管理のテストを先に作成
    - ライセンス表示テスト（以下のテストケースを作成）
      - `ShowLicenses_Should_DisplayAllLicenses_When_LicensesOptionProvided`
      - `ShowVersion_Should_IncludeLicenseInfo_When_VersionOptionProvided`
    - ライセンスファイル生成テスト（以下のテストケースを作成）
      - `GenerateNotices_Should_CreateFile_When_BuildExecuted`
      - `EmbedLicenses_Should_IncludeInBinary_When_AotBuild`
  - **Green（〜1時間）**: テストを通すための実装
    - THIRD-PARTY-NOTICES.txtの作成と管理
    - --licensesオプションの実装
    - --versionコマンドへのライセンス情報追加
    - ビルド時のライセンス情報埋め込み
    - MSBuildタスクによる自動生成
  - **Refactor（〜30分）**: ライセンス管理の最適化
    - ライセンス情報のキャッシュ
    - 表示フォーマットの改善
    - 依存関係の自動検出
  - _要件: 9_

- [x] 12. 日時表示の改善 - 相対時刻表示とローカルタイムゾーン対応（TDD）
  - **Red**: TimeHelperのテストを先に作成
    - GetRelativeTime_Should_ReturnMinutesAgo_When_LessThanHour
    - GetRelativeTime_Should_ReturnHoursAgo_When_LessThanDay  
    - GetRelativeTime_Should_ReturnDaysAgo_When_LessThanMonth
    - GetRelativeTime_Should_ReturnMonthDay_When_CurrentYear
    - GetRelativeTime_Should_ReturnFullDate_When_PreviousYear
    - ConvertToLocalTime_Should_ConvertFromUtc_When_ValidDateTime
    - FormatTime_Should_UseConfiguredFormat_When_SettingExists
  - **Green**: テストを通すための実装
    - Utils/TimeHelper.csクラスの実装（ITimeHelperインターフェース）
    - UTCからローカルタイムゾーンへの変換機能
    - 相対時刻計算ロジック（GitHub CLI準拠）
    - TableFormatterでの時刻表示の改善
    - --absolute-timeオプションの追加（ローカル時刻表示）
    - 設定による時刻表示形式の切り替え（relative/absolute/utc）
  - **Refactor**: 時刻表示の一貫性確保とパフォーマンス最適化
  - _要件: 2_

- [ ] 13. 設定管理コマンドの実装（TDD）
  - **Red**: ConfigCommandのテストを先に作成
    - Set_Should_UpdateValue_When_ValidKeyProvided
    - Get_Should_ReturnValue_When_KeyExists
    - List_Should_ShowAllSettings_When_Called
    - Set_Should_ValidateKey_When_UnknownKeyProvided
    - Set_Should_UpdateTimeFormat_When_TimeSettingProvided
  - **Green**: テストを通すための実装
    - `config set`コマンド：設定値の変更
    - `config get`コマンド：設定値の取得
    - `config list`コマンド：全設定の一覧表示
    - デフォルトプロジェクト等の設定管理
    - 時刻表示設定の管理（time.format, time.timezone）
  - **Refactor**: 設定キーの検証とヘルプの改善
  - _要件: 7_

- [ ] 14. エラーハンドリングとログ機能の実装（TDD）
  - **Red**: エラーハンドリングのテストを先に作成
    - GlobalHandler_Should_ShowFriendlyMessage_When_ApiException
    - GlobalHandler_Should_ShowStackTrace_When_DebugMode
    - Logger_Should_WriteToFile_When_DebugEnabled
    - ErrorMessage_Should_SuggestRecovery_When_KnownError
  - **Green**: テストを通すための実装
    - グローバルエラーハンドラーの実装
    - Microsoft.Extensions.Loggingによるログ出力
    - デバッグモードの実装（--debugフラグ）
    - ユーザーフレンドリーなエラーメッセージ
  - **Refactor**: エラーメッセージのローカライズ準備
  - _要件: 全体的な基盤_

- [ ] 15. 統合テストとE2Eテストの実装
  - **注**: 各機能の単体テストは既にTDDで実装済み
  - **統合テスト**: 実際のファイルシステムとの連携テスト
    - 設定ファイルの読み書き統合テスト
    - コマンド間の連携テスト
  - **E2Eテスト**: WireMock.Netを使用したAPIモックテスト
    - 完全なコマンドシナリオのテスト
    - エラーシナリオのテスト
  - **カバレッジ**: 単体テストで80%以上、統合テスト含めて90%以上を目標
  - _要件: 全体的な基盤_

- [ ] 16. ドキュメントとパッケージング
  - **ドキュメント作成**
    - README.mdの作成（インストール手順、使用方法）
    - CONTRIBUTING.mdの作成（TDD開発フローの説明）
    - API仕様書の生成
  - **パッケージング**
    - dotnet toolとしてのパッケージング設定
    - 各プラットフォーム向けネイティブバイナリのリリースビルド
    - 実行ファイルサイズの最終最適化（<10MB確認）
    - 起動時間の最終確認（<100ms確認）
  - **リリース準備**
    - リリースノートの作成
    - CI/CDパイプラインの設定
    - 自動テストとビルドの設定
  - _要件: 8, 全体的な基盤_

- [ ] 17. 継続的テスト実行環境の整備（TDD文化の定着）
  - **テスト自動実行の設定**
    - ファイル変更監視による自動テスト実行（dotnet watch test）
    - Git pre-commitフックでのテスト実行
    - プッシュ前の全テスト実行の強制
  - **テストフィードバックの高速化**
    - 並列テスト実行の最適化
    - テストの実行順序の最適化（失敗しやすいテストを先に）
    - 増分テスト実行の設定（変更されたコードに関連するテストのみ）
  - **テスト品質の可視化**
    - コードカバレッジレポートの自動生成
    - ミューテーションテストによるテスト品質の検証
    - テスト実行時間のモニタリングとアラート
  - **チーム文化の醸成**
    - TDDペアプログラミングセッションのガイドライン作成
    - テストレビューチェックリストの作成
    - 「テストが書けない設計は悪い設計」の原則の徹底
  - _要件: 全体的な基盤_

- [ ] 18. 拡張機能システムの実装（TDD）
  - **Red**: ExtensionExecutorのテストを先に作成
    - Execute_Should_RunExtension_When_ExtensionExists
    - Execute_Should_PassEnvironmentVariables_When_ProfileActive
    - Execute_Should_ReturnError_When_ExtensionNotFound
    - Execute_Should_PropagateExitCode_When_ExtensionFails
  - **Green**: テストを通すための実装
    - ExtensionExecutorクラスの実装
    - 拡張機能の検索ロジック（複数パス対応）
    - 環境変数の設定と子プロセス起動
    - 標準出力/エラー出力のパススルー
    - System.CommandLineへの統合（未知のコマンドを拡張機能として処理）
  - **Refactor**: エラーハンドリングとパフォーマンスの最適化
  - _要件: 10_

- [ ] 19. 拡張機能管理コマンドの実装（将来機能）
  - **Red**: ExtensionCommandのテストを先に作成
    - Install_Should_DownloadAndExtract_When_ValidUrl
    - List_Should_ShowInstalledExtensions_When_Called
    - Remove_Should_DeleteExtension_When_Exists
    - Update_Should_ReplaceExtension_When_NewVersionAvailable
  - **Green**: テストを通すための実装
    - `extension install`コマンド：GitHubリリースからダウンロード
    - `extension list`コマンド：インストール済み拡張機能の一覧
    - `extension remove`コマンド：拡張機能の削除
    - `extension update`コマンド：拡張機能の更新
  - **Refactor**: セキュリティとエラー処理の強化
  - _要件: 10（将来機能）_

- [ ] 20. 添付ファイルダウンロード機能の実装（TDD）
  - **Red**: AttachmentCommandのテストを先に作成
    - Download_Should_SaveFile_When_ValidAttachmentId
    - Download_Should_UseOutputPath_When_OutputOptionProvided
    - Download_Should_ShowProgress_When_Downloading
    - Download_Should_SanitizeFilename_When_UnsafeCharacters
    - Download_Should_OverwriteFile_When_ForceOptionProvided
    - View_Should_ShowMetadata_When_AttachmentIdProvided
  - **Green**: テストを通すための実装
    - Attachmentモデルクラスの作成（JSON Source Generator対応）
    - `attachment download <id>`コマンドの実装
    - `attachment view <id>`コマンドの実装
    - HTTPストリーミングダウンロード（進捗表示付き）
    - ファイル名のサニタイズ処理（パストラバーサル対策）
    - --output/-o オプションによる保存先指定
    - --force/-f オプションによる上書き許可
  - **Refactor**: ダウンロード処理の最適化とエラーハンドリング
  - _要件: 11_

- [ ] 21. チケット添付ファイル管理コマンドの実装（TDD）
  - **Red**: IssueAttachmentCommandのテストを先に作成
    - ListAttachments_Should_ShowAttachments_When_IssueHasFiles
    - ListAttachments_Should_ShowEmptyMessage_When_NoAttachments
    - DownloadAttachments_Should_PromptSelection_When_InteractiveMode
    - DownloadAttachments_Should_DownloadAll_When_AllOptionProvided
    - IssueView_Should_IncludeAttachments_When_AttachmentsExist
  - **Green**: テストを通すための実装
    - `issue list-attachments <id>` / `issue attachments <id>`コマンドの実装
    - `issue download-attachments <id> --interactive`の実装
    - `issue download-attachments <id> --all`の実装
    - IssueモデルにAttachmentsプロパティを追加
    - issue viewコマンドでの添付ファイル表示
    - Spectre.ConsoleのMultiSelectionPromptを使用した対話的選択
    - 複数ファイルの並行ダウンロード
  - **Refactor**: UIの改善とパフォーマンス最適化
  - _要件: 11_

## 実装メモ

### タスク2完了（2025-07-20）
- TDD手法（Red→Green→Refactor）に従って実装
- YamlDotNetのNative AOT互換性問題のため、VYamlに移行
- VYamlは優れたパフォーマンス（YamlDotNetの6倍高速）とAOT互換性を提供
- すべてのテスト（15件）が成功
- AOTビルドサイズ: 6.1MB（要件の10MB以下を達成）

### タスク3完了（2025-07-21）
- TDD手法（Red→Green→Refactor）に従って実装
- System.IO.Abstractionsを使用してファイルシステムを抽象化
- ConfigServiceのテスト8件を作成（ファイルI/O、プロファイル管理、セキュリティ）
- APIキーの暗号化: Windows（DPAPI）、その他OS（Base64エンコーディング）
- すべてのテスト（23件）が成功

### タスク4完了（2025-07-21）
- TDD手法（Red→Green→Refactor）に従って実装
- WireMock.Netを使用したHTTP通信のモックテスト14件を作成
- RedmineApiClientの完全実装（CRUD操作、認証、エラーハンドリング）
- Native AOT対応のJSONシリアライゼーション（Source Generator使用）
- 共通HTTPリクエストメソッドの抽出によるコード品質向上
- すべてのテスト（37件）が成功

### タスク5完了（2025-07-21）
- TDD手法（Red→Green→Refactor）に従って実装
- System.CommandLine v2.0.0-beta6を使用したコマンド実装
- AuthCommandのテスト13件を作成（ログイン、ステータス、ログアウト）
- Spectre.Consoleを使用した対話的UI実装
- ヘルパーメソッド抽出によるコードの重複排除
- すべてのテスト（50件）が成功
- Native AOTビルド成功：起動時間15ms、バイナリサイズ4.9MB達成
- SetActionメソッドを使用したコマンドハンドラーの実装
- 実行ファイル名: redmine（プロジェクト名・AssemblyNameはRedmineCLIを維持）
- 接続テストの修正：URL/APIキーを直接指定できるオーバーロードを追加

### Native AOT対応改善（2025-07-21）
- VYamlとSpectre.ConsoleのAOT警告への対処
- ILLink.Descriptors.xmlを作成してシリアライゼーション関連の型を保護
- プロジェクトファイルで警告を抑制（IL2104, IL3053）
- TrimmerRootAssemblyにVYamlを追加
- 最終バイナリサイズ: 15MB（デバッグシンボルを除く）
- アプリケーションは正常に動作し、すべての機能が利用可能

### タスク6完了（2025-07-21）
- TDD手法（Red→Green→Refactor）に従って実装
- IssueCommandのテスト14件を作成（一覧表示、フィルタリング、ページネーション、フォーマット、--webオプション）
- GitHub CLI準拠の仕様に変更（デフォルトで全オープンチケット表示）
- ショートハンドオプション実装（-a, -s, -p, -L）
- @me特殊値サポート（現在のユーザーを指定）
- all特殊値サポート（全ステータス表示）
- IssueFilterモデルによるフィルタリング機能の実装
- TableFormatterとJsonFormatterインターフェースの実装
- GetCurrentUserAsync APIの追加（@me使用時に呼び出し）
- --web/-wオプションによるブラウザ連携機能の実装
  - set_filter=1パラメータ追加でRedmineフィルタ有効化
  - $BROWSER環境変数サポート（%sプレースホルダ対応）
  - クロスプラットフォーム対応（Windows/macOS/Linux）
- コンパイラ警告修正（CS8602, CS1998）
- すべてのテスト（62件）が成功
- Native AOTビルド成功：起動時間17ms、バイナリサイズ15MB

### タスク7完了（2025-07-21）
- TDD手法（Red→Green→Refactor）に従って実装
- テストケース7件を作成（詳細表示、履歴表示、エラー処理、JSON出力、ブラウザ起動）
- `issue view <ID>`コマンドの実装
- Journalモデルを追加してチケット履歴表示機能を実装
- GetIssueAsyncのオーバーロード追加（includeJournalsパラメータ）
- Spectre.Consoleによる美しいチケット詳細表示（パネル、グリッド、履歴）
- --web/-wオプションによる個別チケットのブラウザ表示
- ブラウザ起動処理の共通化（OpenInBrowserAsyncメソッド）
- すべてのテスト（71件）が成功

### タスク8完了（2025-07-21）
- TDD手法（Red→Green→Refactor）に従って実装
- テストケース7件を作成（対話的モード、オプション指定、URL表示、入力検証、ブラウザ起動、@me処理）
- `issue create`コマンドの実装
- 対話的モード：プロジェクト選択→タイトル入力→説明入力→自分に割り当て確認
- コマンドラインオプション：-p/--project, -t/--title, -d/--description, -a/--assignee
- @me特殊値のサポート（現在のユーザーに割り当て）
- APIリクエスト形式の修正：IssueCreateDataモデルを追加してproject_idフィールドを使用
- 作成成功時のチケットIDとURL表示（Markup.Escapeでエスケープ処理）
- --web/-wオプションによる新規作成ページのブラウザ起動（プロジェクト指定対応）
- 共通ロジックのリファクタリング：ResolveAssigneeAsync、ParseAssignee、ParseProject
- すべてのテスト（81件）が成功
- Native AOTビルド成功：バイナリサイズ15MB

### タスク9完了（2025-07-21）
- TDD手法（Red→Green→Refactor）に従って実装
- テストケース12件を作成（ステータス更新、担当者更新、進捗率更新、確認メッセージ、ブラウザ起動、@me処理など）
- `issue edit`コマンドの実装
- コマンドラインオプション：-s/--status, -a/--assignee, --done-ratio, -w/--web
- @me特殊値のサポート（ResolveAssigneeAsync共通メソッドを使用）
- 進捗率の範囲検証（0-100）
- 対話的モード：オプションを指定しない場合は対話的にフィールドを選択
  - 現在の値を表示してから編集フィールドを選択
  - ステータス、担当者、進捗率の個別編集
  - 変更内容の確認と保存/キャンセル機能
- 部分更新の実装：IssueUpdateDataモデルを追加してnullフィールドを除外
- Redmine API要件への対応：subjectフィールドを必ず含める（現在値を取得して設定）
- ステータス名からIDへの解決機能（GetIssueStatusesAsyncを使用）
- 更新成功時の詳細表示（どのフィールドを何に更新したかを表示）
- --web/-wオプションによる編集ページのブラウザ起動
- リファクタリング：更新フィールドの追跡と表示ロジックの改善
- すべてのテスト（93件）が成功

### タスク10完了（2025-07-21）
- TDD手法（Red→Green→Refactor）に従って実装
- テストケース6件を作成（メッセージ提供、エディタ起動、確認表示、空コメント処理、APIエラー処理、チケット未発見エラー）
- `issue comment <ID>`コマンドの完全実装
- -m/--messageオプションによる直接コメント入力
- $EDITOR環境変数を使用したエディタ統合（Windows: notepad、Unix: nano）
- エディタ失敗時のフォールバック：コンソール入力
- テンポラリファイル管理とクリーンアップ処理
- コメントテンプレート：指示行（#で始まる行を除外）
- 空コメント検証とエラー処理
- APIエラーの適切な処理（404 Issue not found等）
- 成功時の確認メッセージとIssue URLの表示
- コードリファクタリング：エディタ統合ロジックの分割とメソッド抽出
- 成功メッセージ表示の共通化（ShowSuccessMessageWithUrlAsyncメソッド）
- すべてのテスト（100件）が成功
- Native AOTビルド成功：バイナリサイズ15MB
- 要件6（コメント追加機能）の完全実装

### タスク11完了（2025-07-21）
- TDD手法（Red→Green→Refactor）に従って実装
- テストケース4件を作成（ライセンス表示、バージョン情報、ファイル生成、埋め込み）
- `redmine --licenses`オプションの実装（Spectre.Console美化表示）
- THIRD-PARTY-NOTICES.txtファイルの生成と管理
- ライセンス情報のキャッシュ機能と動的依存関係検出
- 正しい権利者情報の調査と修正
  - RedmineCLI: Arstella ltd.
  - System.CommandLine: .NET Foundation and Contributors
  - Spectre.Console: Patrik Svensson, Phil Scott, Nils Andresen, Cédric Luthi, Frank Ray
  - VYaml: hadashiA（URL修正：github.com/hadashiA/VYaml）
- Program.csにILicenseHelperのDI登録とライセンスオプション処理
- すべてのテスト（104件）が成功
- 要件9（ライセンス情報管理機能）の完全実装

### タスク12完了（2025-07-22）
- TDD手法（Red→Green→Refactor）に従って実装
- TimeHelperTestsを作成し、テストケース14件を実装
  - 相対時刻表示（just now, minutes ago, hours ago, days ago）
  - 年内の日付表示（MMM dd）、前年の日付表示（MMM dd, yyyy）
  - ローカル時刻変換、UTC時刻表示、カスタムフォーマット対応
- ITimeHelperインターフェースとTimeHelperクラスの実装
- TimeFormat列挙型とTimeSettingsモデルの追加
- TableFormatterにTimeHelper依存性注入とSetTimeFormatメソッドを追加
- IssueCommandに--absolute-timeオプションを追加（list、viewコマンド）
- 設定ファイルからtime.formatを読み取る機能を実装
- ConfigCommandを新規作成（config set/get/listコマンド）
- すべての既存テストをアップデート（新しいパラメータに対応）
- 要件2（日時表示の改善）の完全実装

### GitHub CLI形式への変更（2025-07-22）
- 相対時刻表示をGitHub CLI形式に変更
  - すべての相対時刻に"about"を追加（例: "about 2 hours ago"）
  - 1分未満の表示を"just now"から"less than a minute ago"に変更
  - 月・年単位の相対表示を実装（30日以上でも相対表示を継続）
  - 単数・複数形の処理をFormatDurationメソッドで統一
- TimeHelperTestsのテストケースを更新（"about"形式に対応）
- テストケース17件すべてが成功

## 使用方法

### 実行可能ファイル名
- 実行可能ファイル名は `redmine` （プロジェクト名は`RedmineCLI`を維持）
- Native AOTビルド後の実行例
  ```bash
  ./bin/Release/net9.0/linux-x64/publish/redmine auth login
  ./bin/Release/net9.0/linux-x64/publish/redmine auth status
  ./bin/Release/net9.0/linux-x64/publish/redmine auth logout
  ```