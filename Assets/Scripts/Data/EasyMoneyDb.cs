using SQLite;

namespace EasyMoney.Data
{
    /// <summary>
    /// 数据库连接与建表。传 ":memory:" 得到内存库（测试用），
    /// 传文件路径得到持久化库（运行时用 Application.persistentDataPath）。
    /// </summary>
    public sealed class EasyMoneyDb : System.IDisposable
    {
        public const int SCHEMA_VERSION = 1;

        private readonly string m_DbPath;
        private SQLiteConnection m_Connection;

        public EasyMoneyDb(string sDbPath)
        {
            m_DbPath = sDbPath;
        }

        public SQLiteConnection Connection
        {
            get
            {
                if (m_Connection == null)
                {
                    throw new System.InvalidOperationException("数据库尚未打开，请先调用 Open()");
                }
                return m_Connection;
            }
        }

        public void Open()
        {
            if (m_Connection != null)
            {
                return;
            }

            SQLitePCL.Batteries_V2.Init();

            m_Connection = new SQLiteConnection(m_DbPath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create);
            m_Connection.Execute("PRAGMA foreign_keys = ON;");
            // journal_mode 是「会返回结果行」的 PRAGMA（返回切换后的模式名），
            // 必须用 ExecuteScalar 把这一行读掉：用 Execute 会走 ExecuteNonQuery，
            // Step() 返回 ROW 时它落入兜底分支并抛 SQLiteException("not an error")。
            // 内存库无法切到 WAL，读回 "memory" 属正常，不报错。
            m_Connection.ExecuteScalar<string>("PRAGMA journal_mode = WAL;");

            _createTables();
            _ensureSchemaVersion();
        }

        public void Close()
        {
            if (m_Connection == null)
            {
                return;
            }

            m_Connection.Close();
            m_Connection = null;
        }

        public void Dispose()
        {
            Close();
        }

        private void _createTables()
        {
            m_Connection.Execute(Schema.CREATE_SCHEMA_VERSION);
            m_Connection.Execute(Schema.CREATE_ACCOUNT);
            m_Connection.Execute(Schema.CREATE_CATEGORY);
            m_Connection.Execute(Schema.CREATE_TX);
            m_Connection.Execute(Schema.CREATE_INDEXES);
        }

        private void _ensureSchemaVersion()
        {
            int iCount = m_Connection.ExecuteScalar<int>("SELECT COUNT(*) FROM schema_version");
            if (iCount == 0)
            {
                m_Connection.Execute("INSERT INTO schema_version (version) VALUES (?)", SCHEMA_VERSION);
            }
        }
    }
}
