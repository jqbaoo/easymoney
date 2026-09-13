namespace EasyMoney.Core
{
    /// <summary>
    /// 字重与字体资源槽位的对应关系，以及字体文件缺失时的回退链。
    ///
    /// 为什么放在 Core：这些规则页面测不到。槽位名写错（比如 Medium 指到 bold 的文件）
    /// 不会报任何错，只会让字重静默错位，真机上肉眼才能发现。抽成纯函数就能在
    /// EditMode 里逐个打表钉住——和 StatementBuilder / AccountForm 把展示规则
    /// 从页面里抽出来是同一个理由。
    /// </summary>
    public static class FontSlots
    {
        /// <summary>Regular 槽位。沿用既有命名——只放一个字体文件时用的就是它。</summary>
        public const string SLOT_REGULAR = "main";

        /// <summary>Medium 槽位。</summary>
        public const string SLOT_MEDIUM = "main_medium";

        /// <summary>Bold 槽位。</summary>
        public const string SLOT_BOLD = "main_bold";

        // 回退链做成静态数组，省掉每次解析的分配。调用方只读，不要就地改。
        private static readonly FontWeight[] CHAIN_REGULAR = { FontWeight.Regular };
        private static readonly FontWeight[] CHAIN_MEDIUM = { FontWeight.Medium, FontWeight.Regular };
        private static readonly FontWeight[] CHAIN_BOLD = { FontWeight.Bold, FontWeight.Regular };

        /// <summary>
        /// 取字重对应的资源槽位名。槽位名即 Assets/Resources/Fonts/ 下的文件名，
        /// 由 AssetProvider 拼成 Resources.Load 的路径。
        /// </summary>
        public static string SlotOf(FontWeight eWeight)
        {
            switch (eWeight)
            {
                case FontWeight.Medium:
                    return SLOT_MEDIUM;
                case FontWeight.Bold:
                    return SLOT_BOLD;
                default:
                    return SLOT_REGULAR;
            }
        }

        /// <summary>
        /// 该字重的回退链，按优先级排列。
        ///
        /// Regular 自己就是兜底，链上只有它；Medium / Bold 找不到各自的字体文件时
        /// 退回 Regular——掉一档字重总比掉字好，调用方也不必自己判断缺没缺。
        /// 链尾恒为 Regular。
        /// </summary>
        public static FontWeight[] FallbackChain(FontWeight eWeight)
        {
            switch (eWeight)
            {
                case FontWeight.Medium:
                    return CHAIN_MEDIUM;
                case FontWeight.Bold:
                    return CHAIN_BOLD;
                default:
                    return CHAIN_REGULAR;
            }
        }
    }
}
