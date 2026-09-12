namespace EasyMoney.Core
{
    /// <summary>账户 + 实时余额的组合，用于账户列表展示。</summary>
    public sealed class AccountBalance
    {
        public AccountBalance(Account oAccount, Money oBalance)
        {
            Account = oAccount;
            Balance = oBalance;
        }

        public Account Account { get; }

        public Money Balance { get; }
    }
}
