using UnityEngine;

namespace MieMieFrameTools.Archive
{
    /// <summary>
    /// 颜色相关静态工具
    /// </summary>
    public static class ColorTools
    {
        /// <summary>
        /// 将 Color 转换为十六进制字符串（如 "#FF8080"）
        /// </summary>
        public static string ToHex(Color color, bool includeHash = true)
        {
            int r = Mathf.RoundToInt(color.r * 255f);
            int g = Mathf.RoundToInt(color.g * 255f);
            int b = Mathf.RoundToInt(color.b * 255f);
            string format = includeHash ? "#{0:X2}{1:X2}{2:X2}" : "{0:X2}{1:X2}{2:X2}";
            return string.Format(format, r, g, b);
        }

        /// <summary>
        /// 从十六进制字符串解析 Color（自动补全 Alpha=1）
        /// </summary>
        /// <param name="hex">支持 "#RRGGBB" 或 "RRGGBB" 格式</param>
        public static Color ParseHex(string hex)
        {
            hex = hex.TrimStart('#');
            if (hex.Length < 6)
                return Color.white;

            bool ok = byte.TryParse(hex[..2], System.Globalization.NumberStyles.HexNumber, null, out byte r)
                   & byte.TryParse(hex[2..4], System.Globalization.NumberStyles.HexNumber, null, out byte g)
                   & byte.TryParse(hex[4..6], System.Globalization.NumberStyles.HexNumber, null, out byte b);

            return ok
                ? new Color(r / 255f, g / 255f, b / 255f, 1f)
                : Color.white;
        }

        /// <summary>
        /// 将 Color 转换为 [R, G, B, A] 归一化数组
        /// </summary>
        public static float[] ToArray(Color color)
        {
            return new[] { color.r, color.g, color.b, color.a };
        }

        /// <summary>
        /// 从归一化数组还原 Color
        /// </summary>
        public static Color FromArray(float[] arr)
        {
            if (arr == null || arr.Length < 3)
                return Color.white;
            return new Color(
                arr.Length > 0 ? arr[0] : 0f,
                arr.Length > 1 ? arr[1] : 0f,
                arr.Length > 2 ? arr[2] : 0f,
                arr.Length > 3 ? arr[3] : 1f
            );
        }

        /// <summary>
        /// 线性渐变混合 t=[0,1] 两端颜色
        /// </summary>
        public static Color Lerp(Color from, Color to, float t)
        {
            t = Mathf.Clamp01(t);
            return new Color(
                from.r + (to.r - from.r) * t,
                from.g + (to.g - from.g) * t,
                from.b + (to.b - from.b) * t,
                from.a + (to.a - from.a) * t
            );
        }

        /// <summary>
        /// 根据亮度判断颜色偏暗还是偏亮
        /// </summary>
        public static bool IsDark(Color color)
        {
            return color.r * 0.299f + color.g * 0.587f + color.b * 0.114f < 0.5f;
        }

        /// <summary>
        /// 获取颜色的灰度值（0~1）
        /// </summary>
        public static float Grayscale(Color color)
        {
            return color.r * 0.299f + color.g * 0.587f + color.b * 0.114f;
        }

        /// <summary>
        /// 随机生成一个不透明颜色
        /// </summary>
        public static Color Random()
        {
            return new Color(UnityEngine.Random.value, UnityEngine.Random.value,
                UnityEngine.Random.value, 1f);
        }

        /// <summary>
        /// 随机生成一个带 Alpha 的颜色
        /// </summary>
        public static Color RandomWithAlpha()
        {
            return new Color(UnityEngine.Random.value, UnityEngine.Random.value,
                UnityEngine.Random.value, UnityEngine.Random.value);
        }

        /// <summary>
        /// 将 Color 转为 RGBA 整数（如 0xFF8040FF）
        /// </summary>
        public static int ToInt(Color color)
        {
            int r = Mathf.RoundToInt(color.r * 255f) << 24;
            int g = Mathf.RoundToInt(color.g * 255f) << 16;
            int b = Mathf.RoundToInt(color.b * 255f) << 8;
            int a = Mathf.RoundToInt(color.a * 255f);
            return r | g | b | a;
        }

        /// <summary>
        /// 从 RGBA 整数还原 Color
        /// </summary>
        public static Color FromInt(int value)
        {
            return new Color(
                ((value >> 24) & 0xFF) / 255f,
                ((value >> 16) & 0xFF) / 255f,
                ((value >> 8) & 0xFF) / 255f,
                (value & 0xFF) / 255f
            );
        }
    }
}
