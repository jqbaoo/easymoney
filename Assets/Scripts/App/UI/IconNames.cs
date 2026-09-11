namespace EasyMoney.App.UI
{
    /// <summary>
    /// 图标名常量。界面上所有图标都从这里引用，不要在各页面写字符串字面量。
    /// 每个名字对应 Assets/Resources/Icons/&lt;名字&gt;.png。
    /// </summary>
    public static class IconNames
    {
        /// <summary>
        /// 选中态图标的后缀。给 tab_list_on.png 就自动用作「账单」标签的选中态；
        /// 不给也没关系，代码会把普通图标染成主色。
        /// </summary>
        public const string SELECTED_SUFFIX = "_on";

        // ── 底部标签栏 ──────────────────────────────
        public const string TAB_RECORD = "tab_record";
        public const string TAB_LIST = "tab_list";
        public const string TAB_ACCOUNT = "tab_account";
        public const string TAB_REPORT = "tab_report";

        // ── 通用 ────────────────────────────────────
        public const string CHEVRON_LEFT = "chevron_left";
        public const string CHEVRON_RIGHT = "chevron_right";
        public const string ADD = "icon_add";
        public const string DELETE = "icon_delete";
        public const string EDIT = "icon_edit";

        // ── 分类图标 ────────────────────────────────
        // 约定：cat_<slug>，与 Database 里 Category.IconName 存的值一致。
        public const string CAT_FOOD = "cat_food";
        public const string CAT_SHOPPING = "cat_shopping";
        public const string CAT_TRANSPORT = "cat_transport";
        public const string CAT_HOUSING = "cat_housing";
        public const string CAT_ENTERTAINMENT = "cat_entertainment";
        public const string CAT_MEDICAL = "cat_medical";
        public const string CAT_EDUCATION = "cat_education";
        public const string CAT_COMMUNICATION = "cat_communication";
        public const string CAT_SOCIAL = "cat_social";
        public const string CAT_OTHER = "cat_other";

        public const string CAT_SALARY = "cat_salary";
        public const string CAT_BONUS = "cat_bonus";
        public const string CAT_PARTTIME = "cat_parttime";
        public const string CAT_INVESTMENT = "cat_investment";
        public const string CAT_REDPACKET = "cat_redpacket";

        /// <summary>
        /// 取分类的图标名。params 里的候选按顺序尝试，返回第一个在 Resources 里真实存在的；
        /// 都不存在则返回 null，调用方回退到文字或纯色圆点。
        /// </summary>
        public static string ForCategory(string sIconName, params string[] lFallbacks)
        {
            if (!string.IsNullOrEmpty(sIconName) && AssetProvider.Icon(sIconName) != null)
            {
                return sIconName;
            }

            if (lFallbacks != null)
            {
                foreach (string sCandidate in lFallbacks)
                {
                    if (!string.IsNullOrEmpty(sCandidate) && AssetProvider.Icon(sCandidate) != null)
                    {
                        return sCandidate;
                    }
                }
            }

            return null;
        }
    }
}
