namespace EasyMoney.Core
{
    public class Transaction
    {
        public int Id { get; set; }

        public TxType Type { get; set; } = TxType.Expense;

        /// <summary>金额，单位「分」，恒为正数；方向由 Type 决定。</summary>
        public long AmountCents { get; set; }

        /// <summary>支出/收入的账户；转账时为转出账户。</summary>
        public int AccountId { get; set; }

        /// <summary>转账的转入账户；非转账为 0。</summary>
        public int ToAccountId { get; set; }

        /// <summary>分类 Id；转账为 0。</summary>
        public int CategoryId { get; set; }

        public string Note { get; set; } = string.Empty;

        public long OccurredAtMs { get; set; }

        public long CreatedAtMs { get; set; }

        public long UpdatedAtMs { get; set; }

        public bool IsTransfer => Type == TxType.Transfer;
    }
}
