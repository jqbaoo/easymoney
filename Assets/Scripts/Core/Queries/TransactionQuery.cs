namespace EasyMoney.Core
{
    /// <summary>账单筛选条件。Task 10 补全所有字段。</summary>
    public class TransactionQuery
    {
        public int Limit { get; set; } = 50;

        public int Offset { get; set; }
    }
}
