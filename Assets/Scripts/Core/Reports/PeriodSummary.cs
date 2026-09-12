namespace EasyMoney.Core
{
    /// <summary>某段时间的收支汇总。转账不计入任何一项。</summary>
    public class PeriodSummary
    {
        public Money Income { get; set; }

        public Money Expense { get; set; }

        /// <summary>结余 = 收入 - 支出，可为负。</summary>
        public Money Net => Income - Expense;

        /// <summary>参与统计的账单笔数，不含转账。</summary>
        public int TxCount { get; set; }
    }
}
