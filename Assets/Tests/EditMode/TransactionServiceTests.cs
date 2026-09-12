using EasyMoney.Core;
using EasyMoney.Data;
using NUnit.Framework;

namespace EasyMoney.Tests
{
    /// <summary>
    /// 记账服务的编排测试：校验拦不拦得住、时间戳谁来写、转账是不是真的只落一行。
    /// 余额断言走的是真实的聚合查询，能顺带证明转账对两个账户的作用方向都对。
    /// </summary>
    public class TransactionServiceTests
    {
        private const long NOW_MS = 1_800_000_000_000L;
        private const long OCCURRED_MS = 1_700_000_000_000L;

        private EasyMoneyDb m_Db;
        private IAccountRepository m_AccountRepo;
        private ICategoryRepository m_CategoryRepo;
        private ITransactionRepository m_TxRepo;
        private TransactionService m_Service;
        private int m_CashId;
        private int m_AlipayId;
        private int m_FoodCategoryId;

        [SetUp]
        public void SetUp()
        {
            m_Db = TestDb.Create();
            m_AccountRepo = new SqliteAccountRepository(m_Db);
            m_CategoryRepo = new SqliteCategoryRepository(m_Db);
            m_TxRepo = new SqliteTransactionRepository(m_Db);
            m_Service = new TransactionService(m_TxRepo, m_AccountRepo, m_CategoryRepo);

            m_CashId = m_AccountRepo.Insert(new Account { Name = "现金", CreatedAtMs = NOW_MS });
            m_AlipayId = m_AccountRepo.Insert(new Account { Name = "支付宝", CreatedAtMs = NOW_MS });
            m_FoodCategoryId = m_CategoryRepo.GetByKind(CategoryKind.Expense)[0].Id;
        }

        [TearDown]
        public void TearDown()
        {
            m_Db.Dispose();
        }

        private Transaction _newExpense(decimal dYuan)
        {
            return new Transaction
            {
                Type = TxType.Expense,
                AmountCents = Money.FromYuan(dYuan).Cents,
                AccountId = m_CashId,
                CategoryId = m_FoodCategoryId,
                Note = "午饭",
                OccurredAtMs = OCCURRED_MS
            };
        }

        [Test]
        public void Save_NewTransaction_StampsBothTimestamps()
        {
            Transaction oTx = _newExpense(30m);
            ValidationResult oResult = m_Service.Save(oTx, NOW_MS);

            Assert.IsTrue(oResult.IsValid, oResult.ErrorMessage);
            Assert.Greater(oTx.Id, 0);
            Assert.AreEqual(NOW_MS, oTx.CreatedAtMs);
            Assert.AreEqual(NOW_MS, oTx.UpdatedAtMs);
            Assert.AreEqual(OCCURRED_MS, oTx.OccurredAtMs, "账单时间不应被覆盖");
        }

        [Test]
        public void Save_InvalidTransaction_DoesNotWriteToDatabase()
        {
            Transaction oTx = _newExpense(0m);
            ValidationResult oResult = m_Service.Save(oTx, NOW_MS);

            Assert.IsFalse(oResult.IsValid);
            Assert.AreEqual(0, oTx.Id);
            Assert.AreEqual(0, m_TxRepo.CountAll());
        }

        [Test]
        public void Save_ExistingTransaction_KeepsCreatedAtAndBumpsUpdatedAt()
        {
            Transaction oTx = _newExpense(30m);
            m_Service.Save(oTx, NOW_MS);

            oTx.Note = "改成晚饭";
            const long LATER_MS = NOW_MS + 60_000;
            ValidationResult oResult = m_Service.Save(oTx, LATER_MS);

            Assert.IsTrue(oResult.IsValid, oResult.ErrorMessage);

            Transaction oReloaded = m_TxRepo.GetById(oTx.Id);
            Assert.AreEqual("改成晚饭", oReloaded.Note);
            Assert.AreEqual(NOW_MS, oReloaded.CreatedAtMs, "创建时间不应被更新覆盖");
            Assert.AreEqual(LATER_MS, oReloaded.UpdatedAtMs);
        }

        [Test]
        public void Save_KeepsEmojiNoteIntact()
        {
            // 真机验收时 emoji 备注显示成空白（android-release-checklist.md #16）。
            // 要判定「是字体画不出来」还是「数据真的丢了」，得把写入链路的每一段都证干净。
            // 仓储那一段 TransactionRepositoryTests.Note_SupportsChineseAndEmoji 已经证过，
            // 这里补上写账单的必经之路（Save 会顺带跑校验、盖时间戳）。
            // 两段都干净，剩下的解释就只有渲染。
            Transaction oTx = _newExpense(35.50m);
            oTx.Note = "和朋友吃饭🍜";

            ValidationResult oResult = m_Service.Save(oTx, NOW_MS);

            Assert.IsTrue(oResult.IsValid, oResult.ErrorMessage);
            Assert.AreEqual("和朋友吃饭🍜", m_TxRepo.GetById(oTx.Id).Note);
        }

