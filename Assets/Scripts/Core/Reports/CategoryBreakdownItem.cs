namespace EasyMoney.Core
{
    /// <summary>单个分类的汇总。</summary>
    public class CategoryBreakdownItem
    {
        public int CategoryId { get; set; }

        public string CategoryName { get; set; } = string.Empty;

        public Money Total { get; set; }

        public int TxCount { get; set; }

        /// <summary>占同类型总额的比例，取值 0~1。由 AssignRatios 填充。</summary>
        public decimal Ratio { get; set; }
    }
}
