namespace EasyMoney.Core
{
    /// <summary>
    /// 校验结果。失败时一定带一句能直接显示给用户的中文原因，
    /// 界面层不需要再翻译错误码。
    /// </summary>
    public sealed class ValidationResult
    {
        private ValidationResult(bool bIsValid, string sErrorMessage)
        {
            IsValid = bIsValid;
            ErrorMessage = sErrorMessage;
        }

        public bool IsValid { get; }

        public string ErrorMessage { get; }

        public static ValidationResult Ok()
        {
            return new ValidationResult(true, string.Empty);
        }

        public static ValidationResult Fail(string sMessage)
        {
            return new ValidationResult(false, sMessage);
        }
    }
}
