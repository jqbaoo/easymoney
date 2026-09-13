namespace EasyMoney.Core
{
    /// <summary>报表的展示形态。</summary>
    public enum ReportViewMode
    {
        /// <summary>按分类列出占比，每行一根条形。</summary>
        Bar = 0,

        /// <summary>按分类占比画一张环形图。</summary>
        Donut = 1
    }

    /// <summary>
    /// 视图清单与它们的名字。下拉列表的选项直接来自 <see cref="ALL"/>，
    /// 以后新增一种视图只要「这里加一项 + 写一个 IReportView 实现」两处，
    /// 页面不用动。
    ///
    /// 放 Core 的理由同 ReportForm：这些是给用户看的字，属于展示规则。
    /// 留在页面里就只能靠肉眼验，而 EditMode 根本跑不到页面。
    /// </summary>
    public static class ReportViews
    {
        public const string BAR_TITLE = "条形图";

        public const string DONUT_TITLE = "环形图";

        /// <summary>下拉列表里的选项，顺序即显示顺序。</summary>
        public static readonly ReportViewMode[] ALL = { ReportViewMode.Bar, ReportViewMode.Donut };

        /// <summary>首次进报表页时的视图。</summary>
        public static readonly ReportViewMode Default = ReportViewMode.Bar;

        /// <summary>
        /// 视图名。认不出的值按默认视图处理——枚举底层是 int，
        /// 从存档或外部传进来一个越界值是可能的，那时宁可显示成条形图，
        /// 也别让下拉按钮上是一片空白。
        /// </summary>
        public static string Title(ReportViewMode oMode)
        {
            return oMode == ReportViewMode.Donut ? DONUT_TITLE : BAR_TITLE;
        }

        /// <summary>模式在 <see cref="ALL"/> 里的下标；找不到返回 0（下拉组件不接受负下标）。</summary>
        public static int IndexOf(ReportViewMode oMode)
        {
            for (int i = 0; i < ALL.Length; i++)
            {
                if (ALL[i] == oMode)
                {
                    return i;
                }
            }

            return 0;
        }

        /// <summary>
        /// 默认视图的下标。下拉组件按下标工作，两者必须对得上——
        /// 所以不写死 0，而是从 <see cref="ALL"/> 里找。
        /// </summary>
        public static int DefaultIndex => IndexOf(Default);
    }
}
