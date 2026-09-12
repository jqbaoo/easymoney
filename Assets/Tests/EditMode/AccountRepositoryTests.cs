using System.Collections.Generic;
using EasyMoney.Core;
using EasyMoney.Data;
using NUnit.Framework;

namespace EasyMoney.Tests
{
    public class AccountRepositoryTests
    {
        private EasyMoneyDb m_Db;
        private IAccountRepository m_Repo;
        private ITransactionRepository m_TxRepo;

        [SetUp]
        public void SetUp()
        {
            m_Db = TestDb.Create();
            m_Repo = new SqliteAccountRepository(m_Db);
            m_TxRepo = new SqliteTransactionRepository(m_Db);
        }

        [TearDown]
        public void TearDown()
        {
            m_Db.Dispose();
        }

        private int _createAccount(string sName, decimal dInitialYuan)
        {
            return m_Repo.Insert(new Account
            {
                Name = sName,
                Type = AccountType.Cash,
                InitialBalanceCents = Money.FromYuan(dInitialYuan).Cents,
                CreatedAtMs = 1_700_000_000_000L
            });
        }

        private void _addTx(int iAccountId, TxType oType, decimal dYuan, int iToAccountId = 0)
        {
            m_TxRepo.Insert(new Transaction
            {
                Type = oType,
                AmountCents = Money.FromYuan(dYuan).Cents,
                AccountId = iAccountId,
                ToAccountId = iToAccountId,
                OccurredAtMs = 1_700_000_000_000L
            });
        }

        [Test]
        public void Insert_RoundTripsAllFields()
        {
            int iId = _createAccount("招商银行", 1234.56m);
            Account oLoaded = m_Repo.GetById(iId);

            Assert.AreEqual("招商银行", oLoaded.Name);
            Assert.AreEqual(AccountType.Cash, oLoaded.Type);
            Assert.AreEqual(123456, oLoaded.InitialBalanceCents);
            Assert.IsFalse(oLoaded.IsArchived);
        }

        [Test]
        public void GetById_ReturnsNullWhenMissing()
        {
            Assert.IsNull(m_Repo.GetById(999999));
        }

        [Test]
        public void GetBalance_NoTransactions_EqualsInitial()
        {
            int iId = _createAccount("现金", 100m);
            Assert.AreEqual(10000, m_Repo.GetBalance(iId).Cents);
        }

        [Test]
        public void GetBalance_IncomeIncreasesExpenseDecreases()
        {
            int iId = _createAccount("现金", 100m);
            _addTx(iId, TxType.Income, 50m);
            _addTx(iId, TxType.Expense, 30m);

            Assert.AreEqual(12000, m_Repo.GetBalance(iId).Cents);
        }

        [Test]
        public void GetBalance_TransferMovesMoneyBetweenAccounts()
        {
            int iFrom = _createAccount("现金", 100m);
            int iTo = _createAccount("支付宝", 20m);

            _addTx(iFrom, TxType.Transfer, 40m, iTo);

            Assert.AreEqual(6000, m_Repo.GetBalance(iFrom).Cents, "转出账户应减少");
            Assert.AreEqual(6000, m_Repo.GetBalance(iTo).Cents, "转入账户应增加");
        }

        [Test]
        public void GetTotalAssets_ExcludesArchivedAccounts()
        {
            _createAccount("现金", 100m);
            int iOld = _createAccount("废弃卡", 500m);
            m_Repo.SetArchived(iOld, true);

            Assert.AreEqual(10000, m_Repo.GetTotalAssets().Cents);
        }

        [Test]
        public void GetAll_HidesArchivedByDefault()
        {
            _createAccount("现金", 100m);
            int iOld = _createAccount("废弃卡", 0m);
            m_Repo.SetArchived(iOld, true);

            Assert.AreEqual(1, m_Repo.GetAll().Count);
            Assert.AreEqual(2, m_Repo.GetAll(bIncludeArchived: true).Count);
        }

        [Test]
        public void GetAllWithBalance_ComputesEachAccount()
        {
            int iA = _createAccount("A", 100m);
            _createAccount("B", 50m);
            _addTx(iA, TxType.Expense, 25m);

            List<AccountBalance> lBalances = m_Repo.GetAllWithBalance();
            Assert.AreEqual(2, lBalances.Count);
            Assert.AreEqual(7500, lBalances[0].Balance.Cents);
            Assert.AreEqual(5000, lBalances[1].Balance.Cents);
        }

        [Test]
        public void GetAll_SortedBySortOrderThenId()
        {
            m_Repo.Insert(new Account { Name = "后", SortOrder = 20, CreatedAtMs = 1 });
            m_Repo.Insert(new Account { Name = "先", SortOrder = 10, CreatedAtMs = 2 });

            List<Account> lAll = m_Repo.GetAll();
            Assert.AreEqual("先", lAll[0].Name);
            Assert.AreEqual("后", lAll[1].Name);
        }

        [Test]
        public void Delete_RefusesAccountWithTransactions()
        {
            int iId = _createAccount("现金", 100m);
            _addTx(iId, TxType.Expense, 1m);
            Assert.IsFalse(m_Repo.Delete(iId));
        }

        [Test]
        public void Delete_RemovesUnusedAccount()
        {
            int iId = _createAccount("现金", 100m);
            Assert.IsTrue(m_Repo.Delete(iId));
            Assert.IsNull(m_Repo.GetById(iId));
        }

        [Test]
        public void Update_PersistsChanges()
        {
            int iId = _createAccount("旧名", 0m);
            Account oAccount = m_Repo.GetById(iId);
            oAccount.Name = "新名";
            oAccount.Type = AccountType.WeChat;
            oAccount.InitialBalanceCents = 8888;
            m_Repo.Update(oAccount);

            Account oReloaded = m_Repo.GetById(iId);
            Assert.AreEqual("新名", oReloaded.Name);
            Assert.AreEqual(AccountType.WeChat, oReloaded.Type);
            Assert.AreEqual(8888, oReloaded.InitialBalanceCents);
        }
    }
}
