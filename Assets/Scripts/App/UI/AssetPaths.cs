namespace EasyMoney.App.UI
{
    /// <summary>
    /// 资源路径与命名约定。全部相对 Assets/Resources/，且不带扩展名
    /// （Resources.Load 就是这么用的）。
    ///
    /// 目录结构：
    ///   Assets/Resources/Icons/     图标
    ///   Assets/Resources/Sprites/   九宫格底图
    ///   Assets/Resources/Fonts/     字体
    ///   Assets/Resources/theme.json 主题配色（可选）
    /// </summary>
    public static class AssetPaths
    {
        public const string ICON_DIR = "Icons";

        public const string SPRITE_DIR = "Sprites";

        public const string FONT_DIR = "Fonts";

        // ── 具名资源 ────────────────────────────────

        /// <summary>
        /// 主字体的 Regular 档。放进 Fonts/main.otf 即自动生效。
        ///
        /// Medium / Bold 两档的槽位名（main_medium / main_bold）不在这里，
        /// 在 <c>Core/Typography/FontSlots.cs</c>——字重到槽位的映射是要被测试打表的纯逻辑，
        /// 放在 Core 才测得到。这里只留 Regular 这个「什么都不传时用哪个」的基准名。
        /// </summary>
        public const string MAIN_FONT = "main";

        /// <summary>卡片底图（九宫格）。放进 Sprites/card.png 即自动生效。</summary>
        public const string CARD_SPRITE = "card";

        /// <summary>按钮底图（九宫格）。可选，缺省时复用卡片底图。</summary>
        public const string BUTTON_SPRITE = "button";

        /// <summary>主题配色文件（Resources/theme.json）。</summary>
        public const string THEME_FILE = "theme";
    }
}
