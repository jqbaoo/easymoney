using System.Collections.Generic;
using SQLite;

namespace EasyMoney.Data
{
    /// <summary>一次结构升级：把库从 Version-1 升到 Version。</summary>
    public sealed class SchemaMigration
    {
        /// <summary>升到哪个版本。与 <see cref="EasyMoneyDb.SCHEMA_VERSION"/> 用同一套编号。</summary>
        public int Version;

        /// <summary>按顺序执行的 SQL。同一版本内的语句是一个整体，失败会一起回滚。</summary>
        public string[] Statements;
    }

    /// <summary>
    /// 表结构版本管理。
    ///
    /// 为什么必须有：建表语句全是 CREATE TABLE IF NOT EXISTS，对**已经存在的表**
    /// 等于什么都不做。第二版只要给 account 加一列，新装的 App 有这列、升级上来的没有，
    /// 一查询就报 no such column——是崩溃，不是降级。而第一版已经装在真机上了，
    /// 里面有真实数据。
    ///
    /// 机制（这个类）与迁移清单（<see cref="SchemaMigrations"/>）分开：这里只管
    /// 「按版本号把该跑的跑了」，某个版本具体执行什么 SQL 不归它管。分开是为了可测——
    /// 现在还没有真实迁移，测试用合成迁移驱动这个类，机制本身照样被钉住。
    /// </summary>
    public static class SchemaMigrator
    {
        /// <summary>schema_version 表里一行都没有时读出来的值。</summary>
        public const int NO_VERSION = 0;

        /// <summary>
        /// 读出当前库的结构版本。空表返回 <see cref="NO_VERSION"/>。
        /// 用 ExecuteScalar 而不是 Query&lt;int&gt;：后者会走「按列名映射到类型属性」
        /// 那套，对 int 这种没有属性的类型能不能读出来是另一回事；而且没有行时
        /// 返回的是空列表，还得自己兜底。ExecuteScalar 读不到行时直接给 default(int)，
        /// 正好就是 NO_VERSION。
        /// </summary>
        public static int ReadVersion(SQLiteConnection oConnection)
        {
            return oConnection.ExecuteScalar<int>("SELECT version FROM schema_version LIMIT 1");
        }

        /// <summary>写入结构版本。这张表只该有一行，先清后写以保证这一点。</summary>
        public static void WriteVersion(SQLiteConnection oConnection, int iVersion)
        {
            oConnection.Execute("DELETE FROM schema_version");
            oConnection.Execute("INSERT INTO schema_version (version) VALUES (?)", iVersion);
        }

        /// <summary>
        /// 把库升到 <paramref name="iTargetVersion"/>，按需执行缺失的迁移。
        /// 用户可能从 1 直接装到 3，所以中间那步不能跳——缺哪步补哪步。
        /// </summary>
        public static void Migrate(
            SQLiteConnection oConnection,
            int iTargetVersion,
            IReadOnlyList<SchemaMigration> lMigrations)
        {
            int iStored = ReadVersion(oConnection);

            if (iStored == NO_VERSION)
            {
                // 全新库：建表语句已经是当前版本的样子，没有历史要补，直接盖章。
                // 注意这跟「已经是最新版」是两回事，不能混在一起判。
                WriteVersion(oConnection, iTargetVersion);
                return;
            }

            if (iStored > iTargetVersion)
            {
                // 装过更新的版本，又退回了旧版。既不能往上跑迁移，也不能把版本号改小：
                // 改小了，用户再装回新版时两边版本号又对不上，这些迁移会被重跑一遍。
                // 留着较大的版本号，下次装新版时两边相等，天然不重跑。
                return;
            }

            if (lMigrations == null)
            {
                WriteVersion(oConnection, iTargetVersion);
                return;
            }

            foreach (SchemaMigration oMigration in _sortedByVersion(lMigrations))
            {
                // 已经走过的版本不重跑（ALTER TABLE 或 INSERT 重跑一次就是事故），
                // 比目标还新的也不跑（清单里可能已经写了为将来准备的迁移）。
                if (oMigration.Version <= iStored || oMigration.Version > iTargetVersion)
                {
                    continue;
                }

                _apply(oConnection, oMigration);
            }

            WriteVersion(oConnection, iTargetVersion);
        }

        /// <summary>
        /// 一个版本的所有语句包在一个事务里。升级到一半失败时，宁可停在旧版本，
        /// 也不要留一个「改了一半」的库——那种库旧版本不认、新版本也不认，最难修。
        /// </summary>
        private static void _apply(SQLiteConnection oConnection, SchemaMigration oMigration)
        {
            oConnection.RunInTransaction(() =>
            {
                foreach (string sStatement in oMigration.Statements)
                {
                    oConnection.Execute(sStatement);
                }
            });
        }

        /// <summary>按版本号升序排一份副本。清单写乱了顺序，不该导致迁移乱序执行。</summary>
        private static List<SchemaMigration> _sortedByVersion(
            IReadOnlyList<SchemaMigration> lMigrations)
        {
            List<SchemaMigration> lSorted = new List<SchemaMigration>(lMigrations);
            lSorted.Sort((oLeft, oRight) => oLeft.Version.CompareTo(oRight.Version));
            return lSorted;
        }
    }
}
