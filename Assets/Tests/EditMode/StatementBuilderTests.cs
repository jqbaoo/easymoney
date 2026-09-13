using System;
using System.Collections.Generic;
using EasyMoney.Core;
using NUnit.Framework;

namespace EasyMoney.Tests
{
    /// <summary>
    /// 账单列表的展示投影测试。重点盯三件事：按「本地日期」分组（不是 UTC 日期）、
    /// 金额的正负号规则（转账不带号）、以及分类/账户被删后的兜底文案。
    /// 这三样以前都藏在页面里，EditMode 跑不到，只能靠肉眼。
    /// </summary>
    public class StatementBuilderTests
    {
        private List<Category> m_Categories;
        private List<Account> m_Accounts;

        [SetUp]
        public void SetUp()
        {
            // 只有「餐饮」配了图标名：预置分类都有图标，用户自建分类没有——
            // 「购物」「工资」保持空图标名，专门用来盯住留空位那条路
            m_Categories = new List<Category>
            {
                new Category { Id = 10, Name = "餐饮", Kind = CategoryKind.Expense, IconName = "cat_food" },
                new Category { Id = 11, Name = "购物", Kind = CategoryKind.Expense },
                new Category { Id = 20, Name = "工资", Kind = CategoryKind.Income }
            };

            m_Accounts = new List<Account>
            {
                new Account { Id = 1, Name = "现金" },
                new Account { Id = 2, Name = "支付宝" }
            };
        }

        private static Transaction _tx(
            TxType oType, decimal dYuan, int iCategoryId = 0, int iAccountId = 1,
            string sNote = "", int iToAccountId = 0)
        {
            return new Transaction
            {
                Type = oType,
                AmountCents = Money.FromYuan(dYuan).Cents,
                CategoryId = iCategoryId,
                AccountId = iAccountId,
                ToAccountId = iToAccountId,
                Note = sNote,
                OccurredAtMs = TimeUtil.ToUnixMs(new DateTime(2026, 9, 11, 12, 0, 0, DateTimeKind.Local))
            };
        }

        // ── 行文案 ──────────────────────────────────

        [Test]
        public void BuildRow_Expense_ShowsNoteCategoryAndAccount()
        {
            StatementRow oRow = StatementBuilder.BuildRow(
                _tx(TxType.Expense, 35.50m, 10, 1, "午饭"), m_Categories, m_Accounts);

            Assert.AreEqual("午饭", oRow.Title);
            Assert.AreEqual("餐饮 · 现金", oRow.Subtitle);
            Assert.AreEqual("-35.50", oRow.AmountText);
            Assert.AreEqual(TxType.Expense, oRow.Type);
        }

        [Test]
        public void BuildRow_BlankNote_FallsBackToCategoryName()
        {
            StatementRow oRow = StatementBuilder.BuildRow(
                _tx(TxType.Expense, 35.50m, 10, 1, string.Empty), m_Categories, m_Accounts);

            Assert.AreEqual("餐饮", oRow.Title);
        }

        [Test]
        public void BuildRow_WhitespaceNote_FallsBackToCategoryName()
        {
            StatementRow oRow = StatementBuilder.BuildRow(
                _tx(TxType.Expense, 35.50m, 10, 1, "   "), m_Categories, m_Accounts);

            Assert.AreEqual("餐饮", oRow.Title);
        }

        [Test]
        public void BuildRow_EmojiNote_KeepsItWholeAsTitle()
        {
            // 真机验收时 emoji 备注显示成空白（android-release-checklist.md #16）。
            // 展示投影这一层必须证明自己是干净的，否则「空白」到底是这里吃掉了字符
            // 还是字体画不出来，就分不清了。emoji 是代理对（U+1F35C 占两个 char），
            // 长度断言顺带盯住「被按 char 截断」这类事故。
            StatementRow oRow = StatementBuilder.BuildRow(
                _tx(TxType.Expense, 35.50m, 10, 1, "和朋友吃饭🍜"), m_Categories, m_Accounts);

            Assert.AreEqual("和朋友吃饭🍜", oRow.Title);
            Assert.AreEqual("和朋友吃饭🍜".Length, oRow.Title.Length);
        }

