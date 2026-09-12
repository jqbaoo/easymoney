using EasyMoney.App;
using EasyMoney.Core;
using NUnit.Framework;

namespace EasyMoney.Tests
{
    /// <summary>
    /// 应用容器的接线测试。除了「五个依赖都装配上了」，还要跑通一条
    /// 建账户 → 记账 → 读回余额 → 查得到记录的完整链路：
    /// 单看每个仓储各自的测试，证明不了 AppContext 把参数接对了
    /// （例如把三个仓储的构造顺序接反，单仓储测试发现不了）。
    /// </summary>
    public class AppContextTests
    {
        private AppContext m_Context;

        [SetUp]
        public void SetUp()
        {
            m_Context = new AppContext();
            m_Context.Initialize(":memory:");
        }

        [TearDown]
        public void TearDown()
        {
            m_Context.Dispose();
        }

        [Test]
        public void Initialize_WiresUpAllRepositories()
        {
            Assert.IsNotNull(m_Context.Db);
            Assert.IsNotNull(m_Context.Accounts);
            Assert.IsNotNull(m_Context.Categories);
            Assert.IsNotNull(m_Context.Transactions);
            Assert.IsNotNull(m_Context.TxService);
        }

        [Test]
        public void Initialize_SeedsDefaultCategories()
        {
            Assert.Greater(m_Context.Categories.CountAll(), 0);
        }

        [Test]
        public void EndToEnd_RecordExpenseThenReadBack()
        {
            int iAccountId = m_Context.Accounts.Insert(new Account { Name = "现金" });
            int iCategoryId = m_Context.Categories.GetByKind(CategoryKind.Expense)[0].Id;

            Transaction oTx = new Transaction
            {
                Type = TxType.Expense,
                AmountCents = Money.FromYuan(35.5m).Cents,
                AccountId = iAccountId,
                CategoryId = iCategoryId,
                Note = "晚饭",
                OccurredAtMs = 1_700_000_000_000L
            };

            ValidationResult oResult = m_Context.TxService.Save(oTx, 1_800_000_000_000L);
            Assert.IsTrue(oResult.IsValid, oResult.ErrorMessage);

            Assert.AreEqual(-3550, m_Context.Accounts.GetBalance(iAccountId).Cents);
            Assert.AreEqual(1, m_Context.Transactions.Count(new TransactionQuery()));
        }

        [Test]
        public void NotifyDataChanged_RaisesEvent()
        {
            int iCount = 0;
            m_Context.DataChanged += () => iCount++;

            m_Context.NotifyDataChanged();
            m_Context.NotifyDataChanged();

            Assert.AreEqual(2, iCount);
        }

        [Test]
        public void Dispose_IsIdempotent()
        {
            m_Context.Dispose();
            Assert.DoesNotThrow(() => m_Context.Dispose());
        }

        [Test]
        public void Initialize_SetsSingletonInstance()
        {
            // Instance 是页面取数的唯一入口，必须指向刚初始化的这一个
            Assert.AreSame(m_Context, AppContext.Instance);
        }

        [Test]
        public void Dispose_ClearsSingletonInstance()
        {
            m_Context.Dispose();

            Assert.IsNull(AppContext.Instance, "已释放的容器不应继续被页面取到");
        }
    }
}
