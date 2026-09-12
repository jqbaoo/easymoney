using System.Collections.Generic;
using EasyMoney.Core;
using NUnit.Framework;

namespace EasyMoney.Tests
{
    /// <summary>
    /// 校验规则是记账的第一道闸门，每条规则都要有正反两个用例盯着，
    /// 否则「规则写反了」也会全绿。
    /// </summary>
    public class TransactionValidatorTests
    {
        private List<Account> m_Accounts;
        private List<Category> m_Categories;

        [SetUp]
        public void SetUp()
        {
            m_Accounts = new List<Account>
            {
                new Account { Id = 1, Name = "现金" },
                new Account { Id = 2, Name = "支付宝" }
            };

            m_Categories = new List<Category>
            {
                new Category { Id = 10, Name = "餐饮", Kind = CategoryKind.Expense },
                new Category { Id = 20, Name = "工资", Kind = CategoryKind.Income }
            };
        }

        private static Transaction _expense()
        {
            return new Transaction
            {
                Type = TxType.Expense,
                AmountCents = 1000,
                AccountId = 1,
                CategoryId = 10,
                OccurredAtMs = 1_700_000_000_000L
            };
        }

        [Test]
        public void ValidExpense_Passes()
        {
            ValidationResult oResult = TransactionValidator.Validate(_expense(), m_Accounts, m_Categories);
            Assert.IsTrue(oResult.IsValid, oResult.ErrorMessage);
            Assert.AreEqual(string.Empty, oResult.ErrorMessage);
        }

        [Test]
        public void ZeroAmount_IsRejected()
        {
            Transaction oTx = _expense();
            oTx.AmountCents = 0;
            Assert.IsFalse(TransactionValidator.Validate(oTx, m_Accounts, m_Categories).IsValid);
        }

        [Test]
        public void NegativeAmount_IsRejected()
        {
            Transaction oTx = _expense();
            oTx.AmountCents = -100;
            Assert.IsFalse(TransactionValidator.Validate(oTx, m_Accounts, m_Categories).IsValid);
        }

        [Test]
        public void AmountOverCap_IsRejected()
        {
            Transaction oTx = _expense();
            oTx.AmountCents = TransactionValidator.MaxAmountCents + 1;
            Assert.IsFalse(TransactionValidator.Validate(oTx, m_Accounts, m_Categories).IsValid);
        }

        [Test]
        public void AmountAtCap_IsAccepted()
        {
            Transaction oTx = _expense();
            oTx.AmountCents = TransactionValidator.MaxAmountCents;
            Assert.IsTrue(TransactionValidator.Validate(oTx, m_Accounts, m_Categories).IsValid);
        }

        [Test]
        public void UnknownAccount_IsRejected()
        {
            Transaction oTx = _expense();
            oTx.AccountId = 999;
            Assert.IsFalse(TransactionValidator.Validate(oTx, m_Accounts, m_Categories).IsValid);
        }

        [Test]
        public void ZeroAccount_IsRejected()
        {
            Transaction oTx = _expense();
            oTx.AccountId = 0;
            Assert.IsFalse(TransactionValidator.Validate(oTx, m_Accounts, m_Categories).IsValid);
        }

        [Test]
        public void ExpenseWithoutCategory_IsRejected()
        {
            Transaction oTx = _expense();
            oTx.CategoryId = 0;
            Assert.IsFalse(TransactionValidator.Validate(oTx, m_Accounts, m_Categories).IsValid);
        }

        [Test]
        public void ExpenseWithIncomeCategory_IsRejected()
        {
            Transaction oTx = _expense();
            oTx.CategoryId = 20;   // 工资是收入分类
            Assert.IsFalse(TransactionValidator.Validate(oTx, m_Accounts, m_Categories).IsValid);
        }

        [Test]
        public void IncomeWithExpenseCategory_IsRejected()
        {
            Transaction oTx = _expense();
            oTx.Type = TxType.Income;
            oTx.CategoryId = 10;   // 餐饮是支出分类
            Assert.IsFalse(TransactionValidator.Validate(oTx, m_Accounts, m_Categories).IsValid);
        }

        [Test]
        public void ValidIncome_Passes()
        {
            Transaction oTx = _expense();
            oTx.Type = TxType.Income;
            oTx.CategoryId = 20;
            Assert.IsTrue(TransactionValidator.Validate(oTx, m_Accounts, m_Categories).IsValid);
        }

        [Test]
        public void Transfer_WithoutTargetAccount_IsRejected()
        {
            Transaction oTx = _expense();
            oTx.Type = TxType.Transfer;
            oTx.ToAccountId = 0;
            oTx.CategoryId = 0;
            Assert.IsFalse(TransactionValidator.Validate(oTx, m_Accounts, m_Categories).IsValid);
        }

        [Test]
        public void Transfer_ToSameAccount_IsRejected()
        {
            Transaction oTx = _expense();
            oTx.Type = TxType.Transfer;
            oTx.ToAccountId = oTx.AccountId;
            oTx.CategoryId = 0;
            Assert.IsFalse(TransactionValidator.Validate(oTx, m_Accounts, m_Categories).IsValid);
        }

        [Test]
        public void Transfer_ToUnknownAccount_IsRejected()
        {
            Transaction oTx = _expense();
            oTx.Type = TxType.Transfer;
            oTx.ToAccountId = 999;
            oTx.CategoryId = 0;
            Assert.IsFalse(TransactionValidator.Validate(oTx, m_Accounts, m_Categories).IsValid);
        }

        [Test]
        public void ValidTransfer_PassesWithoutCategory()
        {
            Transaction oTx = _expense();
            oTx.Type = TxType.Transfer;
            oTx.ToAccountId = 2;
            oTx.CategoryId = 0;
            Assert.IsTrue(TransactionValidator.Validate(oTx, m_Accounts, m_Categories).IsValid);
        }

        [Test]
        public void MissingOccurredTime_IsRejected()
        {
            Transaction oTx = _expense();
            oTx.OccurredAtMs = 0;
            Assert.IsFalse(TransactionValidator.Validate(oTx, m_Accounts, m_Categories).IsValid);
        }

        [Test]
        public void Failure_AlwaysCarriesNonEmptyMessage()
        {
            Transaction oTx = _expense();
            oTx.AmountCents = 0;

            ValidationResult oResult = TransactionValidator.Validate(oTx, m_Accounts, m_Categories);
            Assert.IsFalse(oResult.IsValid);
            Assert.IsNotEmpty(oResult.ErrorMessage);
        }
    }
}
