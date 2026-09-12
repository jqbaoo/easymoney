namespace EasyMoney.Core
{
    /// <summary>
    /// 报表页的展示规则：构成标题、每行的占比文案、条形填充的宽度。
    ///
    /// 放 Core 的理由与 StatementBuilder / AccountForm 一样——「占比保留一位小数」
    /// 「条形宽度只在 0~1 之间」这类约定留在页面里就只能靠肉眼验，
    /// 而 EditMode 测试根本跑不到页面。
    /// </summary>
    public static class ReportForm
    {
        public const string EXPENSE_TITLE = "支出构成";

        public const string INCOME_TITLE = "收入构成";

        /// <summary>当月没有任何可统计的账单时的提示。</summary>
        public const string EMPTY_HINT = "这个月还没有记录";

        /// <summary>
        /// 构成标题。转账不参与收支统计，页面也不会切到它，真传进来时按支出处理——
        /// 总比标题空着强。
        /// </summary>
        public static string BreakdownTitle(TxType oType)
        {
            return oType == TxType.Income ? INCOME_TITLE : EXPENSE_TITLE;
        }

        /// <summary>占比文案，形如「35.50  (15.1%)」。</summary>
        public static string BreakdownValueText(Money oTotal, decimal dRatio)
        {
            // 百分比用 decimal 算，不用 float：float 的 0.1 是 0.100000001，
            // 乘 100 以后落在四舍五入边界上的值会飘（54.25 → 54.2 还是 54.3 说不准）
            return $"{oTotal}  ({dRatio * 100m:0.0}%)";
        }

        /// <summary>
        /// 条形填充占轨道的宽度比例，钳在 0~1。
        /// 比值本身是 decimal 除法算出来的，正常不会越界；钳一道是为了
        /// 万一数据坏掉时条形不会画到轨道外面去——锚点越界不会报错，只会画歪。
        /// </summary>
        public static decimal BarWidthRatio(decimal dRatio)
        {
            if (dRatio < 0m)
            {
                return 0m;
            }

            return dRatio > 1m ? 1m : dRatio;
        }
    }
}
