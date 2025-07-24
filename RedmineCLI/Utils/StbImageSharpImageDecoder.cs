using System;
using System.IO;
using StbImageSharp;

namespace RedmineCLI.Utils
{
    /// <summary>
    /// StbImageSharpを使用した画像デコーダー
    /// </summary>
    public static class StbImageSharpImageDecoder
    {
        /// <summary>
        /// 画像データをデコードしてRGBピクセルデータを取得
        /// </summary>
        public static (byte[] pixelData, int width, int height)? DecodeImage(byte[] imageData)
        {
            try
            {
                using var stream = new MemoryStream(imageData);
                
                // StbImageSharpを使用して画像をデコード
                ImageResult image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlue);
                
                if (image == null)
                    return null;
                
                // StbImageSharpはRGB形式で返すので、そのまま使用可能
                return (image.Data, image.Width, image.Height);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}