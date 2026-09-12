using System.Globalization;

namespace EasyMoney.Core
{
    /// <summary>
    /// 把用户输入的字符串解析成 Money。只接受纯数字（可带负号与最多两位小数），
    /// 不接受千分位、科学计数法、货币符号——记账场景下这些几乎总是误输入。
    /// </summary>
    public static class MoneyParser
    {
        public static bool TryParseYuan(string sText, out Money oResult)
        {
            oResult = Money.Zero;

            if (string.IsNullOrWhiteSpace(sText))
            {
                return false;
            }

            string sTrimmed = sText.Trim();

            if (!decimal.TryParse(
                    sTrimmed,
                    NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                    CultureInfo.InvariantCulture,
                    out decimal dYuan))
            {
                return false;
            }

            if (dYuan != decimal.Round(dYuan, 2))
            {
                return false;
            }

            oResult = Money.FromYuan(dYuan);
            return true;
        }
    }
}
