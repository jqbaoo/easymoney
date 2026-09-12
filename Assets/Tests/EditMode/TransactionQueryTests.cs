using System.Collections.Generic;
using EasyMoney.Core;
using EasyMoney.Data;
using NUnit.Framework;

namespace EasyMoney.Tests
{
    /// <summary>
    /// 账单筛选测试。每个维度都单独测一遍，再测组合——单维度全对但拼起来
    /// 用了 OR 而不是 AND，是这类拼接逻辑最容易犯的错。
    /// </summary>
    public class TransactionQueryTests
    {
        private const long DAY_MS = 86_400_000L;
        private const long BASE_MS = 1_700_000_000_000L;

        private EasyMoneyDb m_Db;
        private ITransactionRepository m_Repo;
        private IAccountRepository m_AccountRepo;
        private ICategoryRepository m_CategoryRepo;

        private int m_CashId;
        private int m_AlipayId;
        private int m_FoodId;
        private int m_ShoppingId;
        private int m_SalaryId;

        [SetUp]
        public void SetUp()
        {
            m_Db = TestDb.Create();
            m_Repo = new SqliteTransactionRepository(m_Db);
            m_AccountRepo = new SqliteAccountRepository(m_Db);
            m_CategoryRepo = new SqliteCategoryRepository(m_Db);

            m_CashId = m_AccountRepo.Insert(new Account { Name = "现金", CreatedAtMs = BASE_MS });
            m_AlipayId = m_AccountRepo.Insert(new Account { Name = "支付宝", CreatedAtMs = BASE_MS });

            m_FoodId = m_CategoryRepo.GetByKind(CategoryKind.Expense)[0].Id;
            m_ShoppingId = m_CategoryRepo.GetByKind(CategoryKind.Expense)[1].Id;
            m_SalaryId = m_CategoryRepo.GetByKind(CategoryKind.Income)[0].Id;

            _add(TxType.Expense, 30m, m_CashId, m_FoodId, "楼下快餐", BASE_MS);
            _add(TxType.Expense, 200m, m_AlipayId, m_ShoppingId, "买鞋", BASE_MS + DAY_MS);
            _add(TxType.Income, 8000m, m_CashId, m_SalaryId, "八月工资", BASE_MS + 2 * DAY_MS);
            _add(TxType.Expense, 15m, m_CashId, m_FoodId, "早餐", BASE_MS + 3 * DAY_MS);
            _add(TxType.Transfer, 500m, m_CashId, 0, "", BASE_MS + 4 * DAY_MS, m_AlipayId);
        }

        [TearDown]
        public void TearDown()
        {
            m_Db.Dispose();
        }

        private void _add(
            TxType oType, decimal dYuan, int iAccountId, int iCategoryId,
            string sNote, long iOccurredMs, int iToAccountId = 0)
        {
            m_Repo.Insert(new Transaction
            {
                Type = oType,
                AmountCents = Money.FromYuan(dYuan).Cents,
                AccountId = iAccountId,
                ToAccountId = iToAccountId,
                CategoryId = iCategoryId,
                Note = sNote,
                OccurredAtMs = iOccurredMs,
                CreatedAtMs = iOccurredMs,
                UpdatedAtMs = iOccurredMs
            });
        }

        [Test]
        public void NoFilters_ReturnsAllOrderedByTimeDescending()
        {
            List<Transaction> lResult = m_Repo.Query(new TransactionQuery { Limit = 100 });
            Assert.AreEqual(5, lResult.Count);
            Assert.AreEqual("", lResult[0].Note, "最新的应是转账");
            Assert.AreEqual("楼下快餐", lResult[4].Note, "最旧的应是第一笔");
        }

        [Test]
        public void LimitAndOffset_Paginate()
        {
            List<Transaction> lPage1 = m_Repo.Query(new TransactionQuery { Limit = 2, Offset = 0 });
            List<Transaction> lPage2 = m_Repo.Query(new TransactionQuery { Limit = 2, Offset = 2 });

            Assert.AreEqual(2, lPage1.Count);
            Assert.AreEqual(2, lPage2.Count);
            Assert.AreNotEqual(lPage1[0].Id, lPage2[0].Id);
        }

        [Test]
        public void Count_IgnoresLimitAndOffset()
        {
            Assert.AreEqual(5, m_Repo.Count(new TransactionQuery { Limit = 1 }));
        }

        [Test]
        public void FilterByTimeRange_IsLeftClosedRightOpen()
        {
            TransactionQuery oQuery = new TransactionQuery
            {
                StartMs = BASE_MS,
                EndMs = BASE_MS + 2 * DAY_MS,
                Limit = 100
            };

            List<Transaction> lResult = m_Repo.Query(oQuery);
            Assert.AreEqual(2, lResult.Count, "应含起点当天，不含终点当天");
        }

        [Test]
        public void FilterByType_ExpenseOnly()
        {
            List<Transaction> lResult = m_Repo.Query(new TransactionQuery { Type = TxType.Expense, Limit = 100 });
            Assert.AreEqual(3, lResult.Count);
        }

