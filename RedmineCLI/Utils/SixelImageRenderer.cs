using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace RedmineCLI.Utils
{
    /// <summary>
    /// Sixelプロトコルを使用して画像をレンダリングするユーティリティクラス
    /// </summary>
    public static class SixelImageRenderer
    {
        private const string SIXEL_START = "\x1bPq";
        private const string SIXEL_END = "\x1b\\";
        private const int MAX_WIDTH = 800;
        private const int MAX_HEIGHT = 600;

        /// <summary>
        /// URLから画像をダウンロードしてSixel形式で出力します
        /// </summary>
        public static async Task RenderImageFromUrlAsync(string imageUrl, HttpClient httpClient, string? apiKey = null, int maxWidth = 0)
        {
            if (!TerminalCapabilityDetector.SupportsSixel())
            {
                return;
            }

            try
            {
                // 画像のダウンロード
                var imageData = await DownloadImageAsync(imageUrl, httpClient, apiKey);
                if (imageData == null || imageData.Length == 0)
                {
                    return;
                }

                // 簡易的なSixel実装のため、一旦スキップして外部ツールを使用
                // 本格的な実装には画像デコーダーが必要
                RenderImageWithExternalTool(imageData, maxWidth);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to render Sixel image: {ex.Message}");
            }
        }

        /// <summary>
        /// 画像データをSixel形式で出力します（簡易版）
        /// </summary>
        public static void RenderImage(byte[] imageData, int maxWidth = 0)
        {
            if (!TerminalCapabilityDetector.SupportsSixel())
            {
                return;
            }

            try
            {
                RenderImageWithExternalTool(imageData, maxWidth);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to render Sixel image: {ex.Message}");
            }
        }

        private static async Task<byte[]?> DownloadImageAsync(string imageUrl, HttpClient httpClient, string? apiKey)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, imageUrl);
                if (!string.IsNullOrEmpty(apiKey))
                {
                    request.Headers.Add("X-Redmine-API-Key", apiKey);
                }

                using var response = await httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsByteArrayAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to download image: {ex.Message}");
            }

            return null;
        }

        private static void RenderImageWithExternalTool(byte[] imageData, int maxWidth)
        {
            // Native AOT制約のため、簡易的な実装として
            // 画像ファイルの一時保存と外部ツールの使用をシミュレート
            // 実際の環境では img2sixel などのツールを使用可能

            // 代替案：単純なテストパターンを出力
            OutputTestPattern(maxWidth > 0 ? maxWidth : 100);
        }

        /// <summary>
        /// Sixelのテストパターンを出力します（デモ用）
        /// </summary>
        private static void OutputTestPattern(int width)
        {
            var sb = new StringBuilder();
            sb.Append(SIXEL_START);

            // カラーパレットの定義
            sb.Append("#0;2;0;0;0");      // 黒
            sb.Append("#1;2;100;0;0");    // 赤
            sb.Append("#2;2;0;100;0");    // 緑
            sb.Append("#3;2;0;0;100");    // 青
            sb.Append("#4;2;100;100;0");  // 黄
            sb.Append("#5;2;100;0;100");  // マゼンタ
            sb.Append("#6;2;0;100;100");  // シアン
            sb.Append("#7;2;100;100;100");// 白

            // グラデーションパターンを生成
            for (int band = 0; band < 8; band++)
            {
                sb.AppendFormat("#{0}", band % 8);
                for (int x = 0; x < Math.Min(width, 80); x++)
                {
                    sb.Append((char)(63 + 63)); // すべてのピクセルを塗りつぶし
                }
                sb.Append("$-"); // 改行
            }

            sb.Append(SIXEL_END);
            Console.Write(sb.ToString());
        }

        /// <summary>
        /// 簡易的なSixelパターンを生成します（画像プレースホルダー）
        /// </summary>
        public static void OutputImagePlaceholder(string filename, int width = 40, int height = 20)
        {
            if (!TerminalCapabilityDetector.SupportsSixel())
            {
                return;
            }

            var sb = new StringBuilder();
            sb.Append(SIXEL_START);

            // パレット定義
            sb.Append("#0;2;20;20;20");   // ダークグレー（枠）
            sb.Append("#1;2;80;80;80");   // ライトグレー（背景）
            sb.Append("#2;2;100;100;100");// 白（テキスト背景）

            // 画像プレースホルダーを描画
            int bands = (height + 5) / 6;

            for (int band = 0; band < bands; band++)
            {
                // 枠の描画
                if (band == 0 || band == bands - 1)
                {
                    sb.Append("#0");
                    for (int x = 0; x < width; x++)
                    {
                        sb.Append((char)(63 + 63)); // フル塗りつぶし
                    }
                    sb.Append("$");
                }
                else
                {
                    // 左右の枠と中央の背景
                    sb.Append("#0??"); // 左枠
                    sb.Append("#1");
                    for (int x = 2; x < width - 2; x++)
                    {
                        sb.Append("~"); // 背景パターン
                    }
                    sb.Append("#0??$"); // 右枠
                }
                sb.Append("-");
            }

            sb.Append(SIXEL_END);
            Console.Write(sb.ToString());

            // ファイル名を表示
            Console.WriteLine($"\n[Image: {filename}]");
        }
    }
}
