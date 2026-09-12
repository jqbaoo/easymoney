namespace EasyMoney.Core
{
    /// <summary>账单类型。数值直接落库，不可更改。</summary>
    public enum TxType
    {
        Expense = 0,
        Income = 1,
        Transfer = 2
    }

    /// <summary>分类方向。数值直接落库，不可更改。</summary>
    public enum CategoryKind
    {
        Expense = 0,
        Income = 1
    }

    /// <summary>账户类型。数值直接落库，不可更改。</summary>
    public enum AccountType
    {
        Cash = 0,
        BankCard = 1,
        Alipay = 2,
        WeChat = 3,
        Other = 4
    }
}
