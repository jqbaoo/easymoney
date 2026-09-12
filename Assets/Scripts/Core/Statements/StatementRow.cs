namespace EasyMoney.Core
{
    /// <summary>
    /// 账单列表里的一行。界面上那三段文字（主标题 / 副标题 / 金额）怎么拼，
    /// 规则放这里而不是页面里——「备注为空时显示分类名」「转账不带正负号」
    /// 都是业务约定，做成纯函数才能被测试逐条盯住。
    /// </summary>
    public sealed class StatementRow
    {
        /// <summary>原始账单。界面要用它的 Id 删除、用 Type 决定金额颜色。</summary>
        public Transaction Transaction { get; set; }

        /// <summary>主标题：有备注显示备注，没有就显示分类名（转账显示「转账」）。</summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>副标题：普通账单是「分类 · 账户」，转账是「转出 → 转入」。</summary>
        public string Subtitle { get; set; } = string.Empty;

        /// <summary>金额文案：收入带 +，支出带 -，转账不带符号。</summary>
        public string AmountText { get; set; } = string.Empty;

        public TxType Type => Transaction?.Type ?? TxType.Expense;
    }
}