        [Test]
        public void BuildRow_Income_PrefixesPlusSign()
        {
            StatementRow oRow = StatementBuilder.BuildRow(
                _tx(TxType.Income, 8000m, 20, 2), m_Categories, m_Accounts);

            Assert.AreEqual("+8000.00", oRow.AmountText);
            Assert.AreEqual("工资 · 支付宝", oRow.Subtitle);
        }

        [Test]
        public void BuildRow_Transfer_ShowsBothAccountsAndNoSign()
        {
            StatementRow oRow = StatementBuilder.BuildRow(
                _tx(TxType.Transfer, 200m, 0, 1, string.Empty, 2), m_Categories, m_Accounts);

            Assert.AreEqual("转账", oRow.Title);
            Assert.AreEqual("现金 → 支付宝", oRow.Subtitle);
            Assert.AreEqual("200.00", oRow.AmountText);
            Assert.AreEqual(TxType.Transfer, oRow.Type);
        }

        [Test]
        public void BuildRow_TransferWithNote_KeepsNoteAsTitle()
        {
            StatementRow oRow = StatementBuilder.BuildRow(
                _tx(TxType.Transfer, 200m, 0, 1, "还钱", 2), m_Categories, m_Accounts);

            Assert.AreEqual("还钱", oRow.Title);
            Assert.AreEqual("现金 → 支付宝", oRow.Subtitle);
        }

        [Test]
        public void BuildRow_MissingCategory_FallsBackToUncategorized()
        {
            StatementRow oRow = StatementBuilder.BuildRow(
                _tx(TxType.Expense, 12m, 999, 1), m_Categories, m_Accounts);

            Assert.AreEqual("未分类", oRow.Title);
            Assert.AreEqual("未分类 · 现金", oRow.Subtitle);
        }

        [Test]
        public void BuildRow_MissingAccount_FallsBackToUnknownAccount()
        {
            StatementRow oRow = StatementBuilder.BuildRow(
                _tx(TxType.Transfer, 200m, 0, 1, string.Empty, 777), m_Categories, m_Accounts);

            Assert.AreEqual("现金 → 未知账户", oRow.Subtitle);
        }

        [Test]
        public void BuildRow_NullLookupLists_DoesNotThrow()
        {
            StatementRow oRow = StatementBuilder.BuildRow(
                _tx(TxType.Expense, 12m, 10, 1), null, null);

            Assert.AreEqual("未分类", oRow.Title);
            Assert.AreEqual("未分类 · 未知账户", oRow.Subtitle);
        }

        [Test]
        public void BuildRow_KeepsTransactionReference()
        {
            Transaction oTransaction = _tx(TxType.Expense, 12m, 10, 1);
            oTransaction.Id = 42;

            StatementRow oRow = StatementBuilder.BuildRow(oTransaction, m_Categories, m_Accounts);

            Assert.AreSame(oTransaction, oRow.Transaction);
            Assert.AreEqual(42, oRow.Transaction.Id);
        }

        // ── 分类图标 ────────────────────────────────

        [Test]
        public void BuildRow_CategoryWithIcon_FillsCategoryIconName()
        {
            StatementRow oRow = StatementBuilder.BuildRow(
                _tx(TxType.Expense, 35.50m, 10, 1, "午饭"), m_Categories, m_Accounts);

            Assert.AreEqual("cat_food", oRow.CategoryIconName);
        }

        [Test]
        public void BuildRow_CategoryWithoutIcon_HasEmptyCategoryIconName()
        {
            // 用户自建分类没有图标名。界面据此留一个等宽空位，
            // 而不是让这行的文字往左顶——否则各行左边缘参差不齐。
            // 备注留空，好让 Title 落到分类名上：顺带证明分类是找到了的，只是没图标
            StatementRow oRow = StatementBuilder.BuildRow(
                _tx(TxType.Expense, 12m, 11, 1), m_Categories, m_Accounts);

            Assert.AreEqual("购物", oRow.Title);
            Assert.AreEqual(string.Empty, oRow.CategoryIconName);
        }

