using System;

namespace RedmineCLI.Utils
{
    /// <summary>
    /// ターミナルの機能を検出するユーティリティクラス
    /// </summary>
    public static class TerminalCapabilityDetector
    {
        /// <summary>
        /// Sixelサポートの検出は削除されました。
        /// --imageオプションで表示を制御します。
        /// </summary>
        public static bool SupportsSixel()
        {
            // Always return false - image display is controlled by --image option
            return false;
        }

        /// <summary>
        /// ターミナルの幅を取得します
        /// </summary>
        public static int GetTerminalWidth()
        {
            try
            {
                var width = Console.WindowWidth;
                return width > 0 ? width : 80; // 0の場合はデフォルト幅を返す
            }
            catch
            {
                return 80; // デフォルト幅
            }
        }
    }
}
