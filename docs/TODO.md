# 実装計画

- [ ] 1. プロジェクトのセットアップと基本構造の作成
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

- [ ] 2. データモデルとインターフェースの定義
  - Issue、Project、User等の基本モデルクラスを作成（AOT向けにSource Generator対応）
  - IRedmineApiClient、IConfigService等のインターフェース定義
  - Config、Profile、Preferences等の設定モデルを作成
  - APIレスポンス用のDTOクラスを定義（System.Text.Json Source Generator使用）
  - _要件: 全体的な基盤_

- [ ] 3. 設定管理機能の実装
  - AOT互換のYAMLライブラリ選定（YamlDotNetのAOT対応確認）
  - ConfigServiceの実装（リフレクション最小化）
  - 設定ファイルの読み込み・保存機能
  - 複数プロファイルの管理機能
  - APIキーの暗号化/復号化処理（Data Protection API使用）
  - _要件: 1, 7_

- [ ] 4. Redmine APIクライアントの実装
  - HttpClientFactoryを使用したRedmineApiClientの実装
  - Pollyによるリトライポリシーの設定
  - 認証ヘッダー（APIキー）の自動付与
  - エラーハンドリングとカスタム例外の実装
  - _要件: 1_

- [ ] 5. 認証コマンドの実装
  - `auth login`コマンド：対話的な認証情報入力
  - `auth status`コマンド：接続状態の確認
  - `auth logout`コマンド：認証情報のクリア
  - Spectre.Consoleを使用した対話的UI
  - _要件: 1_

- [ ] 6. チケット一覧表示コマンドの実装
  - `issue list`コマンドの基本実装
  - フィルタリングオプション（assignee、status、project）の実装
  - ページネーション対応（limit、offset）
  - TableFormatterとJsonFormatterの実装
  - _要件: 2_

- [ ] 7. チケット詳細表示コマンドの実装
  - `issue view`コマンドの実装
  - チケット履歴（journals）の表示機能
  - Spectre.Consoleによる整形された出力
  - JSON出力オプションの実装
  - _要件: 3_

- [ ] 8. チケット作成コマンドの実装
  - `issue create`コマンドの実装
  - 対話的な入力モード（プロジェクト選択、タイトル、説明入力）
  - コマンドラインオプションによる非対話的作成
  - 作成成功時のURL表示
  - _要件: 4_

- [ ] 9. チケット更新コマンドの実装
  - `issue edit`コマンドの実装
  - ステータス、担当者、進捗率の更新機能
  - 対話的な更新内容選択UI
  - 部分的な更新（PATCH）の実装
  - _要件: 5_

- [ ] 10. コメント追加コマンドの実装
  - `issue comment`コマンドの実装
  - エディタ統合（$EDITOR環境変数の利用）
  - メッセージオプションによる直接入力
  - コメント追加確認メッセージ
  - _要件: 6_

- [ ] 11. 設定管理コマンドの実装
  - `config set`コマンド：設定値の変更
  - `config get`コマンド：設定値の取得
  - `config list`コマンド：全設定の一覧表示
  - デフォルトプロジェクト等の設定管理
  - _要件: 7_

- [ ] 12. エラーハンドリングとログ機能の実装
  - グローバルエラーハンドラーの実装
  - Microsoft.Extensions.Loggingによるログ出力
  - デバッグモードの実装（--debugフラグ）
  - ユーザーフレンドリーなエラーメッセージ
  - _要件: 全体的な基盤_

- [ ] 13. テストの実装
  - xUnitによる単体テストの作成
  - Moqを使用したモックテスト
  - WireMock.Netによる統合テスト
  - コードカバレッジ80%以上を目標
  - _要件: 全体的な基盤_

- [ ] 14. ドキュメントとパッケージング
  - README.mdの作成（インストール手順、使用方法）
  - dotnet toolとしてのパッケージング設定
  - 各プラットフォーム向けネイティブバイナリのリリースビルド
  - 実行ファイルサイズの最終最適化
  - リリースノートの作成
  - _要件: 全体的な基盤_