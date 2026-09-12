using EasyMoney.Core;
using EasyMoney.Data;
using NUnit.Framework;

namespace EasyMoney.Tests
{
    /// <summary>
    /// 账单仓储的字段往返测试。这些用例是「先实现后补测试」——
    /// Task 7 为了让余额聚合有真实数据可算，已经先写好了仓储实现。
    /// 此处把每个字段的往返、枚举映射和边界值都钉死。
    /// </summary>
    public class TransactionRepositoryTests
    {
        private EasyMoneyDb m_Db;
        private ITransactionRepository m_Repo;

        [SetUp]
        public void SetUp()
        {
            m_Db = TestDb.Create();
            m_Repo = new SqliteTransactionRepository(m_Db);
        }

        [TearDown]
        public void TearDown()
        {
            m_Db.Dispose();
        }

        private static Transaction _make(
            TxType oType, decimal dYuan, string sNote = "", int iCategoryId = 0,
            long iOccurredAtMs = 1_700_000_000_000L)
        {
            return new Transaction
            {
                Type = oType,
                AmountCents = Money.FromYuan(dYuan).Cents,
                AccountId = 1,
                CategoryId = iCategoryId,
                Note = sNote,
                OccurredAtMs = iOccurredAtMs,
                CreatedAtMs = iOccurredAtMs,
                UpdatedAtMs = iOccurredAtMs
            };
        }

        [Test]
        public void Insert_RoundTripsEveryField()
        {
            Transaction oTx = _make(TxType.Expense, 12.34m, "午饭", iCategoryId: 7);
            oTx.ToAccountId = 3;

            int iId = m_Repo.Insert(oTx);
            Assert.Greater(iId, 0);

            Transaction oLoaded = m_Repo.GetById(iId);
            Assert.AreEqual(TxType.Expense, oLoaded.Type);
            Assert.AreEqual(1234, oLoaded.AmountCents);
            Assert.AreEqual(1, oLoaded.AccountId);
            Assert.AreEqual(3, oLoaded.ToAccountId);
            Assert.AreEqual(7, oLoaded.CategoryId);
            Assert.AreEqual("午饭", oLoaded.Note);
            Assert.AreEqual(1_700_000_000_000L, oLoaded.OccurredAtMs);
            Assert.IsFalse(oLoaded.IsTransfer);
        }

        [Test]
        public void Insert_PreservesEnumTypes()
        {
            int iIncome = m_Repo.Insert(_make(TxType.Income, 1m));
            int iTransfer = m_Repo.Insert(_make(TxType.Transfer, 1m));

            Assert.AreEqual(TxType.Income, m_Repo.GetById(iIncome).Type);
            Assert.AreEqual(TxType.Transfer, m_Repo.GetById(iTransfer).Type);
            Assert.IsTrue(m_Repo.GetById(iTransfer).IsTransfer);
        }

        [Test]
        public void Insert_HandlesEmptyNote()
        {
            int iId = m_Repo.Insert(_make(TxType.Expense, 1m));
            Assert.AreEqual(string.Empty, m_Repo.GetById(iId).Note);
        }

        [Test]
        public void Insert_HandlesLargeAmountWithoutOverflow()
        {
            Transaction oTx = _make(TxType.Expense, 0m);
            oTx.AmountCents = 9_999_999_999L;   // 约 1 亿元
            int iId = m_Repo.Insert(oTx);
            Assert.AreEqual(9_999_999_999L, m_Repo.GetById(iId).AmountCents);
        }

        [Test]
        public void GetById_ReturnsNullWhenMissing()
        {
            Assert.IsNull(m_Repo.GetById(999999));
        }

        [Test]
        public void Update_ChangesAllMutableFields()
        {
            int iId = m_Repo.Insert(_make(TxType.Expense, 10m, "旧备注"));

            Transaction oTx = m_Repo.GetById(iId);
            oTx.AmountCents = 2500;
            oTx.Note = "新备注";
            oTx.CategoryId = 5;
            oTx.OccurredAtMs = 1_600_000_000_000L;
            oTx.UpdatedAtMs = 1_700_000_000_999L;
            m_Repo.Update(oTx);

            Transaction oReloaded = m_Repo.GetById(iId);
            Assert.AreEqual(2500, oReloaded.AmountCents);
            Assert.AreEqual("新备注", oReloaded.Note);
            Assert.AreEqual(5, oReloaded.CategoryId);
            Assert.AreEqual(1_600_000_000_000L, oReloaded.OccurredAtMs);
            Assert.AreEqual(1_700_000_000_999L, oReloaded.UpdatedAtMs);
        }

        [Test]
        public void Delete_RemovesRow()
        {
            int iId = m_Repo.Insert(_make(TxType.Expense, 1m));
            Assert.IsTrue(m_Repo.Delete(iId));
            Assert.IsNull(m_Repo.GetById(iId));
            Assert.AreEqual(0, m_Repo.CountAll());
        }

        [Test]
        public void Delete_ReturnsFalseForMissingId()
        {
            Assert.IsFalse(m_Repo.Delete(999999));
        }

        [Test]
        public void Insert_DoesNotDisturbOtherRows()
        {
            int iFirst = m_Repo.Insert(_make(TxType.Expense, 1m, "第一笔"));
            m_Repo.Insert(_make(TxType.Income, 2m, "第二笔"));

            Assert.AreEqual(2, m_Repo.CountAll());
            Assert.AreEqual("第一笔", m_Repo.GetById(iFirst).Note);
        }

        [Test]
        public void Note_SupportsChineseAndEmoji()
        {
            int iId = m_Repo.Insert(_make(TxType.Expense, 1m, "和朋友吃饭🍜"));
            Assert.AreEqual("和朋友吃饭🍜", m_Repo.GetById(iId).Note);
        }
    }
}
