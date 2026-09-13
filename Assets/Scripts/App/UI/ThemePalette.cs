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

        /// <summary>
        /// 卡片投影的颜色。暖色底上用纯黑会发灰，所以浅色主题给的是偏棕的暖黑，
        /// 靠 alpha 控制轻重——饱和度高的阴影会显得脏。
        /// </summary>
        public Color Shadow { get; private set; }

        /// <summary>
        /// 分类色板，按顺序取给环形图的扇区用（<see cref="Theme.ChartColor"/> 负责回绕）。
        ///
        /// 刻意**不开放 theme.json 覆盖**：它是一组颜色而不是一个，JsonUtility 解析
        /// 数组还要多一层长度校验，而设计改配色的入口本来就有 theme.json 那 11 个键，
        /// 收益不抵成本。要改就改这里。
        ///
        /// 相邻两块的颜色拉开了冷暖——分类按金额降序排，相邻两项的金额最接近、
        /// 在环上也挨着，同色系排在一起会糊成一片。
        /// </summary>
        public Color[] ChartColors { get; private set; }

        public static ThemePalette Light()
        {
            return new ThemePalette
            {
                // 暖色纸感。底色是燕麦米白，卡片是暖白——两者只差十几个色阶，
                // 靠的是卡片那一层投影把边界交代清楚，光靠底色深浅是分不出来的。
                //
                // 支出红和收入绿也跟着暖化了：原来的纯红 #E03E3E、冷绿 #2EA05C
                // 放在米白底上会「燥」，跟整页的暖调打架。
                Background = _hex("#F2ECE2"),
                Surface = _hex("#FFFCF6"),
                Primary = _hex("#A9714B"),
                Expense = _hex("#C0523C"),
                Income = _hex("#4F8A5B"),
                TextPrimary = _hex("#2B2620"),
                TextWeak = _hex("#8A8177"),
                Divider = _hex("#E7DFD2"),
                BarTrack = _hex("#E7DFD2"),
                Scrim = _hex("#00000073"),
                Shadow = _hex("#46311C1A"),
                // 砖红起手，与支出红同源，整圈跟暖色纸感的底子是一路的。
                // 冷暖交替排：相邻两块在环上挨着，同色系排一起会糊成一片
                ChartColors = new[]
                {
                    _hex("#C0523C"),
                    _hex("#4F8A5B"),
                    _hex("#D98E4A"),
                    _hex("#5B8C9E"),
                    _hex("#A9714B"),
                    _hex("#7A6A9B"),
                    _hex("#B4677A"),
                    _hex("#8A8177")
                }
            };
        }

        public static ThemePalette Dark()
        {
            return new ThemePalette
            {
                // 深色下卡片靠「比底亮一档」浮起来，投影几乎看不见，
                // 所以这里的值只是让对比更强一点，不是主要手段。
                Background = _hex("#121316"),
                Surface = _hex("#1C1E22"),
                Primary = _hex("#D19A6E"),
                Expense = _hex("#E0705C"),
                Income = _hex("#5FB47C"),
                TextPrimary = _hex("#ECEDEF"),
                TextWeak = _hex("#9A9CA1"),
                Divider = _hex("#2A2D32"),
                BarTrack = _hex("#2A2D32"),
                Scrim = _hex("#000000A6"),
                Shadow = _hex("#0000003D"),
                // 深色下饱和度要抬一档，否则整圈会显得脏
                ChartColors = new[]
                {
                    _hex("#E0705C"),
                    _hex("#5FB47C"),
                    _hex("#E8A961"),
                    _hex("#6FA8BC"),
                    _hex("#D19A6E"),
                    _hex("#9A8ABC"),
                    _hex("#D0859A"),
                    _hex("#9A9CA1")
                }
            };
        }

        /// <summary>
        /// 从 JSON 覆盖默认浅色主题。字段缺省或格式不合法时，该项退回默认值——
        /// 这样 theme.json 里只写想改的那几个颜色也能工作。
        ///
        /// ChartColors 不在这张表里，它跟着 Light() 走（见该属性的说明）。
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
            oResult.Shadow = _override(oData.shadow, oResult.Shadow);

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
            public string shadow;
        }
    }
}
