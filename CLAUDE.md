# CLAUDE.md

このファイルは、このリポジトリでコード作業を行う際のClaude Code (claude.ai/code) への指針を提供します。

## プロジェクト概要

RedmineCLIは、Redmineチケットを管理するためのコマンドラインインターフェースツールで、GitHub CLI (`gh`) のような体験を提供するよう設計されています。
高速起動（< 100ms）と小さなバイナリサイズ（< 10MB）を実現するため、.NET 9のNative AOTコンパイルを使用しています。
実行ファイル名は`redmine`で、プロジェクト名・名前空間は`RedmineCLI`を維持しています。

## 会話の言語

このリポジトリでは、**すべての会話は日本語で行ってください**。これにはGitHubのissue、pull request、コード内のコメント、コミットメッセージなどが含まれます。
英語の技術用語（例：API、JSON、AOT など）はそのまま使用して構いませんが、説明や文章は日本語で記述してください。

## ビルドコマンド

```bash
# プロジェクトのビルド
dotnet build

# アプリケーションの実行
dotnet run -- --help

# 特定のコマンドで実行
dotnet run -- issue list
dotnet run -- auth login
```

## Native AOT パブリッシング

```bash
# Windows
dotnet publish -c Release -r win-x64 -p:PublishAot=true -p:StripSymbols=true

# macOS (Intel/Apple Silicon)
dotnet publish -c Release -r osx-x64 -p:PublishAot=true -p:StripSymbols=true
dotnet publish -c Release -r osx-arm64 -p:PublishAot=true -p:StripSymbols=true

# Linux (x64/ARM64)
dotnet publish -c Release -r linux-x64 -p:PublishAot=true -p:StripSymbols=true
dotnet publish -c Release -r linux-arm64 -p:PublishAot=true -p:StripSymbols=true
```

## テスト

### テストの実行

```bash
# 全テストの実行
dotnet test

# カバレッジ付きで実行
dotnet test --collect:"XPlat Code Coverage"

# 特定のテストカテゴリの実行
dotnet test --filter Category=Unit
dotnet test --filter FullyQualifiedName~IssueCommandTests

# 単一のテストメソッドの実行
dotnet test --filter "FullyQualifiedName=RedmineCLI.Tests.Commands.IssueCommandTests.List_Should_ReturnFilteredIssues_When_StatusIsSpecified"
```

### t-wada流のテスト方針

このプロジェクトでは、和田卓人（t-wada）氏が推奨するテスト手法に従います。

1. **テスト駆動開発（TDD）**
   - Red → Green → Refactorのサイクルを徹底
   - まずは失敗するテストを書いてから実装

2. **テストのAAA（Arrange-Act-Assert）パターン**
   ```csharp
   // Arrange: テストの準備
   var service = new RedmineService(mockApiClient.Object);
   var expectedIssues = new List<Issue> { ... };
   
   // Act: テスト対象の実行
   var result = await service.GetIssuesAsync();
   
   // Assert: 結果の検証
   result.Should().BeEquivalentTo(expectedIssues);
   ```

3. **テストコードの品質**
   - プロダクションコードと同等の品質を保つ
   - テストの意図が明確になる命名
   - 1テスト1アサーション原則

4. **テストダブルの使い分け**
   - スタブ：依存オブジェクトの代替（状態の検証）
   - モック：振る舞いの検証が必要な場合のみ使用
   - できるだけ本物のオブジェクトを使用

5. **テストピラミッド**
   - 単体テスト（多） > 統合テスト（中） > E2Eテスト（少）
   - 高速でフィードバックを得られるテストを重視

## アーキテクチャ

アプリケーションはNative AOT向けに最適化されたレイヤードアーキテクチャに従います。

```
ユーザー入力 → System.CommandLine → コマンドハンドラー → サービス層 → APIクライアント → Redmine API
                                         ↓              ↓
                                   設定サービス   フォーマッター → コンソール出力
```

### 主要コンポーネント

