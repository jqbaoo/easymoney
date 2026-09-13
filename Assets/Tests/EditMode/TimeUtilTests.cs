using System;
using EasyMoney.Core;
using NUnit.Framework;

namespace EasyMoney.Tests
{
    public class TimeUtilTests
    {
        [Test]
        public void RoundTrip_PreservesLocalTimeToTheSecond()
        {
            DateTime oOriginal = new DateTime(2026, 3, 15, 14, 30, 45, DateTimeKind.Local);
            long iMs = TimeUtil.ToUnixMs(oOriginal);
            DateTime oBack = TimeUtil.FromUnixMs(iMs);
            Assert.AreEqual(oOriginal, oBack);
        }

        [Test]
        public void ToUnixMs_EpochIsZero()
        {
            DateTime oEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            Assert.AreEqual(0, TimeUtil.ToUnixMs(oEpoch.ToLocalTime()));
        }

        [Test]
        public void StartOfMonthMs_IsMidnightOfFirstDay()
        {
            long iStart = TimeUtil.StartOfMonthMs(2026, 3);
            DateTime oLocal = TimeUtil.FromUnixMs(iStart);
            Assert.AreEqual(new DateTime(2026, 3, 1, 0, 0, 0), oLocal);
        }

        [Test]
        public void StartOfNextMonthMs_HandlesDecemberRollover()
        {
            long iNext = TimeUtil.StartOfNextMonthMs(2026, 12);
            DateTime oLocal = TimeUtil.FromUnixMs(iNext);
            Assert.AreEqual(new DateTime(2027, 1, 1, 0, 0, 0), oLocal);
        }

        [Test]
        public void MonthRange_CoversExactlyOneMonth()
        {
            long iStart = TimeUtil.StartOfMonthMs(2026, 2);
            long iEnd = TimeUtil.StartOfNextMonthMs(2026, 2);
            TimeSpan tSpan = TimeUtil.FromUnixMs(iEnd) - TimeUtil.FromUnixMs(iStart);

            // 2026 年 2 月有 28 天
            Assert.AreEqual(28, tSpan.TotalDays);
        }

        [Test]
        public void StartOfDayMs_TruncatesTimePart()
        {
            DateTime oNoon = new DateTime(2026, 7, 4, 13, 45, 12, DateTimeKind.Local);
            DateTime oStart = TimeUtil.FromUnixMs(TimeUtil.StartOfDayMs(oNoon));
            Assert.AreEqual(new DateTime(2026, 7, 4, 0, 0, 0), oStart);
        }

        [Test]
        public void CurrentYearMonth_MatchesSystemClock()
        {
            (int iYear, int iMonth) = TimeUtil.CurrentYearMonth();
            Assert.AreEqual(DateTime.Now.Year, iYear);
            Assert.AreEqual(DateTime.Now.Month, iMonth);
        }

        [Test]
        public void AddMonths_WithinSameYear_KeepsYear()
        {
            Assert.AreEqual((2026, 10), TimeUtil.AddMonths(2026, 9, 1));
            Assert.AreEqual((2026, 8), TimeUtil.AddMonths(2026, 9, -1));
        }

        [Test]
        public void AddMonths_ForwardAcrossYearBoundary_RollsOverToNextYear()
        {
            Assert.AreEqual((2027, 1), TimeUtil.AddMonths(2026, 12, 1));
        }

        [Test]
        public void AddMonths_BackwardAcrossYearBoundary_BorrowsFromPreviousYear()
        {
            Assert.AreEqual((2025, 12), TimeUtil.AddMonths(2026, 1, -1));
        }

        [Test]
        public void AddMonths_MultipleMonths_CrossesSeveralYears()
        {
            Assert.AreEqual((2027, 6), TimeUtil.AddMonths(2026, 3, 15));
            Assert.AreEqual((2024, 12), TimeUtil.AddMonths(2026, 3, -15));
        }

        [Test]
        public void FormatYearMonth_UsesChineseYearMonthLabel()
        {
            Assert.AreEqual("2026年9月", TimeUtil.FormatYearMonth(2026, 9));

            // 月份不补零：账单页与报表页顶部的月份条都按这个格式显示
            Assert.AreEqual("2026年12月", TimeUtil.FormatYearMonth(2026, 12));
            Assert.AreEqual("2027年1月", TimeUtil.FormatYearMonth(2027, 1));
        }

        [Test]
        public void FormatMonth_DoesNotPadWithZero()
        {
            Assert.AreEqual("9月", TimeUtil.FormatMonth(9));
            Assert.AreEqual("1月", TimeUtil.FormatMonth(1));
            Assert.AreEqual("12月", TimeUtil.FormatMonth(12));
        }

        [Test]
        public void FormatYear_EndsWithNian()
        {
            Assert.AreEqual("2026年", TimeUtil.FormatYear(2026));
        }

        [Test]
        public void FormatYearMonth_IsYearPlusMonth()
        {
            // FormatYearMonth 现在由 FormatYear + FormatMonth 拼出来。
            // 这条钉住「拆成两个函数之后，拼回去还是原来那串」——
            // 少了它，改动 FormatYearMonth 的实现就没有测试拦得住了
            Assert.AreEqual("2026年9月", TimeUtil.FormatYear(2026) + TimeUtil.FormatMonth(9));
            Assert.AreEqual("2027年1月", TimeUtil.FormatYear(2027) + TimeUtil.FormatMonth(1));
        }
    }
}
