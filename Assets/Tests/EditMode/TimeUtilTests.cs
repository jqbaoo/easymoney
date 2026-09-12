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
    }
}
