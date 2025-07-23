using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace RedmineCLI.Utils
{
    /// <summary>
    /// ターミナルの機能を検出するユーティリティクラス
    /// </summary>
    public static class TerminalCapabilityDetector
    {
        private static bool? _supportsSixel;

        /// <summary>
        /// ターミナルがSixelプロトコルをサポートしているかを検出します
        /// </summary>
        public static bool SupportsSixel()
        {
            if (_supportsSixel.HasValue)
            {
                return _supportsSixel.Value;
            }

            _supportsSixel = DetectSixelSupport();
            return _supportsSixel.Value;
        }

        private static bool DetectSixelSupport()
        {
            // 環境変数でSixelサポートが明示的に設定されている場合
            var sixelEnv = Environment.GetEnvironmentVariable("SIXEL_SUPPORT");
            if (sixelEnv != null)
            {
                return sixelEnv.Equals("1") || sixelEnv.Equals("true", StringComparison.OrdinalIgnoreCase);
            }

            // CIやパイプ出力の場合はSixelを無効化
            if (!Console.IsInputRedirected && !Console.IsOutputRedirected)
            {
                var term = Environment.GetEnvironmentVariable("TERM");
                var termProgram = Environment.GetEnvironmentVariable("TERM_PROGRAM");

                // 既知のSixel対応ターミナル
                if (term != null)
                {
                    if (term.Contains("sixel", StringComparison.OrdinalIgnoreCase) ||
                        term.Contains("mlterm", StringComparison.OrdinalIgnoreCase) ||
                        term.Contains("xterm-256color", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }

                // Windows Terminal (v1.22以降でSixelサポート)
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    var wtSession = Environment.GetEnvironmentVariable("WT_SESSION");
                    if (!string.IsNullOrEmpty(wtSession))
                    {
                        return CheckWindowsTerminalVersion();
                    }
                }

                // iTerm2
                if (termProgram?.Equals("iTerm.app", StringComparison.OrdinalIgnoreCase) == true)
                {
                    return true;
                }

                // WezTerm
                if (termProgram?.Contains("WezTerm", StringComparison.OrdinalIgnoreCase) == true)
                {
                    return true;
                }

                // Kitty
                if (term?.Contains("kitty", StringComparison.OrdinalIgnoreCase) == true)
                {
                    return false; // KittyはSixelではなく独自プロトコルを使用
                }
            }

            return false;
        }

        private static bool CheckWindowsTerminalVersion()
        {
            try
            {
                // Windows Terminalのバージョン確認（簡易的な方法）
                // v1.22以降でSixelサポート
                var wtProfilePath = Environment.GetEnvironmentVariable("WT_PROFILE_ID");
                return !string.IsNullOrEmpty(wtProfilePath);
            }
            catch
            {
                return false;
            }
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
