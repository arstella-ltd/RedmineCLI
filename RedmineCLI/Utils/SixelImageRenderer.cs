using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text;

namespace RedmineCLI.Utils
{
    /// <summary>
    /// Sixelプロトコルを使用して画像をレンダリングするユーティリティクラス
    /// </summary>
    public static class SixelImageRenderer
    {

        /// <summary>
        /// 実際の画像をSixel形式で出力します（内蔵エンコーダーを使用）
        /// </summary>
        public static bool RenderActualImage(string contentUrl, HttpClient httpClient, string? apiKey, string filename, int maxWidth = 400)
        {
            // Sixel support check is removed - --image option controls display

            try
            {
                // 画像をダウンロード
                using var request = new HttpRequestMessage(HttpMethod.Get, contentUrl);
                if (!string.IsNullOrEmpty(apiKey))
                {
                    request.Headers.Add("X-Redmine-API-Key", apiKey);
                }

                var response = httpClient.Send(request);
                if (!response.IsSuccessStatusCode)
                {
                    return false;
                }

                // 画像データを取得
                var imageData = response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();

                // StbImageSharpで画像をデコード
                var decoded = StbImageSharpImageDecoder.DecodeImage(imageData);

                if (decoded.HasValue)
                {
                    var (pixelData, width, height) = decoded.Value;

                    // アスペクト比を保ちながらリサイズ
                    if (width > maxWidth)
                    {
                        var scale = (double)maxWidth / width;
                        var newWidth = maxWidth;
                        var newHeight = (int)(height * scale);
                        var resized = ResizeImage(pixelData, width, height, newWidth, newHeight);
                        RenderSixelImage(resized.pixelData, resized.width, resized.height);
                    }
                    else
                    {
                        RenderSixelImage(pixelData, width, height);
                    }
                    Console.WriteLine($"[Image: {filename}]");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to render actual image: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// ピクセルデータをSixel形式で出力
        /// </summary>
        private static void RenderSixelImage(byte[] pixelData, int width, int height)
        {
            var encoder = new SixelEncoder(maxColors: 256); // 256色に制限
            var sixelData = encoder.Encode(pixelData, width, height, 3);
            Console.Write(sixelData);
        }

        /// <summary>
        /// 画像を簡易的にリサイズ（最近傍補間）
        /// </summary>
        private static (byte[] pixelData, int width, int height) ResizeImage(
            byte[] sourcePixels, int sourceWidth, int sourceHeight, int targetWidth, int targetHeight)
        {
            var targetPixels = new byte[targetWidth * targetHeight * 3];

            for (int y = 0; y < targetHeight; y++)
            {
                for (int x = 0; x < targetWidth; x++)
                {
                    // 最近傍のピクセルを取得
                    int srcX = x * sourceWidth / targetWidth;
                    int srcY = y * sourceHeight / targetHeight;

                    int srcIdx = (srcY * sourceWidth + srcX) * 3;
                    int dstIdx = (y * targetWidth + x) * 3;

                    targetPixels[dstIdx + 0] = sourcePixels[srcIdx + 0];
                    targetPixels[dstIdx + 1] = sourcePixels[srcIdx + 1];
                    targetPixels[dstIdx + 2] = sourcePixels[srcIdx + 2];
                }
            }

            return (targetPixels, targetWidth, targetHeight);
        }
    }
}
