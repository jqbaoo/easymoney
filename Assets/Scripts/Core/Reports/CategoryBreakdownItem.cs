namespace EasyMoney.Core
{
    /// <summary>单个分类的汇总。</summary>
    public class CategoryBreakdownItem
    {
        public int CategoryId { get; set; }

        public string CategoryName { get; set; } = string.Empty;

        /// <summary>分类图标名（数据库里存的值，如 cat_food）。分类被删或没配图标时是空字符串。</summary>
        public string IconName { get; set; } = string.Empty;

        public Money Total { get; set; }

        public int TxCount { get; set; }

        /// <summary>占同类型总额的比例，取值 0~1。由 AssignRatios 填充。</summary>
        public decimal Ratio { get; set; }
    }
}