        [Test]
        public void BuildRow_Transfer_HasNoCategoryIcon()
        {
            // 转账的 CategoryId 是 0，找分类天然落空。不能因为转账在主标题上
            // 有「转账」这个兜底文案，就顺手也给它的图标位塞点什么
            StatementRow oRow = StatementBuilder.BuildRow(
                _tx(TxType.Transfer, 200m, 0, 1, string.Empty, 2), m_Categories, m_Accounts);

            Assert.AreEqual(string.Empty, oRow.CategoryIconName);
        }

        [Test]
        public void BuildRow_DeletedCategory_HasEmptyCategoryIconName()
        {
            // 分类被删后历史账单照样要显示：名称走「未分类」兜底，图标位留空
            StatementRow oRow = StatementBuilder.BuildRow(
                _tx(TxType.Expense, 12m, 999, 1), m_Categories, m_Accounts);

            Assert.AreEqual("未分类", oRow.Title);
            Assert.AreEqual(string.Empty, oRow.CategoryIconName);
        }

        [Test]
        public void BuildDays_FillsCategoryIconNameFromLookupTable()
        {
            // 分类表在 BuildDays 里只是往下传，中间漏一层的话单测 BuildRow 全绿，
            // 而界面上一整列空位——这条盯的就是那个缝
            List<StatementDay> lDays = StatementBuilder.BuildDays(
                new List<Transaction> { _tx(TxType.Expense, 35.50m, 10, 1, "午饭") },
                m_Categories, m_Accounts);

            Assert.AreEqual("cat_food", lDays[0].Items[0].CategoryIconName);
        }

        // ── 按天分组 ────────────────────────────────

        [Test]
        public void BuildDays_SameLocalDay_LandsInOneGroup()
        {
            List<Transaction> lTx = new List<Transaction>
            {
                _tx(TxType.Expense, 35.50m, 10, 1, "午饭"),
                _tx(TxType.Expense, 6m, 10, 1, "地铁")
            };

            List<StatementDay> lDays = StatementBuilder.BuildDays(lTx, m_Categories, m_Accounts);

            Assert.AreEqual(1, lDays.Count);
            Assert.AreEqual(2, lDays[0].Items.Count);
            Assert.AreEqual("午饭", lDays[0].Items[0].Title);
            Assert.AreEqual("地铁", lDays[0].Items[1].Title);
        }

        [Test]
        public void BuildDays_DifferentDays_SplitIntoGroups()
        {
            Transaction oToday = _tx(TxType.Expense, 35.50m, 10, 1);
            Transaction oYesterday = _tx(TxType.Expense, 6m, 10, 1);
            oYesterday.OccurredAtMs = oToday.OccurredAtMs - 86_400_000L;

            List<StatementDay> lDays = StatementBuilder.BuildDays(
                new List<Transaction> { oToday, oYesterday }, m_Categories, m_Accounts);

            Assert.AreEqual(2, lDays.Count);
            Assert.AreEqual(1, lDays[0].Items.Count);
            Assert.AreEqual(1, lDays[1].Items.Count);
            Assert.AreEqual("9月11日 周五", lDays[0].DateLabel);
            Assert.AreEqual("9月10日 周四", lDays[1].DateLabel);
        }

        [Test]
        public void BuildDays_GroupsByLocalDateNotUtcDate()
        {
            // 本地 9 月 11 日早上 7 点，在 UTC+8 已经是 UTC 的 9 月 10 日 23 点。
            // 用 UTC 日期分组的话这会掉进「9月10日」那一组。
            Transaction oEarlyMorning = _tx(TxType.Expense, 6m, 10, 1);
            oEarlyMorning.OccurredAtMs =
                TimeUtil.ToUnixMs(new DateTime(2026, 9, 11, 7, 0, 0, DateTimeKind.Local));

            List<StatementDay> lDays = StatementBuilder.BuildDays(
                new List<Transaction> { oEarlyMorning }, m_Categories, m_Accounts);

            Assert.AreEqual(1, lDays.Count);
            Assert.AreEqual(new DateTime(2026, 9, 11), lDays[0].Date);
            Assert.AreEqual("9月11日 周五", lDays[0].DateLabel);
        }

