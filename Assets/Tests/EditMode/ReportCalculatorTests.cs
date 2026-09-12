using System.Collections.Generic;
using EasyMoney.Core;
using NUnit.Framework;

namespace EasyMoney.Tests
{
    /// <summary>
    /// 报表计算测试。重点盯两件事：转账不能被算进收支（否则报表虚高），
    /// 以及分类被删后的历史账单不能算丢或崩掉。
    /// </summary>
    public class ReportCalculatorTests
    {
        private List<Category> m_Categories;

        [SetUp]
        public void SetUp()
        {
            m_Categories = new List<Category>
            {
                new Category { Id = 10, Name = "餐饮", Kind = CategoryKind.Expense },
                new Category { Id = 11, Name = "购物", Kind = CategoryKind.Expense },
                new Category { Id = 12, Name = "交通", Kind = CategoryKind.Expense },
                new Category { Id = 20, Name = "工资", Kind = CategoryKind.Income }
            };
        }

        private static Transaction _tx(TxType oType, decimal dYuan, int iCategoryId = 0)
        {
            return new Transaction
            {
                Type = oType,
                AmountCents = Money.FromYuan(dYuan).Cents,
                AccountId = 1,
                CategoryId = iCategoryId,
                OccurredAtMs = 1_700_000_000_000L
            };
        }

        [Test]
        public void BuildSummary_EmptyInput_ReturnsZeros()
        {
            PeriodSummary oSummary = ReportCalculator.BuildSummary(new List<Transaction>());

            Assert.AreEqual(0, oSummary.Income.Cents);
            Assert.AreEqual(0, oSummary.Expense.Cents);
            Assert.AreEqual(0, oSummary.Net.Cents);
            Assert.AreEqual(0, oSummary.TxCount);
        }

        [Test]
        public void BuildSummary_SumsIncomeAndExpenseSeparately()
        {
            List<Transaction> lTx = new List<Transaction>
            {
                _tx(TxType.Income, 8000m),
                _tx(TxType.Expense, 30m),
                _tx(TxType.Expense, 200m)
            };

            PeriodSummary oSummary = ReportCalculator.BuildSummary(lTx);

            Assert.AreEqual(800000, oSummary.Income.Cents);
            Assert.AreEqual(23000, oSummary.Expense.Cents);
            Assert.AreEqual(777000, oSummary.Net.Cents);
            Assert.AreEqual(3, oSummary.TxCount);
        }

        [Test]
        public void BuildSummary_IgnoresTransfers()
        {
            List<Transaction> lTx = new List<Transaction>
            {
                _tx(TxType.Expense, 100m),
                _tx(TxType.Transfer, 5000m)
            };

            PeriodSummary oSummary = ReportCalculator.BuildSummary(lTx);

            Assert.AreEqual(0, oSummary.Income.Cents, "转账不是收入");
            Assert.AreEqual(10000, oSummary.Expense.Cents, "转账不是支出");
            Assert.AreEqual(1, oSummary.TxCount, "转账不计入笔数");
        }

        [Test]
        public void BuildSummary_NetGoesNegativeWhenOverspending()
        {
            List<Transaction> lTx = new List<Transaction>
            {
                _tx(TxType.Income, 100m),
                _tx(TxType.Expense, 250m)
            };

            Assert.AreEqual(-15000, ReportCalculator.BuildSummary(lTx).Net.Cents);
        }

        [Test]
        public void BuildBreakdown_GroupsAndSortsByTotalDescending()
        {
            List<Transaction> lTx = new List<Transaction>
            {
                _tx(TxType.Expense, 30m, 10),
                _tx(TxType.Expense, 20m, 10),
                _tx(TxType.Expense, 300m, 11),
                _tx(TxType.Expense, 10m, 12)
            };

            List<CategoryBreakdownItem> lItems =
                ReportCalculator.BuildBreakdown(lTx, TxType.Expense, m_Categories);

            Assert.AreEqual(3, lItems.Count);
            Assert.AreEqual(11, lItems[0].CategoryId);
            Assert.AreEqual(30000, lItems[0].Total.Cents);
            Assert.AreEqual(1, lItems[0].TxCount);
            Assert.AreEqual(10, lItems[1].CategoryId);
            Assert.AreEqual(5000, lItems[1].Total.Cents);
            Assert.AreEqual(2, lItems[1].TxCount, "同分类两笔应合并计数");
            Assert.AreEqual(12, lItems[2].CategoryId);
        }

