namespace EasyMoney.Core
{
    /// <summary>
    /// 账户表单的规则：类型标签、类型循环顺序、名称与初始余额的校验。
    ///
    /// 放 Core 是因为这些与界面无关，而且是纯函数——「账户名不能为空」「初始余额
    /// 留空按 0 算」「可以填负数表示信用卡欠款」这类约定留在页面里就没法被测到，
    /// 而 EditMode 测试根本跑不到页面。
    /// </summary>
    public static class AccountForm
    {
        public const string NAME_REQUIRED_MESSAGE = "请填写账户名称";

        public const string BALANCE_INVALID_MESSAGE = "初始余额格式不正确";

        public const string OTHER_TYPE_LABEL = "其他";

        /// <summary>
        /// 切换类型时用的循环顺序。与枚举数值无关，纯粹是给用户点着顺手。
        /// </summary>
        public static readonly AccountType[] TYPE_CYCLE =
        {
            AccountType.Cash, AccountType.BankCard, AccountType.Alipay,
            AccountType.WeChat, AccountType.Other
        };

        public static string TypeLabel(AccountType oType)
        {
            switch (oType)
            {
                case AccountType.Cash: return "现金";
                case AccountType.BankCard: return "银行卡";
                case AccountType.Alipay: return "支付宝";
                case AccountType.WeChat: return "微信";
                default: return OTHER_TYPE_LABEL;
            }
        }

        /// <summary>
        /// 循环到下一个类型。不认识的取值一律回到第一项，保证界面上永远点得动——
        /// 枚举将来加了新值而这里忘了改时，最多是顺序不对，不会卡住。
        /// </summary>
        public static AccountType NextType(AccountType oType)
        {
            for (int i = 0; i < TYPE_CYCLE.Length; i++)
            {
                if (TYPE_CYCLE[i] == oType)
                {
                    return TYPE_CYCLE[(i + 1) % TYPE_CYCLE.Length];
                }
            }

            return TYPE_CYCLE[0];
        }

        /// <summary>
        /// 校验名称与初始余额。余额留空按 0 算（新建账户时最省事），
        /// 填了就必须是合法金额；负数允许，那是信用卡欠款这类情况。
        /// </summary>
        public static AccountFormResult Validate(string sName, string sBalanceText)
        {
            string sTrimmedName = (sName ?? string.Empty).Trim();
            if (sTrimmedName.Length == 0)
            {
                return AccountFormResult.Fail(NAME_REQUIRED_MESSAGE);
            }

            // 留空不是错误，是「这个账户从 0 开始」
            if (string.IsNullOrWhiteSpace(sBalanceText))
            {
                return AccountFormResult.Ok(sTrimmedName, 0L);
            }

            if (!MoneyParser.TryParseYuan(sBalanceText, out Money oBalance))
            {
                return AccountFormResult.Fail(BALANCE_INVALID_MESSAGE);
            }

            return AccountFormResult.Ok(sTrimmedName, oBalance.Cents);
        }
    }
}
