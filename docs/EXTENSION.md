# RedmineCLI拡張機能（Extensions）仕様

このドキュメントは、RedmineCLIの拡張機能システムの仕様と開発ガイドラインを記述します。

## 概要

RedmineCLIの拡張機能システムは、GitHub CLI（`gh`）の拡張機能モデルに基づいており、Native AOTでコンパイルされた独立した実行ファイルとして実装されます。これにより、RedmineCLI本体の高速起動とランタイム非依存性を維持しながら、機能を拡張できます。

## 設計原則

1. **Native AOT優先**: 拡張機能もRedmineCLI本体と同様にNative AOTでコンパイルすることを推奨
2. **プロセス分離**: 各拡張機能は独立したプロセスとして実行され、本体の安定性に影響しない
3. **環境変数による通信**: 認証情報や設定は環境変数経由で安全に受け渡し
4. **シンプルなインターフェース**: 標準入出力による単純な通信モデル

## 拡張機能の命名規則

拡張機能は以下の命名規則に従います：

- 実行ファイル名: `redmine-<extension-name>`
- 例: `redmine-forum`, `redmine-report`, `redmine-backup`

ユーザーは以下のように拡張機能を実行します：

```bash
redmine forum list      # redmine-forum list を実行
redmine report generate # redmine-report generate を実行
```

## 拡張機能の開発

### プロジェクト構造

```
RedmineCLI.Extension.Forum/
├── RedmineCLI.Extension.Forum.csproj
├── Program.cs
├── ForumExtension.cs
├── Models/
│   └── Forum.cs
└── README.md
```

### プロジェクトファイル（Native AOT）

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net9.0</TargetFramework>
    <AssemblyName>redmine-forum</AssemblyName>
    <Nullable>enable</Nullable>
    
    <!-- Native AOT設定 -->
    <PublishAot>true</PublishAot>
    <StripSymbols>true</StripSymbols>
    <InvariantGlobalization>true</InvariantGlobalization>
    <JsonSerializerIsReflectionEnabledByDefault>false</JsonSerializerIsReflectionEnabledByDefault>
    <EnableTrimAnalyzer>true</EnableTrimAnalyzer>
    <TrimMode>link</TrimMode>
  </PropertyGroup>
  
  <ItemGroup>
    <!-- AOT互換のパッケージのみを使用 -->
    <PackageReference Include="System.Text.Json" Version="9.0.0" />
    <PackageReference Include="System.CommandLine" Version="2.0.0-beta4.22272.1" />
  </ItemGroup>
</Project>
```

### 実装例

```csharp
using System;
using System.CommandLine;
using System.Text.Json;
using System.Threading.Tasks;

namespace RedmineCLI.Extension.Forum;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("Redmine forum extension");
        
        var listCommand = new Command("list", "List forum topics");
        listCommand.SetHandler(async () =>
        {
            var extension = new ForumExtension();
            await extension.ListForumsAsync();
        });
        
        var postCommand = new Command("post", "Create a new forum post");
        postCommand.Add(new Argument<string>("title", "Post title"));
        postCommand.Add(new Argument<string>("body", "Post body"));
        postCommand.SetHandler(async (string title, string body) =>
        {
            var extension = new ForumExtension();
            await extension.CreatePostAsync(title, body);
        }, postCommand.Arguments[0], postCommand.Arguments[1]);
        
        rootCommand.Add(listCommand);
        rootCommand.Add(postCommand);
        
        return await rootCommand.InvokeAsync(args);
    }
}

public class ForumExtension
{
    private readonly string _redmineUrl;
    private readonly string _apiKey;
    private readonly JsonSerializerOptions _jsonOptions;
    
    public ForumExtension()
    {
        _redmineUrl = Environment.GetEnvironmentVariable("REDMINE_URL") 
            ?? throw new InvalidOperationException("REDMINE_URL not set");
        _apiKey = Environment.GetEnvironmentVariable("REDMINE_API_KEY") 
            ?? throw new InvalidOperationException("REDMINE_API_KEY not set");
        
        // AOT対応のJsonSerializerOptions
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            TypeInfoResolver = ForumJsonContext.Default
        };
    }
    
    public async Task ListForumsAsync()
    {
        // フォーラム一覧の取得（実装例）
        Console.WriteLine("Forum Topics:");
        Console.WriteLine("1. General Discussion");
        Console.WriteLine("2. Development");
        Console.WriteLine("3. Support");
    }
    
    public async Task CreatePostAsync(string title, string body)
    {
        Console.WriteLine($"Creating post: {title}");
        // 実際の投稿処理
    }
}

// AOT用のJsonSerializerContext
[JsonSerializable(typeof(Forum))]
[JsonSerializable(typeof(List<Forum>))]
public partial class ForumJsonContext : JsonSerializerContext { }

