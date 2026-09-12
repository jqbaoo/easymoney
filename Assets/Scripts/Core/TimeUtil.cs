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
    }
}
