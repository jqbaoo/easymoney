using UnityEngine;

namespace EasyMoney.App.UI
{
    /// <summary>
    /// 字体解析，三级回退：
    ///   1. Assets/Resources/Fonts/main.ttf —— 美术/设计给了字体就用它，保证跨机型一致
    ///   2. 系统里的中文字体 —— 按平台逐个尝试，不必往包里塞字体文件
    ///   3. Unity 内置字体 —— 最后兜底（可能是方块字，但至少不崩）
    /// </summary>
    public static class FontProvider
    {
        private const int SAMPLE_SIZE = 32;

        private static readonly string[] FONT_CANDIDATES =
        {
            "Microsoft YaHei",
            "微软雅黑",
            "Noto Sans CJK SC",
            "Source Han Sans CN",
            "Droid Sans Fallback",
            "PingFang SC",
            "Heiti SC",
            "SimHei",
            "SimSun",
            "Arial Unicode MS"
        };

        private static Font s_Cached;

        public static Font Resolve()
        {
            if (s_Cached != null)
            {
                return s_Cached;
            }

            // 1. 项目自带字体优先
            Font oCustom = AssetProvider.Font(AssetPaths.MAIN_FONT);
            if (oCustom != null)
            {
                s_Cached = oCustom;
                return s_Cached;
            }

            // 2. 系统字体
            foreach (string sName in FONT_CANDIDATES)
            {
                Font oFont = Font.CreateDynamicFontFromOSFont(sName, SAMPLE_SIZE);
                if (oFont != null && oFont.dynamic)
                {
                    s_Cached = oFont;
                    return s_Cached;
                }
            }

            // 3. 兜底：Unity 内置字体。2022 起叫 LegacyRuntime.ttf（旧版是 Arial.ttf）。
            s_Cached = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return s_Cached;
        }
    }
}
