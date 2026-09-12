using System;
using System.Collections.Generic;
using System.IO;
using EasyMoney.Core;
using EasyMoney.Data;
using NUnit.Framework;

namespace EasyMoney.Tests
{
    public class CategoryRepositoryTests
    {
        private EasyMoneyDb m_Db;
        private ICategoryRepository m_Repo;

        [SetUp]
        public void SetUp()
        {
            m_Db = TestDb.Create();
            m_Repo = new SqliteCategoryRepository(m_Db);
        }

        [TearDown]
        public void TearDown()
        {
            m_Db.Dispose();
        }

        [Test]
        public void Defaults_AreSeededOnOpen()
        {
            Assert.Greater(m_Repo.CountAll(), 0);
        }

        [Test]
        public void Defaults_ContainCommonExpenseCategories()
        {
            List<Category> lExpense = m_Repo.GetByKind(CategoryKind.Expense);
            List<string> lNames = new List<string>();
            foreach (Category oCategory in lExpense)
            {
                lNames.Add(oCategory.Name);
            }

            Assert.Contains("餐饮", lNames);
            Assert.Contains("购物", lNames);
            Assert.Contains("交通", lNames);
            Assert.Contains("住房", lNames);
        }

        [Test]
        public void Defaults_AreNotDuplicatedWhenOpenedTwice()
        {
            int iBefore = m_Repo.CountAll();
            m_Db.Open();
            m_Db.Open();
            Assert.AreEqual(iBefore, m_Repo.CountAll());
        }

        [Test]
        public void Defaults_AreNotDuplicatedAfterReopen()
        {
            // 上面那个用例测不到种子逻辑：Open() 在连接已存在时直接返回，两次调用都是空操作。
            // 而 :memory: 库一旦 Close 数据就没了，重开必然重新种子，同样测不出幂等。
            // 所以幂等这件事只能落一个临时文件库，走完整的「关掉再打开」路径来验证。
            string sPath = Path.Combine(Path.GetTempPath(), $"easymoney_seed_{Guid.NewGuid():N}.db");
            try
            {
                int iFirstCount;
                using (EasyMoneyDb oFirst = new EasyMoneyDb(sPath))
                {
                    oFirst.Open();
                    iFirstCount = new SqliteCategoryRepository(oFirst).CountAll();
                }

                using (EasyMoneyDb oSecond = new EasyMoneyDb(sPath))
                {
                    oSecond.Open();
                    Assert.AreEqual(iFirstCount, new SqliteCategoryRepository(oSecond).CountAll());
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

        [Test]
        public void Defaults_AreAllMarkedSystem()
        {
            foreach (Category oCategory in m_Repo.GetAll())
            {
                Assert.IsTrue(oCategory.IsSystem, $"{oCategory.Name} 应当标记为系统分类");
            }
        }

        [Test]
        public void Insert_ReturnsNewIdAndRoundTrips()
        {
            Category oNew = new Category
            {
                Name = "宠物",
                Kind = CategoryKind.Expense,
                IconName = "pet",
                SortOrder = 99
            };

            int iId = m_Repo.Insert(oNew);
            Assert.Greater(iId, 0);

            Category oLoaded = m_Repo.GetById(iId);
            Assert.AreEqual("宠物", oLoaded.Name);
            Assert.AreEqual(CategoryKind.Expense, oLoaded.Kind);
            Assert.AreEqual("pet", oLoaded.IconName);
            Assert.AreEqual(99, oLoaded.SortOrder);
            Assert.IsFalse(oLoaded.IsSystem);
            Assert.IsTrue(oLoaded.IsTopLevel);
        }

        [Test]
        public void GetById_ReturnsNullWhenMissing()
        {
            Assert.IsNull(m_Repo.GetById(999999));
        }

        [Test]
        public void GetChildren_ReturnsOnlyDirectChildren()
        {
            int iParentId = m_Repo.Insert(new Category { Name = "餐饮", Kind = CategoryKind.Expense });
            m_Repo.Insert(new Category { Name = "早餐", Kind = CategoryKind.Expense, ParentId = iParentId });
            m_Repo.Insert(new Category { Name = "午餐", Kind = CategoryKind.Expense, ParentId = iParentId });
            m_Repo.Insert(new Category { Name = "无关", Kind = CategoryKind.Expense });

            List<Category> lChildren = m_Repo.GetChildren(iParentId);
            Assert.AreEqual(2, lChildren.Count);
        }

        [Test]
        public void Delete_RefusesSystemCategory()
        {
            List<Category> lAll = m_Repo.GetAll();
            Assert.IsFalse(m_Repo.Delete(lAll[0].Id));
        }

        [Test]
        public void Delete_RefusesCategoryWithChildren()
        {
            int iParentId = m_Repo.Insert(new Category { Name = "父", Kind = CategoryKind.Expense });
            m_Repo.Insert(new Category { Name = "子", Kind = CategoryKind.Expense, ParentId = iParentId });
            Assert.IsFalse(m_Repo.Delete(iParentId));
        }

        [Test]
        public void Delete_RemovesLeafUserCategory()
        {
            int iId = m_Repo.Insert(new Category { Name = "临时", Kind = CategoryKind.Expense });
            Assert.IsTrue(m_Repo.Delete(iId));
            Assert.IsNull(m_Repo.GetById(iId));
        }

        [Test]
        public void Update_PersistsChanges()
        {
            int iId = m_Repo.Insert(new Category { Name = "旧名", Kind = CategoryKind.Expense });
            Category oCategory = m_Repo.GetById(iId);
            oCategory.Name = "新名";
            oCategory.SortOrder = 42;
            m_Repo.Update(oCategory);

            Category oReloaded = m_Repo.GetById(iId);
            Assert.AreEqual("新名", oReloaded.Name);
            Assert.AreEqual(42, oReloaded.SortOrder);
        }

        [Test]
        public void GetAll_SortedByKindThenSortOrder()
        {
            List<Category> lAll = m_Repo.GetAll();
            for (int i = 1; i < lAll.Count; i++)
            {
                bool bKindAscending = lAll[i - 1].Kind < lAll[i].Kind;
                bool bSameKindAndOrdered =
                    lAll[i - 1].Kind == lAll[i].Kind &&
                    lAll[i - 1].SortOrder <= lAll[i].SortOrder;

                Assert.IsTrue(bKindAscending || bSameKindAndOrdered,
                    $"排序错误: {lAll[i - 1].Name} 在 {lAll[i].Name} 之前");
            }
        }
    }
}
