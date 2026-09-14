using System.Collections.Generic;
using EasyMoney.Core;
using NUnit.Framework;

namespace EasyMoney.Tests
{
    /// <summary>
    /// 草稿里的名字 → 数据库 Id 的匹配测试。
    ///
    /// 盯住两件事：**匹配不上返回 0**（而不是抛异常或猜一个），
    /// 以及**分类要按收支方向过滤**——语音说了个支出分类而类型是收入时，
    /// 让它落空比让它填错好：填错了要等到保存才报错，用户会以为问题出在别处。
    /// </summary>
    public class DraftResolverTests
    {
        // ── 账户 ────────────────────────────────────

        [Test]
        public void MatchAccountId_Found_ReturnsId()
        {
            Assert.AreEqual(2, DraftResolver.MatchAccountId("微信", _accounts()));
        }

        [Test]
        public void MatchAccountId_UnknownName_ReturnsZero()
        {
            // 「张三」不是资金账户。返回 0 让表单停在「请选择」，用户看得见
            Assert.AreEqual(0, DraftResolver.MatchAccountId("张三", _accounts()));
        }

        [Test]
        public void MatchAccountId_EmptyName_ReturnsZero()
        {
            Assert.AreEqual(0, DraftResolver.MatchAccountId(string.Empty, _accounts()));
            Assert.AreEqual(0, DraftResolver.MatchAccountId(null, _accounts()));
            Assert.AreEqual(0, DraftResolver.MatchAccountId("   ", _accounts()));
        }

        [Test]
        public void MatchAccountId_TrimsSurroundingSpace()
        {
            Assert.AreEqual(1, DraftResolver.MatchAccountId("  现金 ", _accounts()));
        }

        [Test]
        public void MatchAccountId_NullList_ReturnsZero()
        {
            Assert.AreEqual(0, DraftResolver.MatchAccountId("现金", null));
        }

        // ── 分类 ────────────────────────────────────

        [Test]
        public void MatchCategoryId_Found_ReturnsId()
        {
            Assert.AreEqual(11,
                DraftResolver.MatchCategoryId("餐饮", _categories(), TxType.Expense));
        }

        [Test]
        public void MatchCategoryId_WrongKind_ReturnsZero()
        {
            // 「工资」是收入分类。类型是支出时不认它——宁可留空，
            // 也不能把一个收入分类记到支出上
            Assert.AreEqual(0,
                DraftResolver.MatchCategoryId("工资", _categories(), TxType.Expense));

            Assert.AreEqual(12,
                DraftResolver.MatchCategoryId("工资", _categories(), TxType.Income));
        }

        [Test]
        public void MatchCategoryId_UnknownName_ReturnsZero()
        {
            Assert.AreEqual(0,
                DraftResolver.MatchCategoryId("不存在的东西", _categories(), TxType.Expense));
        }

        [Test]
        public void MatchCategoryId_EmptyName_ReturnsZero()
        {
            Assert.AreEqual(0, DraftResolver.MatchCategoryId(string.Empty, _categories(), TxType.Expense));
            Assert.AreEqual(0, DraftResolver.MatchCategoryId(null, _categories(), TxType.Expense));
        }

        // ── 方向换算 ────────────────────────────────

        [Test]
        public void CategoryKindFor_MapsIncomeAndEverythingElse()
        {
            Assert.AreEqual(CategoryKind.Income, DraftResolver.CategoryKindFor(TxType.Income));

            // 转账没有分类，落到支出方向只是给调用方一个确定值
            Assert.AreEqual(CategoryKind.Expense, DraftResolver.CategoryKindFor(TxType.Expense));
            Assert.AreEqual(CategoryKind.Expense, DraftResolver.CategoryKindFor(TxType.Transfer));
        }

        private static List<Account> _accounts()
        {
            return new List<Account>
            {
                new Account { Id = 1, Name = "现金" },
                new Account { Id = 2, Name = "微信" },
                new Account { Id = 3, Name = "支付宝" }
            };
        }

        private static List<Category> _categories()
        {
            return new List<Category>
            {
                new Category { Id = 11, Name = "餐饮", Kind = CategoryKind.Expense },
                new Category { Id = 12, Name = "工资", Kind = CategoryKind.Income }
            };
        }
    }
}
