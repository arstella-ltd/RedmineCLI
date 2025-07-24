using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace RedmineCLI.Utils
{
    /// <summary>
    /// Sixelプロトコルエンコーダー
    /// </summary>
    // 参考: https://github.com/mattn/go-sixel
    public class SixelEncoder
    {
        // Sixel制御シーケンス
        private const string DCS = "\x1bP";  // Device Control String
        private const string ST = "\x1b\\";  // String Terminator

        // Sixelの基本定数
        private const int SIXEL_OFFSET = 63;  // '?'
        private const int SIXEL_HEIGHT = 6;   // 1文字で6ピクセルの高さ

        private readonly int _maxColors;
        private readonly bool _useDithering;

        public SixelEncoder(int maxColors = 256, bool useDithering = false)
        {
            _maxColors = Math.Min(Math.Max(2, maxColors), 256);
            _useDithering = useDithering;
        }

        /// <summary>
        /// ピクセルデータをSixel形式にエンコード
        /// </summary>
        public string Encode(byte[] pixelData, int width, int height, int channels = 3)
        {
            if (pixelData.Length < width * height * channels)
            {
                throw new ArgumentException("Insufficient pixel data");
            }

            var sb = new StringBuilder();

            // Sixelヘッダー
            // P<Ps1>;<Ps2>;<Ps3>q
            // Ps1: アスペクト比 (0=1:1)
            // Ps2: 背景選択 (0=透明, 1=不透明)
            // Ps3: グリッド拡張 (0=なし)
            sb.Append(DCS);
            sb.Append("0;0;0q");

            // 画像データを量子化
            var (quantizedData, palette) = QuantizeImage(pixelData, width, height, channels);

            // カラーパレットを定義
            DefinePalette(sb, palette);

            // 画像データをエンコード
            EncodePixels(sb, quantizedData, width, height, palette.Count);

            // Sixel終端
            sb.Append(ST);

            return sb.ToString();
        }

        /// <summary>
        /// 画像を量子化してパレットを生成
        /// </summary>
        private (byte[] quantizedData, List<Color> palette) QuantizeImage(
            byte[] pixelData, int width, int height, int channels)
        {
            var pixelCount = width * height;
            var colorCounts = new Dictionary<Color, int>();

            // 全ピクセルの色と頻度を収集
            for (int i = 0; i < pixelCount; i++)
            {
                int idx = i * channels;
                byte r = pixelData[idx];
                byte g = channels > 1 ? pixelData[idx + 1] : r;
                byte b = channels > 2 ? pixelData[idx + 2] : r;
                var color = new Color(r, g, b);

                if (colorCounts.ContainsKey(color))
                    colorCounts[color]++;
                else
                    colorCounts[color] = 1;
            }

            // 頻度の高い色を優先してパレットを作成
            var palette = colorCounts
                .OrderByDescending(kv => kv.Value)
                .Take(_maxColors)
                .Select(kv => kv.Key)
                .ToList();

            if (palette.Count == 0)
            {
                palette.Add(new Color(0, 0, 0)); // 黒を追加
            }

            // 各ピクセルを最も近い色にマップ
            var quantizedData = new byte[pixelCount];
            for (int i = 0; i < pixelCount; i++)
            {
                int idx = i * channels;
                byte r = pixelData[idx];
                byte g = channels > 1 ? pixelData[idx + 1] : r;
                byte b = channels > 2 ? pixelData[idx + 2] : r;

                quantizedData[i] = (byte)FindClosestColor(palette, r, g, b);
            }

            return (quantizedData, palette);
        }

        /// <summary>
        /// 最も近い色のインデックスを検索
        /// </summary>
        private int FindClosestColor(List<Color> palette, byte r, byte g, byte b)
        {
            int minDist = int.MaxValue;
            int bestIdx = 0;

            for (int i = 0; i < palette.Count; i++)
            {
                var color = palette[i];
                int dr = r - color.R;
                int dg = g - color.G;
                int db = b - color.B;
                int dist = dr * dr + dg * dg + db * db;

                if (dist < minDist)
                {
                    minDist = dist;
                    bestIdx = i;
                }
            }

            return bestIdx;
        }

        /// <summary>
        /// カラーパレットを定義
        /// </summary>
        private void DefinePalette(StringBuilder sb, List<Color> palette)
        {
            for (int i = 0; i < palette.Count; i++)
            {
                var color = palette[i];
                // #Pc;Pu;Px;Py;Pz
                // Pc: カラー番号
                // Pu: カラーモデル (2=RGB)
                // Px, Py, Pz: RGB値 (0-100)
                sb.AppendFormat("#{0};2;{1};{2};{3}",
                    i,
                    color.R * 100 / 255,
                    color.G * 100 / 255,
                    color.B * 100 / 255);
            }
        }

        /// <summary>
        /// ピクセルデータをSixel文字にエンコード
        /// </summary>
        private void EncodePixels(StringBuilder sb, byte[] quantizedData,
            int width, int height, int colorCount)
        {
            // 6行ごとに処理（Sixelは6ピクセルの高さ）
            for (int y = 0; y < height; y += SIXEL_HEIGHT)
            {
                var rowHeight = Math.Min(SIXEL_HEIGHT, height - y);
                bool bandHasData = false;

                // 各色ごとに行を処理
                for (int color = 0; color < colorCount; color++)
                {
                    var rowData = new StringBuilder();
                    bool colorHasPixels = false;
                    int repeatCount = 0;
                    char lastChar = '?';

                    // 横方向にスキャン
                    for (int x = 0; x < width; x++)
                    {
                        byte sixel = 0;

                        // 6ピクセルの縦列を1つのSixel文字に変換
                        for (int dy = 0; dy < rowHeight; dy++)
                        {
                            int pixelIdx = (y + dy) * width + x;
                            if (pixelIdx < quantizedData.Length &&
                                quantizedData[pixelIdx] == color)
                            {
                                sixel |= (byte)(1 << dy);
                            }
                        }

                        char sixelChar = (char)(sixel + SIXEL_OFFSET);

                        // 空のピクセルでも文字を出力（位置を保持するため）
                        if (sixel > 0)
                        {
                            colorHasPixels = true;
                        }

                        // RLE圧縮
                        if (sixelChar == lastChar)
                        {
                            repeatCount++;
                        }
                        else
                        {
                            if (repeatCount > 3)
                            {
                                rowData.AppendFormat("!{0}{1}", repeatCount, lastChar);
                            }
                            else
                            {
                                for (int i = 0; i < repeatCount; i++)
                                    rowData.Append(lastChar);
                            }
                            lastChar = sixelChar;
                            repeatCount = 1;
                        }
                    }

                    // 最後の文字を処理
                    if (repeatCount > 3)
                    {
                        rowData.AppendFormat("!{0}{1}", repeatCount, lastChar);
                    }
                    else
                    {
                        for (int i = 0; i < repeatCount; i++)
                            rowData.Append(lastChar);
                    }

                    // この色のピクセルがある場合のみ出力
                    if (colorHasPixels)
                    {
                        // 色選択
                        sb.AppendFormat("#{0}", color);
                        sb.Append(rowData);
                        sb.Append('$'); // 行頭に戻る
                        bandHasData = true;
                    }
                }

                // 次の行へ（改行）- データがある場合のみ
                if (bandHasData)
                {
                    sb.Append('-');
                }
            }
        }

        /// <summary>
        /// 色を表す内部クラス
        /// </summary>
        private class Color : IEquatable<Color>
        {
            public byte R { get; }
            public byte G { get; }
            public byte B { get; }

            public Color(byte r, byte g, byte b)
            {
                R = r;
                G = g;
                B = b;
            }

            public bool Equals(Color? other)
            {
                if (other is null) return false;
                return R == other.R && G == other.G && B == other.B;
            }

            public override bool Equals(object? obj) => Equals(obj as Color);

            public override int GetHashCode() => HashCode.Combine(R, G, B);
        }
    }
}
