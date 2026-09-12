using System;
using EasyMoney.Core;
using EasyMoney.Data;

namespace EasyMoney.App
{
    /// <summary>
    /// 应用级依赖容器。持有数据库连接与全部仓储，界面层统一从这里取数。
    ///
    /// 刻意不碰 UnityEngine：这样它能在 EditMode 测试里直接 new 出来跑，
    /// 端到端链路（建库 → 记账 → 余额 → 查询）不必启动播放模式就能验证。
    /// </summary>
    public sealed class AppContext : IDisposable
    {
        private static AppContext s_Instance;

        private EasyMoneyDb m_Db;
        private bool m_IsDisposed;

        /// <summary>当前容器。未初始化时为 null。</summary>
        public static AppContext Instance => s_Instance;

        public EasyMoneyDb Db => m_Db;

        public IAccountRepository Accounts { get; private set; }

        public ICategoryRepository Categories { get; private set; }

        public ITransactionRepository Transactions { get; private set; }

        public TransactionService TxService { get; private set; }

        /// <summary>
        /// 任何写操作完成后触发，页面据此刷新自己。
        /// 页面自己不互相通知——写入方只管喊一声「数据变了」，
        /// 至于谁需要重画由订阅方自己决定。
        /// </summary>
        public event Action DataChanged;

        /// <summary>
        /// 打开数据库并装配全部仓储。传 ":memory:" 得到内存库（测试用），
        /// 传文件路径得到持久化库（运行时用 Application.persistentDataPath）。
        /// </summary>
        public void Initialize(string sDbPath)
        {
            m_Db = new EasyMoneyDb(sDbPath);
            m_Db.Open();

            Accounts = new SqliteAccountRepository(m_Db);
            Categories = new SqliteCategoryRepository(m_Db);
            Transactions = new SqliteTransactionRepository(m_Db);
            TxService = new TransactionService(Transactions, Accounts, Categories);

            // 放在最后：上面任何一步抛异常都不该把半残的容器暴露成 Instance
            s_Instance = this;
        }

        public void NotifyDataChanged()
        {
            DataChanged?.Invoke();
        }

        public void Dispose()
        {
            if (m_IsDisposed)
            {
                return;
            }

            m_IsDisposed = true;

            if (m_Db != null)
            {
                m_Db.Dispose();
                m_Db = null;
            }

            if (s_Instance == this)
            {
                s_Instance = null;
            }
        }
    }
}
