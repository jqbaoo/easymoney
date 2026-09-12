using System.Collections.Generic;
using EasyMoney.Core;
using NUnit.Framework;

namespace EasyMoney.Tests
{
    /// <summary>
    /// 账户表单规则测试。盯住三件事：名称必须非空（且去掉首尾空格）、
    /// 初始余额留空按 0 算、填了就必须是合法金额。
    /// </summary>
    public class AccountFormTests
    {
        // ── 类型标签 ────────────────────────────────

        [Test]
        public void TypeLabel_CoversEveryAccountType()
        {
            Assert.AreEqual("现金", AccountForm.TypeLabel(AccountType.Cash));
            Assert.AreEqual("银行卡", AccountForm.TypeLabel(AccountType.BankCard));
            Assert.AreEqual("支付宝", AccountForm.TypeLabel(AccountType.Alipay));
            Assert.AreEqual("微信", AccountForm.TypeLabel(AccountType.WeChat));
            Assert.AreEqual("其他", AccountForm.TypeLabel(AccountType.Other));
        }

        [Test]
        public void TypeLabel_UnknownValue_FallsBackToOther()
        {
            Assert.AreEqual("其他", AccountForm.TypeLabel((AccountType)999));
        }

        [Test]
        public void TypeCycle_ContainsEveryAccountTypeExactlyOnce()
        {
            // 漏一个类型用户就切不到它，重复一个则要多点一次才跳过
            List<AccountType> lSorted = new List<AccountType>(AccountForm.TYPE_CYCLE);
            lSorted.Sort();

            Assert.AreEqual(
                new List<AccountType>
                {
                    AccountType.Cash, AccountType.BankCard, AccountType.Alipay,
                    AccountType.WeChat, AccountType.Other
                },
                lSorted);
        }

        // ── 名称校验 ────────────────────────────────

        [Test]
        public void Validate_TrimsName()
        {
            AccountFormResult oResult = AccountForm.Validate("  现金  ", "0");

            Assert.IsTrue(oResult.IsValid);
            Assert.AreEqual("现金", oResult.Name);
        }

        [Test]
        public void Validate_EmptyName_Fails()
        {
            AccountFormResult oResult = AccountForm.Validate(string.Empty, "0");

            Assert.IsFalse(oResult.IsValid);
            Assert.AreEqual(AccountForm.NAME_REQUIRED_MESSAGE, oResult.ErrorMessage);
        }

        [Test]
        public void Validate_WhitespaceName_Fails()
        {
            AccountFormResult oResult = AccountForm.Validate("   ", "0");

            Assert.IsFalse(oResult.IsValid);
            Assert.AreEqual(AccountForm.NAME_REQUIRED_MESSAGE, oResult.ErrorMessage);
        }

        [Test]
        public void Validate_NullName_Fails()
        {
            AccountFormResult oResult = AccountForm.Validate(null, "0");

            Assert.IsFalse(oResult.IsValid);
            Assert.AreEqual(AccountForm.NAME_REQUIRED_MESSAGE, oResult.ErrorMessage);
        }

        [Test]
        public void Validate_NameIsCheckedBeforeBalance()
        {
            // 两个都错时先报名称：用户按从上到下的顺序填，先修上面那个
            AccountFormResult oResult = AccountForm.Validate("  ", "abc");

            Assert.AreEqual(AccountForm.NAME_REQUIRED_MESSAGE, oResult.ErrorMessage);
        }

        // ── 初始余额校验 ────────────────────────────

        [Test]
        public void Validate_EmptyBalance_MeansZero()
        {
            // 光断言 Cents == 0 是不够的：校验失败返回的 Fail 里那个字段也是 0，
            // 两件事就分不开了。必须连着 IsValid 一起断言
            _assertEmptyBalanceIsZero(string.Empty);
            _assertEmptyBalanceIsZero(null);
            _assertEmptyBalanceIsZero("   ");
        }

        private static void _assertEmptyBalanceIsZero(string sBalanceText)
        {
            AccountFormResult oResult = AccountForm.Validate("现金", sBalanceText);

            Assert.IsTrue(oResult.IsValid, $"余额「{sBalanceText}」应视为 0 而不是错误");
            Assert.AreEqual(0L, oResult.InitialBalanceCents);
        }

        [Test]
        public void Validate_ParsesYuanIntoCents()
        {
            AccountFormResult oResult = AccountForm.Validate("现金", "500");

            Assert.IsTrue(oResult.IsValid);
            Assert.AreEqual(50000L, oResult.InitialBalanceCents);

            Assert.AreEqual(1234L, AccountForm.Validate("现金", "12.34").InitialBalanceCents);
        }

        [Test]
        public void Validate_NegativeBalance_IsAllowed()
        {
            // 信用卡欠款：初始余额为负是正常场景，不能拦
            AccountFormResult oResult = AccountForm.Validate("信用卡", "-500");

            Assert.IsTrue(oResult.IsValid);
            Assert.AreEqual(-50000L, oResult.InitialBalanceCents);
        }

        [Test]
        public void Validate_UnparsableBalance_Fails()
        {
            AccountFormResult oResult = AccountForm.Validate("现金", "abc");

            Assert.IsFalse(oResult.IsValid);
            Assert.AreEqual(AccountForm.BALANCE_INVALID_MESSAGE, oResult.ErrorMessage);
        }

        [Test]
        public void Validate_OverPreciseBalance_Fails()
        {
            // 12.345 元凑不出整分，属于误输入
            AccountFormResult oResult = AccountForm.Validate("现金", "12.345");

            Assert.IsFalse(oResult.IsValid);
            Assert.AreEqual(AccountForm.BALANCE_INVALID_MESSAGE, oResult.ErrorMessage);
        }
    }
}
