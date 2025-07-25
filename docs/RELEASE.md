# リリースワークフロー

このドキュメントでは、RedmineCLIのリリース手順について説明します。

## 現在のリリースワークフロー

### 自動化されているプロセス

1. **トリガー**: `v*`形式のタグがプッシュされると自動的にリリースワークフローが開始
2. **ビルド**: 以下のプラットフォーム向けにNative AOTでコンパイル
   - Windows x64 (zip形式)
   - macOS x64/ARM64 (tar.gz形式)
   - Linux x64/ARM64 (tar.gz形式)
3. **リリース作成**: GitHubリリースが自動作成（現在は即座に公開）
4. **Homebrew更新**: 安定版リリース時に自動でFormulaを更新

### 問題点

- リリースが即座に公開される（ドラフトではない）
- Unix系プラットフォームでtar.gz形式を使用（zipに統一したい）
- リリースノートがハードコードされている

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

### 3. ドラフトリリースの確認

GitHub Actionsによって以下が自動的に実行されます：

1. 全プラットフォーム向けのバイナリビルド
2. ドラフトリリースの作成
3. ビルド済みバイナリのアップロード

### 4. リリースノートの編集

1. [GitHub Releases](https://github.com/arstella-ltd/RedmineCLI/releases)ページを開く
2. ドラフトリリースを確認
3. リリースノートを編集：
   - 主な変更点
   - 新機能
   - バグ修正
   - Breaking Changes（ある場合）

### 5. リリースの公開

1. リリースノートの編集が完了したら「Publish release」をクリック
2. Homebrewの自動更新が開始される（安定版のみ）

## GitHub Actionsワークフローの改善案

### ドラフトリリースの有効化

`.github/workflows/release.yml`の変更：

```yaml
- name: Create Release
  id: create_release
  uses: actions/create-release@v1
  env:
    GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
  with:
    tag_name: ${{ github.ref_name }}
    release_name: RedmineCLI ${{ steps.extract_version.outputs.version }}
    body: ${{ steps.release_notes.outputs.release_notes }}
    draft: true  # ← ドラフトとして作成
    prerelease: ${{ contains(steps.extract_version.outputs.version, '-') }}
```

### 全プラットフォームでzip形式に統一

Unix系プラットフォームでもzip形式を使用：

```yaml
- name: Compress binary (Unix)
  if: runner.os != 'Windows'
  run: |
    cd publish/${{ matrix.config.rid }}
    zip -j ../../${{ matrix.config.asset_name }}.zip ${{ matrix.config.output }}
    cd ../..
```

### リリース公開時のHomebrew更新

別ワークフロー（`.github/workflows/homebrew-update.yml`）を追加：

```yaml
name: Update Homebrew Formula

on:
  release:
    types: [published]

jobs:
  update-homebrew:
    name: Update Homebrew Formula
    runs-on: ubuntu-latest
    if: "!github.event.release.prerelease"
    
    steps:
    - name: Update Homebrew formula
      uses: dawidd6/action-homebrew-bump-formula@v3
      with:
        token: ${{ secrets.HOMEBREW_TOKEN }}
        formula: redmine
        tag: ${{ github.event.release.tag_name }}
        revision: ${{ github.sha }}
```

## プレリリース版

アルファ版やベータ版をリリースする場合：

```bash
# プレリリース版のタグ（例: v0.9.0-beta.1）
git tag -a v0.9.0-beta.1 -m "Release v0.9.0-beta.1"
git push origin v0.9.0-beta.1
```

プレリリース版の場合：
- GitHubリリースで「This is a pre-release」にチェック
- Homebrewの自動更新はスキップされる

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

## 注意事項

- セマンティックバージョニングに従う（MAJOR.MINOR.PATCH）
- Breaking Changesがある場合はメジャーバージョンを上げる
- リリース前に必ずテストを実行する
- リリースノートには日本語で記載する（技術用語は英語可）
- checksums.txtが正しく生成されることを確認（Scoop自動更新に必要）