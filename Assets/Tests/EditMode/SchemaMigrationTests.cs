using System;
using System.Collections.Generic;
using System.IO;
using EasyMoney.Core;
using EasyMoney.Data;
using NUnit.Framework;
using SQLite;

namespace EasyMoney.Tests
{
    /// <summary>
    /// 表结构版本管理。
    ///
    /// 存在的理由：建表语句全是 CREATE TABLE IF NOT EXISTS，对**已经存在的表**等于
    /// 什么都不做。第二版只要给 account / category / tx 加一列，新装的 App 有这列、
    /// 升级上来的没有，查询就报 no such column——是崩溃，不是降级。而第一版已经装在
    /// 真机上，里面有真实数据。
    ///
    /// 前半部分用**合成迁移**驱动 <see cref="SchemaMigrator"/>：机制（这个类）与迁移
    /// 清单（SchemaMigrations）分开，机制本身照样能被钉住，不必等有真实迁移才测。
    /// 后半部分是真迁移（v2 给预置分类补图标名）的用例——那是**数据回填**，
    /// 只测机制看不见，得拿真库跑一遍升级路径。
    /// </summary>
    public class SchemaMigrationTests
    {
        private EasyMoneyDb m_Db;

        [SetUp]
        public void SetUp()
        {
            m_Db = TestDb.Create();

            // 迁移的落点。真实迁移会 ALTER 业务表，测试用这张探针表观察效果，
            // 免得断言绑死在某次具体的表结构改动上。
            m_Db.Connection.Execute(
                "CREATE TABLE probe (id INTEGER PRIMARY KEY AUTOINCREMENT, tag TEXT)");
        }

        [TearDown]
        public void TearDown()
        {
            m_Db.Dispose();
        }

        private static SchemaMigration _migration(int iVersion, params string[] lStatements)
        {
            return new SchemaMigration { Version = iVersion, Statements = lStatements };
        }

        /// <summary>造一条「往探针表里插一行」的迁移，用一个可观察的副作用代表结构变更。</summary>
        private static SchemaMigration _tagMigration(int iVersion, string sTag)
        {
            return _migration(iVersion, $"INSERT INTO probe (tag) VALUES ('{sTag}')");
        }

        /// <summary>
        /// 探针表的一行。sqlite-net 的 Query&lt;T&gt; 带 new() 约束，string 没有无参构造，
        /// 所以不能 Query&lt;string&gt; 直接取标量——得给一个能按列名映射的类型。
        /// </summary>
        public sealed class ProbeRow
        {
            public string Tag { get; set; }
        }

        private List<string> _tags()
        {
            List<string> lTags = new List<string>();

            foreach (ProbeRow oRow in m_Db.Connection.Query<ProbeRow>(
                "SELECT tag AS Tag FROM probe ORDER BY id"))
            {
                lTags.Add(oRow.Tag);
            }

            return lTags;
        }

        // ── 接线：Open 要真的调用迁移器 ──────────────

        [Test]
        public void Open_StampsSchemaVersionOnFreshDatabase()
        {
            // 这条同时是接线测试：TestDb.Create() 走的就是 Open()。
            // 全新库的建表语句已经是当前版本的样子，没有历史要补，直接盖当前版本号。
            Assert.AreEqual(EasyMoneyDb.SCHEMA_VERSION,
                SchemaMigrator.ReadVersion(m_Db.Connection));
        }

        // ── 升级 ────────────────────────────────────

        [Test]
        public void Migrate_RunsMissingMigrationsInOrder()
        {
            SchemaMigrator.WriteVersion(m_Db.Connection, 1);

            SchemaMigrator.Migrate(m_Db.Connection, 3, new[]
            {
                _tagMigration(2, "v2"),
                _tagMigration(3, "v3")
            });

            CollectionAssert.AreEqual(new[] { "v2", "v3" }, _tags());
            Assert.AreEqual(3, SchemaMigrator.ReadVersion(m_Db.Connection));
        }

        [Test]
        public void Migrate_AppliesEachMigrationOnlyOnce()
        {
            SchemaMigrator.WriteVersion(m_Db.Connection, 1);
            IReadOnlyList<SchemaMigration> lAll = new[] { _tagMigration(2, "v2") };

            SchemaMigrator.Migrate(m_Db.Connection, 2, lAll);
            // 再启动一次。老用户每次打开 App 都会走这里，迁移绝不能重跑——
            // 重跑一次 ALTER TABLE 就是崩，重跑一次 INSERT 就是重复数据。
            SchemaMigrator.Migrate(m_Db.Connection, 2, lAll);

            CollectionAssert.AreEqual(new[] { "v2" }, _tags());
        }

