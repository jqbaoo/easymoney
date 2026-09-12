namespace EasyMoney.Core
{
    public class Account
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public AccountType Type { get; set; } = AccountType.Cash;

        /// <summary>初始余额，单位「分」。</summary>
        public long InitialBalanceCents { get; set; }

        public bool IsArchived { get; set; }

        public int SortOrder { get; set; }

        public long CreatedAtMs { get; set; }
    }
}
