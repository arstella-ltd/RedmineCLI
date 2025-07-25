# リリースワークフロー

このドキュメントでは、RedmineCLIのリリース手順について説明します。

## 現在のリリースワークフロー

### 自動化されているプロセス

1. **トリガー**: `v*`形式のタグがプッシュされると自動的にリリースワークフローが開始
2. **ビルド**: 以下のプラットフォーム向けにNative AOTでコンパイル
   - Windows x64 (zip形式)
   - macOS x64/ARM64 (zip形式)
   - Linux x64/ARM64 (zip形式)
3. **リリース作成**: GitHubリリースが自動作成（softprops/action-gh-release@v2使用）
4. **checksums.txt生成**: 全アセットのSHA256ハッシュを自動生成
5. **Homebrew更新**: 安定版リリース時に自動でFormulaを更新
6. **Scoop更新**: scoop-bucketリポジトリで6時間ごとに自動チェック

### バージョン表示ルール

- **安定版** (v0.8.1): コミットハッシュなし → `0.8.1`
- **プレリリース版** (v0.8.1-beta.1): コミットハッシュなし → `0.8.1-beta.1`
- **開発版** (タグなし): コミットハッシュあり → `0.8.0+abc123...`

リリースビルドでは `-p:IncludeSourceRevisionInInformationalVersion=false` を使用してコミットハッシュを除外します。これにより、リリース版では常にクリーンなバージョン番号が表示されます。

## 推奨されるリリースプロセス

### 1. リリース準備

```bash
# バージョンを決定（例: v0.9.0）
VERSION=v0.9.0

# CHANGELOGの更新（存在する場合）
# テストの実行
dotnet test

# Native AOTビルドの確認
dotnet publish -c Release -r win-x64 -p:PublishAot=true
```

### 2. タグの作成とプッシュ

```bash
# タグを作成
git tag -a $VERSION -m "Release $VERSION"

# タグをプッシュ（これによりGitHub Actionsが起動）
git push origin $VERSION
```

### 3. リリースの確認

GitHub Actionsによって以下が自動的に実行されます：

1. 全プラットフォーム向けのバイナリビルド（zip形式）
2. リリースの作成（現在は即座に公開）
3. checksums.txtの生成とアップロード
4. Homebrew/Scoopの自動更新（安定版のみ）

### 4. リリースノートの確認と編集

