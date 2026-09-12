using System;

namespace EasyMoney.Core
{
    /// <summary>
    /// 时间工具。全项目统一用 Unix 毫秒（UTC）在数据库与各层之间传递时间，
    /// 只有展示和「按月/按日分组」时才转成本地时间。
    /// </summary>
    public static class TimeUtil
    {
        private static readonly DateTime EPOCH_UTC =
            new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static long ToUnixMs(DateTime oLocalTime)
        {
            DateTime oUtc = oLocalTime.Kind == DateTimeKind.Utc
                ? oLocalTime
                : oLocalTime.ToUniversalTime();

            return (long)(oUtc - EPOCH_UTC).TotalMilliseconds;
        }

        public static DateTime FromUnixMs(long iUnixMs)
        {
            return EPOCH_UTC.AddMilliseconds(iUnixMs).ToLocalTime();
        }

        public static long StartOfDayMs(DateTime oLocalTime)
        {
            DateTime oDate = oLocalTime.Date;
            DateTime oWithKind = DateTime.SpecifyKind(oDate, DateTimeKind.Local);
            return ToUnixMs(oWithKind);
        }

        public static long StartOfMonthMs(int iYear, int iMonth)
        {
            DateTime oFirstDay = new DateTime(iYear, iMonth, 1, 0, 0, 0, DateTimeKind.Local);
            return ToUnixMs(oFirstDay);
        }

        public static long StartOfNextMonthMs(int iYear, int iMonth)
        {
            DateTime oFirstDay = new DateTime(iYear, iMonth, 1, 0, 0, 0, DateTimeKind.Local);
            return ToUnixMs(oFirstDay.AddMonths(1));
        }

        public static (int Year, int Month) CurrentYearMonth()
        {
            DateTime oNow = DateTime.Now;
            return (oNow.Year, oNow.Month);
        }

        /// <summary>
        /// 年月标题，形如「2026年9月」。账单页与报表页顶部的月份条共用，
        /// 免得两处各写一遍、慢慢长得不一样。
        /// </summary>
        public static string FormatYearMonth(int iYear, int iMonth)
        {
            return $"{iYear}年{iMonth}月";
        }

        /// <summary>
        /// 年月加减，跨年自动进位 / 借位（2026-12 加 1 个月得 2027-1，2026-1 减 1 得 2025-12）。
        /// 「上个月 / 下个月」按钮用它，免得页面里散落一堆 DateTime 构造和进位判断。
        /// </summary>
        public static (int Year, int Month) AddMonths(int iYear, int iMonth, int iDelta)
        {
            // 日固定为 1 再借 DateTime 的进位：用 31 号会撞上「1月31日加一个月 = 3月3日」
            // 这种日期溢出，而调用方要的只是年月
            DateTime oResult = new DateTime(iYear, iMonth, 1).AddMonths(iDelta);
            return (oResult.Year, oResult.Month);
        }
    }
}