        [Test]
        public void FilterByType_TransferOnly()
        {
            List<Transaction> lResult = m_Repo.Query(new TransactionQuery { Type = TxType.Transfer, Limit = 100 });
            Assert.AreEqual(1, lResult.Count);
        }

        [Test]
        public void FilterByCategory()
        {
            List<Transaction> lResult = m_Repo.Query(new TransactionQuery { CategoryId = m_FoodId, Limit = 100 });
            Assert.AreEqual(2, lResult.Count);
        }

        [Test]
        public void FilterByAccount_HistoryMode_IncludesIncomingTransfers()
        {
            // 支付宝账户直接产生的记录只有 1 笔支出；但有一笔 500 转入，
            // 从「账户流水」角度看应当看到 2 笔。
            TransactionQuery oQuery = new TransactionQuery
            {
                AccountId = m_AlipayId,
                AccountIncludesTransfers = true,
                Limit = 100
            };

            Assert.AreEqual(2, m_Repo.Query(oQuery).Count);
        }

        [Test]
        public void FilterByAccount_OwnRecordsMode_ExcludesIncomingTransfers()
        {
            TransactionQuery oQuery = new TransactionQuery
            {
                AccountId = m_AlipayId,
                AccountIncludesTransfers = false,
                Limit = 100
            };

            Assert.AreEqual(1, m_Repo.Query(oQuery).Count);
        }

        [Test]
        public void FilterByAmountRange()
        {
            TransactionQuery oQuery = new TransactionQuery
            {
                MinCents = Money.FromYuan(20m).Cents,
                MaxCents = Money.FromYuan(300m).Cents,
                Limit = 100
            };

            List<Transaction> lResult = m_Repo.Query(oQuery);
            Assert.AreEqual(2, lResult.Count, "30 元和 200 元两笔");
        }

        [Test]
        public void FilterByAmountRange_IsInclusive()
        {
            TransactionQuery oQuery = new TransactionQuery
            {
                MinCents = Money.FromYuan(30m).Cents,
                MaxCents = Money.FromYuan(30m).Cents,
                Limit = 100
            };

            Assert.AreEqual(1, m_Repo.Query(oQuery).Count);
        }

        [Test]
        public void Keyword_MatchesNote()
        {
            List<Transaction> lResult = m_Repo.Query(new TransactionQuery { Keyword = "餐", Limit = 100 });
            Assert.AreEqual(2, lResult.Count, "「楼下快餐」与「早餐」");
        }

        [Test]
        public void Keyword_IsCaseInsensitive()
        {
            _add(TxType.Expense, 1m, m_CashId, m_FoodId, "Starbucks Coffee", BASE_MS + 5 * DAY_MS);

            Assert.AreEqual(1, m_Repo.Query(new TransactionQuery { Keyword = "starbucks", Limit = 100 }).Count);
            Assert.AreEqual(1, m_Repo.Query(new TransactionQuery { Keyword = "COFFEE", Limit = 100 }).Count);
        }

        [Test]
        public void Keyword_SupportsChinese()
        {
            Assert.AreEqual(1, m_Repo.Query(new TransactionQuery { Keyword = "工资", Limit = 100 }).Count);
        }

        [Test]
        public void Keyword_EscapesLikeWildcards()
        {
            _add(TxType.Expense, 1m, m_CashId, m_FoodId, "100%纯棉", BASE_MS + 6 * DAY_MS);
            _add(TxType.Expense, 1m, m_CashId, m_FoodId, "100X纯棉", BASE_MS + 7 * DAY_MS);

            List<Transaction> lResult = m_Repo.Query(new TransactionQuery { Keyword = "100%", Limit = 100 });
            Assert.AreEqual(1, lResult.Count, "% 应当被当成普通字符而非通配符");
        }

        [Test]
        public void Keyword_IgnoresWhitespaceOnly()
        {
            List<Transaction> lResult = m_Repo.Query(new TransactionQuery { Keyword = "   ", Limit = 100 });
            Assert.AreEqual(5, lResult.Count, "纯空白关键词应视为无筛选");
        }

        [Test]
        public void CombinedFilters_AreAnded()
        {
            TransactionQuery oQuery = new TransactionQuery
            {
                Type = TxType.Expense,
                AccountId = m_CashId,
                StartMs = BASE_MS,
                EndMs = BASE_MS + 4 * DAY_MS,
                Limit = 100
            };

            List<Transaction> lResult = m_Repo.Query(oQuery);
            Assert.AreEqual(2, lResult.Count, "现金账户期间内的两笔支出");
        }

        [Test]
        public void EmptyResult_ReturnsEmptyListNotNull()
        {
            List<Transaction> lResult = m_Repo.Query(new TransactionQuery { Keyword = "不存在的备注", Limit = 100 });
            Assert.IsNotNull(lResult);
            Assert.AreEqual(0, lResult.Count);
        }
    }
}
