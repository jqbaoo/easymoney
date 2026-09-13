using System.Collections.Generic;
using EasyMoney.Core;
using UnityEngine;

namespace EasyMoney.App.UI
{
    /// <summary>
    /// 字体解析，按字重分层。
    ///
    /// 每档字重先试自己的字体文件（Assets/Resources/Fonts/），
    /// Medium / Bold 缺失时按 Core 的 <see cref="FontSlots.FallbackChain"/> 退回 Regular；
    /// Regular 再缺失才落到系统字体，最后是 Unity 内置字体。
    /// 掉一档字重总比掉字好，掉字体总比崩好。
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

        // 按字重分别缓存。同一档字重的解析结果在整个进程内复用。
        private static readonly Dictionary<FontWeight, Font> s_Cache =
            new Dictionary<FontWeight, Font>();

        /// <summary>取 Regular 字重的字体。等价于 <c>Resolve(FontWeight.Regular)</c>。</summary>
        public static Font Resolve()
        {
            return Resolve(FontWeight.Regular);
        }

        /// <summary>取指定字重的字体。</summary>
        public static Font Resolve(FontWeight eWeight)
        {
            if (s_Cache.TryGetValue(eWeight, out Font oCached) && oCached != null)
            {
                return oCached;
            }

            Font oFont = _resolveByChain(eWeight);
            s_Cache[eWeight] = oFont;
            return oFont;
        }

        /// <summary>
        /// 清掉字体缓存，下次 Resolve 时重新解析。
        ///
        /// 连 AssetProvider 的缓存一起清：那边会把「文件不存在」的 null 也缓存下来，
        /// 只清这里的话，之后新放进 Fonts/ 的字体依然拿不到。
        /// </summary>
        public static void Reset()
        {
            s_Cache.Clear();
            AssetProvider.ClearCache();
        }

        /// <summary>沿回退链逐档试自带字体，全都落空才去要系统字体。</summary>
        private static Font _resolveByChain(FontWeight eWeight)
        {
            foreach (FontWeight eCandidate in FontSlots.FallbackChain(eWeight))
            {
                Font oFont = AssetProvider.Font(FontSlots.SlotOf(eCandidate));
                if (oFont != null)
                {
                    return oFont;
                }
            }

            return _resolveSystemFont();
        }

        /// <summary>自带字体一个都没放时的兜底：系统字体 → Unity 内置字体。</summary>
        private static Font _resolveSystemFont()
        {
            foreach (string sName in FONT_CANDIDATES)
            {
                Font oFont = Font.CreateDynamicFontFromOSFont(sName, SAMPLE_SIZE);
                if (oFont != null && oFont.dynamic)
                {
                    return oFont;
                }
            }

            // 2022 起叫 LegacyRuntime.ttf（旧版是 Arial.ttf）。
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
    }
}