        [Test]
        public void Migrate_SkipsMigrationsNewerThanTarget()
        {
            // 清单里可能已经写了为将来准备的迁移，目标版本还没到就不该跑
            SchemaMigrator.WriteVersion(m_Db.Connection, 1);

            SchemaMigrator.Migrate(m_Db.Connection, 2, new[]
            {
                _tagMigration(2, "v2"),
                _tagMigration(5, "v5")
            });

            CollectionAssert.AreEqual(new[] { "v2" }, _tags());
        }

        [Test]
        public void Migrate_OutOfOrderList_StillRunsInVersionOrder()
        {
            // 清单写乱了顺序不该导致迁移乱序执行——顺序错了，后面的迁移会建立在
            // 前面的迁移还没跑的前提上
            SchemaMigrator.WriteVersion(m_Db.Connection, 1);

            SchemaMigrator.Migrate(m_Db.Connection, 3, new[]
            {
                _tagMigration(3, "v3"),
                _tagMigration(2, "v2")
            });

            CollectionAssert.AreEqual(new[] { "v2", "v3" }, _tags());
        }

        // ── 降级 ────────────────────────────────────

        [Test]
        public void Migrate_OnDowngrade_RunsNothingAndKeepsHigherVersion()
        {
            // 装过新版又退回旧版。把版本号改小是错的：用户再装回新版时，
            // 两边版本号又对不上，这些迁移会被重跑一遍。
            SchemaMigrator.WriteVersion(m_Db.Connection, 3);

            SchemaMigrator.Migrate(m_Db.Connection, 1, new[] { _tagMigration(2, "v2") });

            CollectionAssert.IsEmpty(_tags(), "降级时不应执行任何迁移");
            Assert.AreEqual(3, SchemaMigrator.ReadVersion(m_Db.Connection),
                "版本号不能被改小");
        }

        // ── 失败处理 ────────────────────────────────

        [Test]
        public void Migrate_FailedMigration_RollsBackAndKeepsOldVersion()
        {
            SchemaMigrator.WriteVersion(m_Db.Connection, 1);

            Assert.Throws<SQLiteException>(() => SchemaMigrator.Migrate(m_Db.Connection, 2, new[]
            {
                _migration(2,
                    "INSERT INTO probe (tag) VALUES ('half')",
                    "THIS IS NOT VALID SQL")
            }));

            // 一个版本内的语句要么全成要么全不成。留一个「改了一半」的库最难修：
            // 旧版本不认它，新版本也不认它。
            CollectionAssert.IsEmpty(_tags(), "失败版本内的语句必须整体回滚");
            Assert.AreEqual(1, SchemaMigrator.ReadVersion(m_Db.Connection),
                "没升成功就不能盖章，否则下次启动会跳过这段迁移");
        }

        // ── 迁移清单自身的守卫 ──────────────────────

        [Test]
        public void MigrationList_TargetsNoVersionBeyondSchemaVersion()
        {
            // 加了迁移却忘了把 SCHEMA_VERSION 加一：这个迁移永远不会被执行，
            // 而编辑器里、测试里、全新安装全都正常，只有升级上来的老库缺列。
            int iHighest = 0;
            foreach (SchemaMigration oMigration in SchemaMigrations.ALL)
            {
                iHighest = Math.Max(iHighest, oMigration.Version);
            }

            Assert.LessOrEqual(iHighest, EasyMoneyDb.SCHEMA_VERSION,
                "迁移清单里有比 SCHEMA_VERSION 更新的版本——它永远不会被执行");
        }

        [Test]
        public void MigrationList_HasNoDuplicateVersions()
        {
            HashSet<int> setSeen = new HashSet<int>();

            foreach (SchemaMigration oMigration in SchemaMigrations.ALL)
            {
                Assert.IsTrue(setSeen.Add(oMigration.Version),
                    $"版本 {oMigration.Version} 出现了不止一条迁移");
            }
        }

        // ── 真实迁移：v2 给预置分类补图标名 ─────────

