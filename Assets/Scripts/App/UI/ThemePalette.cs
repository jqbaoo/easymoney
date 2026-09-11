using System;
using UnityEngine;

namespace EasyMoney.App.UI
{
    /// <summary>
    /// 主题配色。换品牌色、加浅色/深色模式，都只动这里，
    /// 页面代码一律通过 Theme.XXX 取色，不直接写 Color。
    /// </summary>
    public sealed class ThemePalette
    {
        public Color Background { get; private set; }

        public Color Surface { get; private set; }

        public Color Primary { get; private set; }

        public Color Expense { get; private set; }

        public Color Income { get; private set; }

        public Color TextPrimary { get; private set; }

        public Color TextWeak { get; private set; }

        public Color Divider { get; private set; }

        public Color BarTrack { get; private set; }

        public Color Scrim { get; private set; }

        public static ThemePalette Light()
        {
            return new ThemePalette
            {
                Background = _hex("#F2F3F5"),
                Surface = _hex("#FFFFFF"),
                Primary = _hex("#307AE8"),
                Expense = _hex("#E03E3E"),
                Income = _hex("#2EA05C"),
                TextPrimary = _hex("#1F2126"),
                TextWeak = _hex("#8A8F99"),
                Divider = _hex("#E6E8EB"),
                BarTrack = _hex("#E6E8EB"),
                Scrim = _hex("#00000073")
            };
        }

        public static ThemePalette Dark()
        {
            return new ThemePalette
            {
                Background = _hex("#121316"),
                Surface = _hex("#1C1E22"),
                Primary = _hex("#4A90E2"),
                Expense = _hex("#E05C5C"),
                Income = _hex("#3FBF74"),
                TextPrimary = _hex("#ECEDEF"),
                TextWeak = _hex("#8A8F99"),
                Divider = _hex("#2A2D32"),
                BarTrack = _hex("#2A2D32"),
                Scrim = _hex("#000000A6")
            };
        }

        /// <summary>
        /// 从 JSON 覆盖默认浅色主题。字段缺省或格式不合法时，该项退回默认值——
        /// 这样 theme.json 里只写想改的那几个颜色也能工作。
        /// </summary>
        public static ThemePalette FromJson(string sJson)
        {
            ThemePalette oResult = Light();

            if (string.IsNullOrWhiteSpace(sJson))
            {
                return oResult;
            }

            ThemePaletteJson oData;
            try
            {
                oData = JsonUtility.FromJson<ThemePaletteJson>(sJson);
            }
            catch (Exception oException)
            {
                Debug.LogWarning($"[Theme] theme.json 解析失败，改用默认配色：{oException.Message}");
                return oResult;
            }

            if (oData == null)
            {
                return oResult;
            }

            oResult.Background = _override(oData.background, oResult.Background);
            oResult.Surface = _override(oData.surface, oResult.Surface);
            oResult.Primary = _override(oData.primary, oResult.Primary);
            oResult.Expense = _override(oData.expense, oResult.Expense);
            oResult.Income = _override(oData.income, oResult.Income);
            oResult.TextPrimary = _override(oData.textPrimary, oResult.TextPrimary);
            oResult.TextWeak = _override(oData.textWeak, oResult.TextWeak);
            oResult.Divider = _override(oData.divider, oResult.Divider);
            oResult.BarTrack = _override(oData.barTrack, oResult.BarTrack);
            oResult.Scrim = _override(oData.scrim, oResult.Scrim);

            return oResult;
        }

        private static Color _override(string sHex, Color oFallback)
        {
            if (string.IsNullOrWhiteSpace(sHex))
            {
                return oFallback;
            }

            return ColorUtility.TryParseHtmlString(sHex, out Color oColor) ? oColor : oFallback;
        }

        private static Color _hex(string sHex)
        {
            return ColorUtility.TryParseHtmlString(sHex, out Color oColor) ? oColor : Color.magenta;
        }

        /// <summary>
        /// theme.json 的结构。字段名小写，与 JSON 一一对应。
        /// 必须 public —— JsonUtility 在 IL2CPP 下对非公开类型支持不稳。
        /// </summary>
        [Serializable]
        public class ThemePaletteJson
        {
            public string background;
            public string surface;
            public string primary;
            public string expense;
            public string income;
            public string textPrimary;
            public string textWeak;
            public string divider;
            public string barTrack;
            public string scrim;
        }
    }
}