        [Test]
        public void Save_InvalidCategoryForType_IsRejected()
        {
            int iIncomeCategoryId = m_CategoryRepo.GetByKind(CategoryKind.Income)[0].Id;
            Transaction oTx = _newExpense(30m);
            oTx.CategoryId = iIncomeCategoryId;

            Assert.IsFalse(m_Service.Save(oTx, NOW_MS).IsValid);
        }

        [Test]
        public void CreateTransfer_WritesSingleAtomicRow()
        {
            ValidationResult oResult = m_Service.CreateTransfer(
                m_CashId, m_AlipayId, Money.FromYuan(200m).Cents, "充值", OCCURRED_MS, NOW_MS);

            Assert.IsTrue(oResult.IsValid, oResult.ErrorMessage);
            Assert.AreEqual(1, m_TxRepo.CountAll(), "转账应只写一条记录，天然原子");

            Transaction oTx = m_TxRepo.Query(new TransactionQuery { Limit = 10 })[0];
            Assert.AreEqual(TxType.Transfer, oTx.Type);
            Assert.AreEqual(m_CashId, oTx.AccountId);
            Assert.AreEqual(m_AlipayId, oTx.ToAccountId);
            Assert.AreEqual(0, oTx.CategoryId);
        }

        [Test]
        public void CreateTransfer_MovesBalancesInBothAccounts()
        {
            m_Service.CreateTransfer(
                m_CashId, m_AlipayId, Money.FromYuan(200m).Cents, "充值", OCCURRED_MS, NOW_MS);

            Assert.AreEqual(-20000, m_AccountRepo.GetBalance(m_CashId).Cents);
            Assert.AreEqual(20000, m_AccountRepo.GetBalance(m_AlipayId).Cents);
            Assert.AreEqual(0, m_AccountRepo.GetTotalAssets().Cents, "转账不改变总资产");
        }

        [Test]
        public void CreateTransfer_ToSameAccount_IsRejected()
        {
            ValidationResult oResult = m_Service.CreateTransfer(
                m_CashId, m_CashId, 10000, "自己转自己", OCCURRED_MS, NOW_MS);

            Assert.IsFalse(oResult.IsValid);
            Assert.AreEqual(0, m_TxRepo.CountAll());
        }

        [Test]
        public void CreateTransfer_ZeroAmount_IsRejected()
        {
            Assert.IsFalse(m_Service.CreateTransfer(
                m_CashId, m_AlipayId, 0, "", OCCURRED_MS, NOW_MS).IsValid);
        }

        [Test]
        public void DeleteTransfer_RestoresBothBalances()
        {
            m_Service.CreateTransfer(
                m_CashId, m_AlipayId, Money.FromYuan(200m).Cents, "充值", OCCURRED_MS, NOW_MS);

            Transaction oTx = m_TxRepo.Query(new TransactionQuery { Limit = 10 })[0];
            m_TxRepo.Delete(oTx.Id);

            Assert.AreEqual(0, m_AccountRepo.GetBalance(m_CashId).Cents);
            Assert.AreEqual(0, m_AccountRepo.GetBalance(m_AlipayId).Cents);
        }

        [Test]
        public void UpdateTransfer_TargetAccount_RecomputesBothSides()
        {
            m_Service.CreateTransfer(
                m_CashId, m_AlipayId, Money.FromYuan(200m).Cents, "充值", OCCURRED_MS, NOW_MS);
            Transaction oTx = m_TxRepo.Query(new TransactionQuery { Limit = 10 })[0];

            // 改为转给另一个账户：先建第三个账户
            int iBankId = m_AccountRepo.Insert(new Account { Name = "银行卡", CreatedAtMs = NOW_MS });
            oTx.ToAccountId = iBankId;
            m_Service.Save(oTx, NOW_MS);

            Assert.AreEqual(-20000, m_AccountRepo.GetBalance(m_CashId).Cents);
            Assert.AreEqual(0, m_AccountRepo.GetBalance(m_AlipayId).Cents, "旧目标账户应恢复");
            Assert.AreEqual(20000, m_AccountRepo.GetBalance(iBankId).Cents);
        }
    }
}