        /// <summary>
        /// 落一个临时文件库，先用 <paramref name="oPrepare"/> 把它弄成想要的样子，关掉，
        /// 再重开——**重开这一步才会跑迁移**——最后交给 <paramref name="oVerify"/> 断言。
        ///
        /// 不用 :memory:：内存库一 Close 数据就没了，重开等于新建，走的是建表那条路，
        /// 迁移根本不会被执行。
        /// </summary>
        private static void _withReopenedDb(Action<EasyMoneyDb> oPrepare, Action<EasyMoneyDb> oVerify)
        {
            string sPath = Path.Combine(Path.GetTempPath(), $"easymoney_mig_{Guid.NewGuid():N}.db");

            try
            {
                using (EasyMoneyDb oFirst = new EasyMoneyDb(sPath))
                {
                    oFirst.Open();
                    oPrepare(oFirst);
                }

                using (EasyMoneyDb oSecond = new EasyMoneyDb(sPath))
                {
                    oSecond.Open();
                    oVerify(oSecond);
                }
            }
            finally
            {
                // WAL 模式下可能留下 -wal / -shm 旁挂文件，一并清掉
                foreach (string sSuffix in new[] { string.Empty, "-wal", "-shm" })
                {
                    if (File.Exists(sPath + sSuffix))
                    {
                        File.Delete(sPath + sSuffix);
                    }
                }
            }
        }

        /// <summary>把库退回成第一版的样子：图标名清空 + 版本号写回 1。</summary>
        private static void _makeLegacy(EasyMoneyDb oDb)
        {
            oDb.Connection.Execute("UPDATE category SET icon_name = ''");
            SchemaMigrator.WriteVersion(oDb.Connection, 1);
        }

        [Test]
        public void CategoryIconMigration_BackfillsLegacyDatabase()
        {
            _withReopenedDb(_makeLegacy, oDb =>
            {
                List<Category> lAll = new SqliteCategoryRepository(oDb).GetAll();

                Assert.IsNotEmpty(lAll, "预置分类应当还在");
                foreach (Category oCategory in lAll)
                {
                    Assert.IsNotEmpty(oCategory.IconName, $"{oCategory.Name} 升级后应当有图标名");
                }
            });
        }

        [Test]
        public void CategoryIconMigration_LeavesUserCategoriesAlone()
        {
            // 数据回填最容易伤到的就是用户自己建的东西：同名分类被顺手改了图标
            _withReopenedDb(oDb =>
            {
                _makeLegacy(oDb);

                oDb.Connection.Execute(
                    @"INSERT INTO category (name, kind, parent_id, icon_name, sort_order, is_system)
                      VALUES ('餐饮', 0, 0, '', 9000, 0)");
            },
            oDb =>
            {
                int iTouched = oDb.Connection.ExecuteScalar<int>(
                    "SELECT COUNT(*) FROM category WHERE is_system = 0 AND icon_name <> ''");

                Assert.AreEqual(0, iTouched, "用户自建分类不该被迁移改到");
            });
        }

        [Test]
        public void CategoryIconMigration_MatchesFreshInstallSeed()
        {
            // 「老库升级」和「全新安装」是两条完全不同的路：前者走迁移里的 UPDATE，
            // 后者走 DefaultCategories 的建表种子。两条路必须到达同一个状态——
            // 只改一边的话，一半用户看得到图标、另一半看不到，而且两种人都在用同一个版本号。
            // 这条把这个不变式写成断言，省得将来靠人记住「改这张表要同时改两处」
            _withReopenedDb(_makeLegacy, oDb =>
            {
                Dictionary<string, string> dSeeded = new Dictionary<string, string>();
                foreach (Category oCategory in DefaultCategories.Build())
                {
                    dSeeded[$"{oCategory.Kind}|{oCategory.Name}"] = oCategory.IconName;
                }

                List<Category> lUpgraded = new SqliteCategoryRepository(oDb).GetAll();
                Assert.AreEqual(dSeeded.Count, lUpgraded.Count,
                    "升级上来的分类数量应与全新安装一致");

                foreach (Category oCategory in lUpgraded)
                {
                    string sKey = $"{oCategory.Kind}|{oCategory.Name}";

                    Assert.IsTrue(dSeeded.ContainsKey(sKey),
                        $"升级后的库里冒出了预置清单之外的分类：{sKey}");
                    Assert.AreEqual(dSeeded[sKey], oCategory.IconName,
                        $"{oCategory.Name} 升级后拿到的图标与全新安装的不一致");
                }
            });
        }

        [Test]
        public void CategoryIconMigration_DoesNotOverwriteExistingIcon()
        {
            _withReopenedDb(oDb =>
            {
                _makeLegacy(oDb);

                // 假装「餐饮」的图标名早就被设成了别的值
                oDb.Connection.Execute(
                    "UPDATE category SET icon_name = 'cat_custom' WHERE name = '餐饮' AND is_system = 1");
            },
            oDb =>
            {
                string sIcon = oDb.Connection.ExecuteScalar<string>(
                    "SELECT icon_name FROM category WHERE name = '餐饮' AND is_system = 1");

                Assert.AreEqual("cat_custom", sIcon, "已经有图标名的分类不该被覆盖");
            });
        }
    }
}
