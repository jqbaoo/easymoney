using System;
using EasyMoney.Core;
using UnityEngine;

namespace EasyMoney.App.UI
{
    /// <summary>
    /// 全局视觉规范。设计基准分辨率 750 x 1334（iPhone 6/7/8，即 375x667pt @2x）。
    /// 所有尺寸单位都是设计稿像素——改版式只改这里，不要在页面里硬编码数字。
    ///
    /// 颜色不在这里定义，而是转发给 <see cref="ThemePalette"/>：
    /// 换品牌色改 ThemePalette，或在 Assets/Resources/theme.json 里覆盖，
    /// 不用碰这个文件，也不用碰任何页面。
    /// </summary>
    public static class Theme
    {
        // ── 设计基准 ────────────────────────────────
        public const float REF_WIDTH = 750f;
        public const float REF_HEIGHT = 1334f;

        // ── 骨架 ────────────────────────────────────
        public const float HEADER_HEIGHT = 88f;

        /// <summary>标签栏高度。比纯文字时代（98）高一些，给图标让位。</summary>
        public const float TABBAR_HEIGHT = 116f;

        /// <summary>标签栏图标size。图标缺失时这部分空间由文字独占。</summary>
        public const float TAB_ICON_SIZE = 48f;

        /// <summary>标签栏文字行高。</summary>
        public const float TAB_LABEL_HEIGHT = 32f;

        /// <summary>
        /// 分类图标尺寸。账单列表行、报表行、记账页分类行、分类选择弹窗四处共用，
        /// 免得四处各写一个数，改的时候漏掉一处、大小对不上。
        /// </summary>
        public const float CATEGORY_ICON_SIZE = 40f;

        /// <summary>
        /// 分类图标与相邻文字之间的间距。靠所在行的 spacing 留出来——
        /// 不额外插占位节点，那样每多一个图标位就多一层节点，布局也难对齐。
        /// </summary>
        public const float CATEGORY_ICON_GAP = 12f;

        /// <summary>
        /// 环形图图例色点的直径。
        ///
        /// 取分类图标的一半：它是「这一行对应环上哪一块」的标记，跟图标抢视觉重量的话
        /// 扫一列下来反而分不出主次。环上没有文字，颜色与分类的对应全靠它。
        /// </summary>
        public const float CHART_DOT_SIZE = 20f;

        // ── 间距 ────────────────────────────────────
        public const float PAGE_PADDING = 32f;
        public const float PAGE_GAP = 24f;
        public const float CARD_PADDING = 24f;
        public const float CARD_GAP = 20f;
        public const float CARD_RADIUS = 16f;

        /// <summary>
        /// 卡片投影向下偏移多少（设计稿像素，6 ≈ 3pt）。
        /// 行间距 CARD_GAP 只有 20，偏移再大一点相邻两行的投影就会挨上，
        /// 列表会显得糊。
        /// </summary>
        public const float SHADOW_OFFSET = 6f;

        // ── 行高 ────────────────────────────────────
        public const float ROW_HEIGHT = 112f;
        public const float ROW_HEIGHT_LARGE = 140f;
        public const float DATE_HEADER_HEIGHT = 64f;
        public const float DIVIDER_HEIGHT = 2f;

        // ── 控件 ────────────────────────────────────
        public const float SEGMENT_HEIGHT = 80f;
        public const float INPUT_HEIGHT = 120f;
        public const float BUTTON_HEIGHT = 112f;
        public const float HERO_HEIGHT = 220f;
        public const float SUMMARY_HEIGHT = 180f;
        public const float BAR_TRACK_HEIGHT = 20f;

        // ── 字号（设计稿像素）────────────────────────
        public const int FONT_HERO = 60;
        public const int FONT_TITLE = 40;
        public const int FONT_BODY = 32;
        public const int FONT_CAPTION = 26;
        public const int FONT_TINY = 22;

        // ── 字重（按语义分配，页面里不要直接写 FontWeight.XXX）──
        // 和字号一样，改这里就能全站生效。

        /// <summary>金额数字。全 App 最重要的信息，Medium 让它压得住又不至于笨重。</summary>
        public const FontWeight WEIGHT_AMOUNT = FontWeight.Medium;

        /// <summary>标题：页面标题、月份条、弹窗标题。</summary>
        public const FontWeight WEIGHT_TITLE = FontWeight.Medium;

        /// <summary>正文与次要说明。也是 CreateText 的默认字重，所以页面通常不用显式传。</summary>
        public const FontWeight WEIGHT_BODY = FontWeight.Regular;

        /// <summary>最强强调：主按钮、标签栏选中态。用得越少越有力。</summary>
        public const FontWeight WEIGHT_STRONG = FontWeight.Bold;

        // ── 颜色（转发给当前配色方案）─────────────────

        private static ThemePalette s_Palette;

        /// <summary>配色变化时触发。界面的颜色是在构建时写死的，所以订阅方
        /// 收到通知后要重建 UI 才能看到新配色（AppRoot 已代为处理）。</summary>
        public static event Action PaletteChanged;

        /// <summary>当前配色。首次访问时从 Resources/theme.json 载入，没有就用内置浅色。</summary>
        public static ThemePalette Palette
        {
            get
            {
                if (s_Palette == null)
                {
                    s_Palette = ThemePalette.FromJson(AssetProvider.Text(AssetPaths.THEME_FILE));
                }

                return s_Palette;
            }
        }

        /// <summary>切换配色（例如浅色 / 深色）。</summary>
        public static void Apply(ThemePalette oPalette)
        {
            if (oPalette == null || ReferenceEquals(oPalette, s_Palette))
            {
                return;
            }

            s_Palette = oPalette;
            PaletteChanged?.Invoke();
        }

        public static Color BACKGROUND => Palette.Background;
        public static Color SURFACE => Palette.Surface;
        public static Color PRIMARY => Palette.Primary;
        public static Color EXPENSE => Palette.Expense;
        public static Color INCOME => Palette.Income;
        public static Color TEXT => Palette.TextPrimary;
        public static Color TEXT_WEAK => Palette.TextWeak;
        public static Color DIVIDER => Palette.Divider;
        public static Color BAR_TRACK => Palette.BarTrack;
        public static Color SCRIM => Palette.Scrim;
        public static Color SHADOW => Palette.Shadow;

        // 这两个与主题无关，固定值。
        public static readonly Color WHITE = Color.white;
        public static readonly Color TRANSPARENT = new Color(0f, 0f, 0f, 0f);

        /// <summary>
        /// 按槽位取分类色，给环形图的扇区用。
        ///
        /// 下标回绕而不是越界：色板长度是固定的（现在 8 个），分类数却由用户自己定，
        /// 十几个分类很常见。回绕的代价是两个分类撞色——比整个界面崩掉强得多。
        /// </summary>
        public static Color ChartColor(int iIndex)
        {
            Color[] lColors = Palette.ChartColors;

            // 配色方案没配色板也不能返回个 null 出去，那样环形图会整块变透明
            if (lColors == null || lColors.Length == 0)
            {
                return PRIMARY;
            }

            int iWrapped = iIndex % lColors.Length;
            if (iWrapped < 0)
            {
                iWrapped += lColors.Length;
            }

            return lColors[iWrapped];
        }
    }
}
