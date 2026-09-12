namespace EasyMoney.Core
{
    /// <summary>
    /// 账户表单的校验结果。通过时顺便带上清洗过的名称与初始余额，
    /// 调用方直接拿去写库，不用自己再解析一遍输入框里的字符串。
    /// </summary>
    public sealed class AccountFormResult
    {
        public bool IsValid { get; private set; }

        public string ErrorMessage { get; private set; } = string.Empty;

        /// <summary>去掉首尾空格后的账户名。仅在校验通过时有意义。</summary>
        public string Name { get; private set; } = string.Empty;

        /// <summary>初始余额，单位「分」。留空按 0 算。仅在校验通过时有意义。</summary>
        public long InitialBalanceCents { get; private set; }

        public static AccountFormResult Ok(string sName, long iBalanceCents)
        {
            return new AccountFormResult
            {
                IsValid = true,
                Name = sName,
                InitialBalanceCents = iBalanceCents
            };
        }

        public static AccountFormResult Fail(string sMessage)
        {
            return new AccountFormResult
            {
                IsValid = false,
                ErrorMessage = sMessage
            };
        }
    }
}