- **Commands/**: System.CommandLineを使用したコマンド実装
- **Services/**: 依存性注入を使用したビジネスロジック（AOT最適化済み）
- **ApiClient/**: Pollyリトライポリシーを使用したHTTP通信
- **Models/**: System.Text.Json Source Generatorsを使用したデータモデル
- **Formatters/**: Spectre.Consoleを使用した出力フォーマット（テーブル/JSON）

### AOT考慮事項

- JSONシリアライゼーションにはSource Generatorを使用（リフレクションなし）
- 動的コード生成を最小限に抑える
- すべてのNuGetパッケージがAOT互換であることを確認
- 可能な限りコンパイル時の依存性注入を使用

## コマンド構造

```bash
redmine auth login       # 対話的な認証
redmine issue list       # チケット一覧（デフォルト：自分に割り当てられたもの）
redmine issue view <ID>  # チケット詳細の表示
redmine issue create     # 新規チケット作成（対話的）
redmine config set <key> <value>  # 設定の変更
```

## 設定

設定ファイルはYAML形式で以下に保存されます。
- Windows: `%APPDATA%\redmine\config.yml`
- macOS/Linux: `~/.config/redmine/config.yml`

## 開発ワークフロー

### TDD（テスト駆動開発）のサイクル

1. **Red**: 失敗するテストを書く
   ```bash
   # 新しいテストを追加
   # テストが失敗することを確認
   dotnet test --filter "新しいテスト名"
   ```

2. **Green**: テストを通す最小限の実装
   ```bash
   # 実装を追加
   # テストが通ることを確認
   dotnet test --filter "新しいテスト名"
   ```

3. **Refactor**: コードを改善
   ```bash
   # リファクタリング実施
   # 全テストが通ることを確認
   dotnet test
   ```

### 通常の開発フロー

1. TDDサイクルで機能を実装
2. AOT制約に従っているか確認
3. `dotnet build`でコンパイルを確認
4. `dotnet run -- <command>`でローカル動作確認
5. `dotnet test`で全テストを実行
6. `dotnet publish -c Release -r <RID> -p:PublishAot=true`でAOTビルドを確認

## 重要な制約

- **Native AOT**: すべてのコードはAOT互換である必要（最小限のリフレクション、動的アセンブリ読み込みなし）
- **起動時間**: 100ms以下である必要
- **バイナリサイズ**: AOTコンパイル後10MB以下である必要
- **ランタイム依存なし**: .NETランタイム不要の単一実行ファイル
- **エラーハンドリング**: ユーザーフレンドリーなメッセージを持つカスタム例外を使用

## ドキュメント構造

- `docs/PRODUCT.md`: 製品ビジョンと機能
- `docs/REQUIREMENTS.md`: 受け入れ基準を含む詳細な要件
- `docs/DESIGN.md`: 技術アーキテクチャと設計決定
- `docs/TECH.md`: 技術スタックと開発コマンド
- `docs/TODO.md`: AOT考慮事項を含む実装計画
- `docs/STRUCTURE.md`: プロジェクト構造（src/ディレクトリなし、フラットレイアウト）
- `docs/RELEASE.md`: リリースワークフロー、GitHub Actionsの設定、Homebrew更新手順
- `docs/EXTENSION.md`: 拡張機能システムの仕様
- `docs/RULES.md`: ドキュメント作成ルール
- `docs/TEST.md`: ユニットテストガイド

## タスク別参照マップ

以下は、タスクごとに参照すべきドキュメントの順番を示した参照マップです。各タスクを実行する際は、リストされたドキュメントを順番に確認してください。

| タスク | 主要ドキュメント |
|------|------------------|
| 新しいコマンドの追加 | REQUIREMENTS.md → DESIGN.md → STRUCTURE.md → TEST.md → TODO.md |
| 新しいAPIエンドポイントの実装 | DESIGN.md → TECH.md → TEST.md |
| テストの作成 | TEST.md → CLAUDE.md（テスト方針） → STRUCTURE.md |
| 拡張機能の開発 | EXTENSION.md → DESIGN.md → TECH.md |
| AOT最適化/パフォーマンス改善 | CLAUDE.md（AOT考慮事項） → TECH.md → DESIGN.md |
| エラーハンドリングの改善 | DESIGN.md → REQUIREMENTS.md → TEST.md |
| 出力フォーマッターの追加 | DESIGN.md → STRUCTURE.md → TEST.md |
| CI/CDの設定変更 | RELEASE.md → TECH.md |
| リリース作業 | RELEASE.md → TODO.md → PRODUCT.md |
| 新機能の企画・設計 | PRODUCT.md → REQUIREMENTS.md → DESIGN.md → TODO.md |
| ドキュメントの更新 | RULES.md → 対象ドキュメント |
| バグ修正 | CLAUDE.md（開発ワークフロー） → TEST.md → 関連するドキュメント |