1. [GitHub Releases](https://github.com/arstella-ltd/RedmineCLI/releases)ページを開く
2. 作成されたリリースを確認
3. 必要に応じてリリースノートを編集：
   - 主な変更点
   - 新機能
   - バグ修正
   - Breaking Changes（ある場合）

## 実装済みの機能

### 現在のrelease.ymlの主な機能

1. **最新のリリースアクション**
   ```yaml
   uses: softprops/action-gh-release@v2
   ```
   - 権限エラーを回避
   - より柔軟な設定が可能

2. **全プラットフォームでzip形式に統一済み**
   - Windows/macOS/Linux全てzip形式
   - 一貫性のあるアセット管理

3. **checksums.txt自動生成**
   ```yaml
   uses: robinraju/release-downloader@v1.11
   ```
   - 全アセットのSHA256ハッシュを含む
   - Scoop等のパッケージマネージャーで利用

4. **バージョンの動的設定**
   ```yaml
   -p:Version=${{ steps.extract_version.outputs.version }}
   -p:IncludeSourceRevisionInInformationalVersion=false
   ```
   - タグからバージョンを自動設定
   - コミットハッシュを明示的に除外（MSBuild標準プロパティ）

5. **Windows/Unix両対応のバージョン抽出**
   - Windows: PowerShellスクリプト
   - Unix: Bashスクリプト

### 今後の改善案

1. **ドラフトリリースの有効化**
   ```yaml
   draft: true  # 現在はfalse
   ```
   - リリースノートを編集する時間を確保
   - 誤った公開を防ぐ

2. **リリースノートの自動生成**
   - コミットメッセージから自動生成
   - Conventional Commitsの活用

## プレリリース版の考え方

### プレリリースの種類と用途

1. **アルファ版** (`v0.9.0-alpha.1`)
   - 内部テスト用
   - 新機能の初期実装
   - 不安定な可能性あり
   - 限定的な配布

2. **ベータ版** (`v0.9.0-beta.1`)
   - 公開テスト用
   - 機能はほぼ完成
   - フィードバック収集目的
   - 広く配布

3. **リリース候補版** (`v0.9.0-rc.1`)
   - 最終テスト用
   - 安定版とほぼ同等
   - 最終確認のみ
   - 本番環境での使用可

### プレリリースの命名規則

```
v<メジャー>.<マイナー>.<パッチ>-<プレリリース>.<番号>

例:
- v0.9.0-alpha.1
- v0.9.0-beta.1
- v0.9.0-beta.2
- v0.9.0-rc.1
```

### プレリリースのリリース手順

```bash
# プレリリース版のタグを作成
VERSION=v0.9.0-beta.1
git tag -a $VERSION -m "Release $VERSION"
git push origin $VERSION
```

### プレリリース版の特徴

1. **自動化**
   - GitHub Actionsが自動的にprereleaseフラグを設定
   - `-`を含むバージョンは自動的にプレリリースとして扱われる

2. **配布**
   - GitHubリリースページで入手可能
   - Homebrewの自動更新はスキップ
   - Scoopは手動更新が必要

3. **バージョン表示**
   - コミットハッシュなし（例: `0.9.0-beta.1`）
   - 正式リリースと同じ扱い

### プレリリースの進行例

```bash
# 新機能開発開始
v0.9.0-alpha.1  # 初期実装
v0.9.0-alpha.2  # バグ修正
v0.9.0-alpha.3  # 機能追加

# ベータテスト開始
v0.9.0-beta.1   # 公開テスト開始
v0.9.0-beta.2   # フィードバック反映
v0.9.0-beta.3   # バグ修正

# リリース候補
v0.9.0-rc.1     # 最終確認
v0.9.0-rc.2     # 微調整

# 正式リリース
v0.9.0          # 安定版リリース
```

## トラブルシューティング

### リリースが失敗した場合

1. [Actions](https://github.com/arstella-ltd/RedmineCLI/actions)ページでエラーを確認
2. 必要に応じてタグを削除して再実行：
   ```bash
   git tag -d $VERSION
   git push origin :refs/tags/$VERSION
   ```

### Homebrew更新が失敗した場合

手動で更新PRを作成：

```bash
# Homebrew tapリポジトリをクローン
git clone https://github.com/arstella-ltd/homebrew-tap
cd homebrew-tap

# Formulaを更新
# Formula/redmine.rb を編集してバージョンとSHA256を更新

# PRを作成
git checkout -b update-redmine-$VERSION
git add Formula/redmine.rb
git commit -m "Update RedmineCLI to $VERSION"
git push origin update-redmine-$VERSION
```

## Scoop更新の確認

リリース後、Scoop bucketの自動更新を確認：

1. [scoop-bucket](https://github.com/arstella-ltd/scoop-bucket)リポジトリを確認
2. GitHub Actionsで`excavator`ワークフローが実行されることを確認
3. 自動更新が失敗した場合は手動で更新：
   ```bash
   # scoop-bucketリポジトリで
   scoop hash https://github.com/arstella-ltd/RedmineCLI/releases/download/v$VERSION/redmine-cli-win-x64.zip
   # bucket/redmine.jsonのhashを更新
   ```

## バージョン管理の技術詳細

### MSBuildプロパティ

RedmineCLIでは以下のMSBuildプロパティを使用してバージョンを制御：

1. **Version**: 基本バージョン番号（例: `0.8.1`）
2. **IncludeSourceRevisionInInformationalVersion**: コミットハッシュの付与を制御
   - `false`: リリースビルド（ハッシュなし）
   - `true`（デフォルト）: 開発ビルド（ハッシュあり）

### なぜIncludeSourceRevisionInInformationalVersionを使用するか

- **明示的**: 意図が明確（「InformationalVersionにソースリビジョンを含めない」）
- **標準的**: MSBuildの公式プロパティ
- **保守性**: 将来のMSBuildバージョンでも互換性が保たれる

## 注意事項

- セマンティックバージョニングに従う（MAJOR.MINOR.PATCH）
- Breaking Changesがある場合はメジャーバージョンを上げる
- リリース前に必ずテストを実行する
- リリースノートには日本語で記載する（技術用語は英語可）
- checksums.txtが正しく生成されることを確認（Scoop自動更新に必要）