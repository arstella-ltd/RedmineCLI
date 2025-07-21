# 実装計画

このプロジェクトではt-wada流のテスト駆動開発（TDD）を採用します。
各機能の実装は以下のサイクルで進めます。
1. **Red**: まず失敗するテストを書く
2. **Green**: テストを通す最小限の実装を行う
3. **Refactor**: コードを改善する

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

- [ ] 4. Redmine APIクライアントの実装（TDD）
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

- [ ] 5. 認証コマンドの実装（TDD）
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

- [ ] 6. チケット一覧表示コマンドの実装（TDD）
  - **Red**: IssueListCommandのテストを先に作成
    - List_Should_ReturnAssignedIssues_When_NoOptionsSpecified
    - List_Should_ReturnFilteredIssues_When_StatusIsSpecified
    - List_Should_LimitResults_When_LimitOptionIsSet
    - List_Should_FormatAsJson_When_JsonOptionIsSet
  - **Green**: テストを通すための実装
    - `issue list`コマンドの基本実装
    - フィルタリングオプション（assignee、status、project）の実装
    - ページネーション対応（limit、offset）
    - TableFormatterとJsonFormatterの実装
  - **Refactor**: フォーマッターの抽象化とコードの整理
  - _要件: 2_

- [ ] 7. チケット詳細表示コマンドの実装（TDD）
  - **Red**: IssueViewCommandのテストを先に作成
    - View_Should_ShowIssueDetails_When_ValidIdProvided
    - View_Should_ShowHistory_When_JournalsExist
    - View_Should_ReturnError_When_IssueNotFound
    - View_Should_FormatAsJson_When_JsonOptionIsSet
  - **Green**: テストを通すための実装
    - `issue view`コマンドの実装
    - チケット履歴（journals）の表示機能
    - Spectre.Consoleによる整形された出力
    - JSON出力オプションの実装
  - **Refactor**: 出力ロジックの共通化
  - _要件: 3_

- [ ] 8. チケット作成コマンドの実装（TDD）
  - **Red**: IssueCreateCommandのテストを先に作成
    - Create_Should_CreateIssue_When_InteractiveMode
    - Create_Should_CreateIssue_When_OptionsProvided
    - Create_Should_ShowUrl_When_IssueCreated
    - Create_Should_ValidateInput_When_RequiredFieldsMissing
  - **Green**: テストを通すための実装
    - `issue create`コマンドの実装
    - 対話的な入力モード（プロジェクト選択、タイトル、説明入力）
    - コマンドラインオプションによる非対話的作成
    - 作成成功時のURL表示
  - **Refactor**: 入力検証ロジックの改善
  - _要件: 4_

- [ ] 9. チケット更新コマンドの実装（TDD）
  - **Red**: IssueEditCommandのテストを先に作成
    - Edit_Should_UpdateStatus_When_StatusOptionProvided
    - Edit_Should_UpdateAssignee_When_AssigneeOptionProvided
    - Edit_Should_UpdateProgress_When_DoneRatioProvided
    - Edit_Should_ShowConfirmation_When_UpdateSuccessful
  - **Green**: テストを通すための実装
    - `issue edit`コマンドの実装
    - ステータス、担当者、進捗率の更新機能
    - 対話的な更新内容選択UI
    - 部分的な更新（PATCH）の実装
  - **Refactor**: 更新ロジックの統合と最適化
  - _要件: 5_

- [ ] 10. コメント追加コマンドの実装（TDD）
  - **Red**: IssueCommentCommandのテストを先に作成
    - Comment_Should_AddComment_When_MessageProvided
    - Comment_Should_OpenEditor_When_NoMessageOption
    - Comment_Should_ShowConfirmation_When_CommentAdded
    - Comment_Should_HandleEmptyComment_When_NoTextEntered
  - **Green**: テストを通すための実装
    - `issue comment`コマンドの実装
    - エディタ統合（$EDITOR環境変数の利用）
    - メッセージオプションによる直接入力
    - コメント追加確認メッセージ
  - **Refactor**: エディタ統合ロジックの改善
  - _要件: 6_

- [ ] 11. 設定管理コマンドの実装（TDD）
  - **Red**: ConfigCommandのテストを先に作成
    - Set_Should_UpdateValue_When_ValidKeyProvided
    - Get_Should_ReturnValue_When_KeyExists
    - List_Should_ShowAllSettings_When_Called
    - Set_Should_ValidateKey_When_UnknownKeyProvided
  - **Green**: テストを通すための実装
    - `config set`コマンド：設定値の変更
    - `config get`コマンド：設定値の取得
    - `config list`コマンド：全設定の一覧表示
    - デフォルトプロジェクト等の設定管理
  - **Refactor**: 設定キーの検証とヘルプの改善
  - _要件: 7_

- [ ] 12. エラーハンドリングとログ機能の実装（TDD）
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

- [ ] 13. 統合テストとE2Eテストの実装
  - **注**: 各機能の単体テストは既にTDDで実装済み
  - **統合テスト**: 実際のファイルシステムとの連携テスト
    - 設定ファイルの読み書き統合テスト
    - コマンド間の連携テスト
  - **E2Eテスト**: WireMock.Netを使用したAPIモックテスト
    - 完全なコマンドシナリオのテスト
    - エラーシナリオのテスト
  - **カバレッジ**: 単体テストで80%以上、統合テスト含めて90%以上を目標
  - _要件: 全体的な基盤_

- [ ] 14. ドキュメントとパッケージング
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

- [ ] 15. 継続的テスト実行環境の整備（TDD文化の定着）
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
    - テストレビューチェックリストの作成
    - 「テストが書けない設計は悪い設計」の原則の徹底
  - _要件: 全体的な基盤_