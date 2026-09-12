using EasyMoney.Core;
using NUnit.Framework;

namespace EasyMoney.Tests
{
    /// <summary>
    /// 报表页展示规则测试。这些规则原先散在页面里，EditMode 根本跑不到；
    /// 抽出来之后重点盯三件事：标题随收支类型切换、占比保留一位小数、
    /// 条形宽度不会越出轨道（锚点越界不报错，只会画歪，肉眼未必看得出来）。
    /// </summary>
    public class ReportFormTests
    {
        // ── 构成标题 ────────────────────────────────

        [Test]
        public void BreakdownTitle_Expense_ShowsExpenseTitle()
        {
            Assert.AreEqual(ReportForm.EXPENSE_TITLE, ReportForm.BreakdownTitle(TxType.Expense));
        }

        [Test]
        public void BreakdownTitle_Income_ShowsIncomeTitle()
        {
            Assert.AreEqual(ReportForm.INCOME_TITLE, ReportForm.BreakdownTitle(TxType.Income));
        }

        [Test]
        public void BreakdownTitle_Transfer_FallsBackToExpense()
        {
            // 转账不进收支统计，正常切不到它；真传进来也得有标题，不能空着
            Assert.AreEqual(ReportForm.EXPENSE_TITLE, ReportForm.BreakdownTitle(TxType.Transfer));
        }

        [Test]
        public void BreakdownTitle_UnknownValue_FallsBackToExpense()
        {
            Assert.AreEqual(ReportForm.EXPENSE_TITLE, ReportForm.BreakdownTitle((TxType)99));
        }

        // ── 占比文案 ────────────────────────────────

        [Test]
        public void BreakdownValueText_FormatsAmountAndOneDecimalPercent()
        {
            string sText = ReportForm.BreakdownValueText(Money.FromCents(3550), 0.543m);

            Assert.AreEqual("35.50  (54.3%)", sText);
        }

        [Test]
        public void BreakdownValueText_Zero_ShowsZeroPercent()
        {
            Assert.AreEqual("0.00  (0.0%)", ReportForm.BreakdownValueText(Money.Zero, 0m));
        }

        [Test]
        public void BreakdownValueText_FullRatio_ShowsHundredPercent()
        {
            // 只有一个分类时占比就是 1，不能显示成 1000% 或 1%
            string sText = ReportForm.BreakdownValueText(Money.FromCents(800000), 1m);

            Assert.AreEqual("8000.00  (100.0%)", sText);
        }

        [Test]
        public void BreakdownValueText_KeepsTwoDecimalsOnAmount()
        {
            // 金额走 Money.ToString()，永远是两位小数——占比的精度不该影响它
            Assert.AreEqual("6.00  (2.6%)", ReportForm.BreakdownValueText(Money.FromCents(600), 0.026m));
        }

        // ── 条形宽度 ────────────────────────────────

        [Test]
        public void BarWidthRatio_KeepsValueInsideRange()
        {
            Assert.AreEqual(0.543m, ReportForm.BarWidthRatio(0.543m));
        }

        [Test]
        public void BarWidthRatio_Boundaries_AreUnchanged()
        {
            Assert.AreEqual(0m, ReportForm.BarWidthRatio(0m));
            Assert.AreEqual(1m, ReportForm.BarWidthRatio(1m));
        }

        [Test]
        public void BarWidthRatio_Negative_BecomesZero()
        {
            Assert.AreEqual(0m, ReportForm.BarWidthRatio(-0.2m));
        }

        [Test]
        public void BarWidthRatio_AboveOne_BecomesOne()
        {
            Assert.AreEqual(1m, ReportForm.BarWidthRatio(1.5m));
        }
    }
}
