using EasyMoney.Core;
using NUnit.Framework;

namespace EasyMoney.Tests
{
    public class ModelTests
    {
        [Test]
        public void TxType_StableNumericValues()
        {
            // 这些数字会写进数据库，改动等于数据迁移，必须锁死
            Assert.AreEqual(0, (int)TxType.Expense);
            Assert.AreEqual(1, (int)TxType.Income);
            Assert.AreEqual(2, (int)TxType.Transfer);
        }

        [Test]
        public void CategoryKind_StableNumericValues()
        {
            Assert.AreEqual(0, (int)CategoryKind.Expense);
            Assert.AreEqual(1, (int)CategoryKind.Income);
        }

        [Test]
        public void AccountType_StableNumericValues()
        {
            Assert.AreEqual(0, (int)AccountType.Cash);
            Assert.AreEqual(1, (int)AccountType.BankCard);
            Assert.AreEqual(2, (int)AccountType.Alipay);
            Assert.AreEqual(3, (int)AccountType.WeChat);
            Assert.AreEqual(4, (int)AccountType.Other);
        }

        [Test]
        public void Transaction_DefaultsAreSafe()
        {
            Transaction oTx = new Transaction();
            Assert.AreEqual(0, oTx.Id);
            Assert.AreEqual(0L, oTx.AmountCents);
            Assert.AreEqual(string.Empty, oTx.Note);
            Assert.AreEqual(0, oTx.ToAccountId);
        }
    }
}