        [Test]
        public void BuildBreakdown_OnlyIncludesRequestedType()
        {
            List<Transaction> lTx = new List<Transaction>
            {
                _tx(TxType.Expense, 30m, 10),
                _tx(TxType.Income, 8000m, 20)
            };

            List<CategoryBreakdownItem> lExpense =
                ReportCalculator.BuildBreakdown(lTx, TxType.Expense, m_Categories);
            List<CategoryBreakdownItem> lIncome =
                ReportCalculator.BuildBreakdown(lTx, TxType.Income, m_Categories);

            Assert.AreEqual(1, lExpense.Count);
            Assert.AreEqual(10, lExpense[0].CategoryId);
            Assert.AreEqual(1, lIncome.Count);
            Assert.AreEqual(20, lIncome[0].CategoryId);
        }

        [Test]
        public void BuildBreakdown_ExcludesTransfers()
        {
            List<Transaction> lTx = new List<Transaction>
            {
                _tx(TxType.Expense, 30m, 10),
                _tx(TxType.Transfer, 5000m, 0)
            };

            Assert.AreEqual(1, ReportCalculator.BuildBreakdown(lTx, TxType.Expense, m_Categories).Count);
        }

        [Test]
        public void BuildBreakdown_UnknownCategoryFallsBackToPlaceholder()
        {
            List<Transaction> lTx = new List<Transaction> { _tx(TxType.Expense, 30m, 999) };

            List<CategoryBreakdownItem> lItems =
                ReportCalculator.BuildBreakdown(lTx, TxType.Expense, m_Categories);

            Assert.AreEqual(1, lItems.Count);
            Assert.AreEqual(999, lItems[0].CategoryId);
            Assert.IsNotEmpty(lItems[0].CategoryName);
            Assert.AreNotEqual("0", lItems[0].CategoryName);
        }

        [Test]
        public void BuildBreakdown_ZeroCategoryIdFallsBackToUncategorized()
        {
            List<Transaction> lTx = new List<Transaction> { _tx(TxType.Expense, 30m, 0) };

            List<CategoryBreakdownItem> lItems =
                ReportCalculator.BuildBreakdown(lTx, TxType.Expense, m_Categories);

            Assert.AreEqual(1, lItems.Count);
            Assert.AreEqual(0, lItems[0].CategoryId);
            Assert.IsNotEmpty(lItems[0].CategoryName);
        }

        [Test]
        public void AssignRatios_SumsToOne()
        {
            List<CategoryBreakdownItem> lItems = new List<CategoryBreakdownItem>
            {
                new CategoryBreakdownItem { CategoryId = 1, Total = Money.FromYuan(25m) },
                new CategoryBreakdownItem { CategoryId = 2, Total = Money.FromYuan(75m) }
            };

            ReportCalculator.AssignRatios(lItems);

            Assert.AreEqual(0.25m, lItems[0].Ratio);
            Assert.AreEqual(0.75m, lItems[1].Ratio);
            Assert.AreEqual(1.0m, lItems[0].Ratio + lItems[1].Ratio);
        }

        [Test]
        public void AssignRatios_HandlesZeroTotalWithoutDividingByZero()
        {
            List<CategoryBreakdownItem> lItems = new List<CategoryBreakdownItem>
            {
                new CategoryBreakdownItem { CategoryId = 1, Total = Money.Zero },
                new CategoryBreakdownItem { CategoryId = 2, Total = Money.Zero }
            };

            ReportCalculator.AssignRatios(lItems);

            Assert.AreEqual(0m, lItems[0].Ratio);
            Assert.AreEqual(0m, lItems[1].Ratio);
        }

        [Test]
        public void AssignRatios_EmptyList_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => ReportCalculator.AssignRatios(new List<CategoryBreakdownItem>()));
        }

        [Test]
        public void AssignRatios_HandlesNullList()
        {
            Assert.DoesNotThrow(() => ReportCalculator.AssignRatios(null));
        }
    }
}