public class Forum
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int PostCount { get; set; }
    public DateTime LastPostDate { get; set; }
}
```

## 環境変数

RedmineCLIは、拡張機能に以下の環境変数を自動的に設定します：

| 環境変数 | 説明 | 例 |
|---------|------|-----|
| `REDMINE_URL` | RedmineサーバーのURL | `https://redmine.example.com` |
| `REDMINE_API_KEY` | API認証キー | `abc123...` |
| `REDMINE_USER` | 現在のユーザー名 | `john.doe` |
| `REDMINE_PROJECT` | デフォルトプロジェクト | `my-project` |
| `REDMINE_CONFIG_DIR` | 設定ディレクトリパス | `~/.config/redmine` |
| `REDMINE_TIME_FORMAT` | 時間表示形式 | `relative` or `absolute` |
| `REDMINE_OUTPUT_FORMAT` | 出力形式 | `table` or `json` |

## ビルドと配布

### Native AOTビルド（推奨）

```bash
# Windows
dotnet publish -c Release -r win-x64 -p:PublishAot=true -p:StripSymbols=true

# macOS (Intel/Apple Silicon)
dotnet publish -c Release -r osx-x64 -p:PublishAot=true -p:StripSymbols=true
dotnet publish -c Release -r osx-arm64 -p:PublishAot=true -p:StripSymbols=true

# Linux
dotnet publish -c Release -r linux-x64 -p:PublishAot=true -p:StripSymbols=true
dotnet publish -c Release -r linux-arm64 -p:PublishAot=true -p:StripSymbols=true
```

### Self-Contained Deployment（代替案）

Native AOTが使用できない場合（AOT非対応のライブラリを使用する場合など）：

```bash
dotnet publish -c Release -r win-x64 --self-contained
```

注意: Self-Containedの場合、サイズが大きくなり（60-100MB）、起動時間も遅くなります。

## インストール方法

### 手動インストール

1. 拡張機能の実行ファイルをダウンロード
2. 以下のディレクトリに配置：
   - Windows: `%LOCALAPPDATA%\redmine\extensions\`
   - macOS/Linux: `~/.local/share/redmine/extensions/`
3. 実行権限を付与（Unix系のみ）：
   ```bash
   chmod +x ~/.local/share/redmine/extensions/redmine-forum
   ```

### 将来の拡張機能管理コマンド（計画中）

```bash
# インストール
redmine extension install https://github.com/user/redmine-forum/releases/latest

# 一覧表示
redmine extension list

# 削除
redmine extension remove forum

# 更新
redmine extension update forum
```

## 拡張機能の検索パス

RedmineCLIは以下の順序で拡張機能を検索します：

1. ユーザー拡張機能ディレクトリ
   - Windows: `%LOCALAPPDATA%\redmine\extensions\`
   - macOS/Linux: `~/.local/share/redmine/extensions/`
2. RedmineCLI実行ファイルと同じディレクトリ
3. PATH環境変数に含まれるディレクトリ

## セキュリティに関する考慮事項

1. **プロセス分離**: 各拡張機能は独立したプロセスとして実行
2. **環境変数の保護**: APIキーなどの機密情報は環境変数経由でのみ渡される
3. **信頼できるソース**: 信頼できるソースからのみ拡張機能をインストール

## AOT互換性のガイドライン

Native AOTでビルドする際の注意点：

1. **リフレクションの制限**: 
   - 動的な型生成は使用不可
   - `Type.GetType(string)` のような実行時型解決は避ける

2. **JSONシリアライゼーション**:
   - Source Generatorを使用
   - `JsonSerializerContext` を定義

3. **依存関係**:
   - すべての依存パッケージがAOT互換であることを確認
   - AOT非対応のパッケージは使用しない

4. **トリミング警告**:
   - ビルド時の警告をすべて解決
   - `<TrimmerRootAssembly>` で必要なアセンブリを保持

## トラブルシューティング

### Native AOTビルドエラー

```
ILC : error IL3050: Using member 'System.Type.GetType(String)' which has 'RequiresDynamicCodeAttribute'
```

**解決方法**: 動的な型解決を避け、コンパイル時に型を確定させる

### 環境変数が見つからない

```
System.InvalidOperationException: REDMINE_URL not set
```

**解決方法**: RedmineCLI経由で拡張機能を実行しているか確認

### バイナリサイズが大きい

**解決方法**: 
- `<StripSymbols>true</StripSymbols>` を設定
- 不要な依存関係を削除
- `<TrimMode>link</TrimMode>` でアグレッシブなトリミング

## 拡張機能の例

- **redmine-forum**: Webスクレイピングによるフォーラム操作
- **redmine-report**: カスタムレポート生成
- **redmine-backup**: プロジェクトデータのバックアップ
- **redmine-import**: 外部データのインポート

## まとめ

RedmineCLIの拡張機能システムは、Native AOTによる高速起動とランタイム非依存性を維持しながら、柔軟な機能拡張を可能にします。GitHub CLIの拡張機能モデルに基づいたシンプルな設計により、開発者は独立した実行ファイルとして拡張機能を作成・配布できます。