        [Test]
        public void BuildDays_KeepsInputOrder()
        {
            Transaction oNewest = _tx(TxType.Expense, 1m, 10, 1);
            Transaction oMiddle = _tx(TxType.Expense, 2m, 10, 1);
            Transaction oOldest = _tx(TxType.Expense, 3m, 10, 1);
            oMiddle.OccurredAtMs = oNewest.OccurredAtMs - 86_400_000L;
            oOldest.OccurredAtMs = oNewest.OccurredAtMs - 2 * 86_400_000L;

            List<StatementDay> lDays = StatementBuilder.BuildDays(
                new List<Transaction> { oNewest, oMiddle, oOldest }, m_Categories, m_Accounts);

            Assert.AreEqual("9月11日 周五", lDays[0].DateLabel);
            Assert.AreEqual("9月10日 周四", lDays[1].DateLabel);
            Assert.AreEqual("9月9日 周三", lDays[2].DateLabel);
        }

        [Test]
        public void BuildDays_SameDayOnBothSidesOfAGroup_IsNotMerged()
        {
            // 输入已按时间倒序时不会出现「同一天被隔开」，但真出现了也不能把两段
            // 静默合成一组——那会让界面上的顺序和查询结果对不上
            Transaction oSep11 = _tx(TxType.Expense, 1m, 10, 1);
            Transaction oSep10 = _tx(TxType.Expense, 2m, 10, 1);
            oSep10.OccurredAtMs = oSep11.OccurredAtMs - 86_400_000L;
            Transaction oSep11Again = _tx(TxType.Expense, 3m, 10, 1);

            List<StatementDay> lDays = StatementBuilder.BuildDays(
                new List<Transaction> { oSep11, oSep10, oSep11Again }, m_Categories, m_Accounts);

            Assert.AreEqual(3, lDays.Count);
        }

        [Test]
        public void BuildDays_EmptyInput_ReturnsEmptyList()
        {
            List<StatementDay> lDays = StatementBuilder.BuildDays(
                new List<Transaction>(), m_Categories, m_Accounts);

            Assert.AreEqual(0, lDays.Count);
        }

        [Test]
        public void BuildDays_NullInput_ReturnsEmptyList()
        {
            List<StatementDay> lDays = StatementBuilder.BuildDays(null, m_Categories, m_Accounts);

            Assert.AreEqual(0, lDays.Count);
        }

        [Test]
        public void BuildDays_FillsRowContentFromLookupTables()
        {
            List<Transaction> lTx = new List<Transaction>
            {
                _tx(TxType.Income, 8000m, 20, 2, "八月工资")
            };

            List<StatementDay> lDays = StatementBuilder.BuildDays(lTx, m_Categories, m_Accounts);

            Assert.AreEqual("八月工资", lDays[0].Items[0].Title);
            Assert.AreEqual("工资 · 支付宝", lDays[0].Items[0].Subtitle);
            Assert.AreEqual("+8000.00", lDays[0].Items[0].AmountText);
        }

        // ── 日期标题 ────────────────────────────────

        [Test]
        public void FormatDayLabel_IncludesMonthDayAndWeekday()
        {
            Assert.AreEqual("9月11日 周五", StatementBuilder.FormatDayLabel(new DateTime(2026, 9, 11)));
            Assert.AreEqual("9月13日 周日", StatementBuilder.FormatDayLabel(new DateTime(2026, 9, 13)));
        }

        [Test]
        public void FormatDayLabel_SingleDigitMonth_IsNotPadded()
        {
            Assert.AreEqual("1月4日 周日", StatementBuilder.FormatDayLabel(new DateTime(2026, 1, 4)));
        }
    }
}
