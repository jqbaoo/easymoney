using System.Collections.Generic;

namespace EasyMoney.Core
{
    /// <summary>
    /// 把分类占比切成环形图的扇区。纯函数，输入是 ReportCalculator.BuildBreakdown
    /// 的输出，既不碰数据库也不碰 UnityEngine，所以「这一圈到底怎么分的」能逐条断言。
    ///
    /// 唯一的硬要求是**闭合**：各片加起来必须正好是一整圈。占比是 decimal 除法算的，
    /// 累加到最后一位可能差那么几位——乘 360 之后小到 1e-25 度，肉眼当然看不见，
    /// 但画进贴图就是圆心到外缘的一道细缝，在环形图上很显眼。所以末片要收口。
    /// </summary>
    public static class DonutLayout
    {
        public const decimal FULL_CIRCLE_DEGREES = 360m;

        /// <summary>
        /// 收口时最多允许补多少度。
        ///
        /// 不是「无脑把末片拉到 360」：占比全为 0（总额算不出来时的兜底，见
        /// ReportCalculator.AssignRatios）时拉满，会把整圈染成最后一个分类的颜色——
        /// 那是在撒谎，明明一分钱都没有。所以只补舍入量级的缝；
        /// 差得超过这个数，说明数据本身有问题，宁可留空。
        /// </summary>
        public const decimal CLOSE_GAP_LIMIT_DEGREES = 1m;

        public static List<DonutSlice> Build(IList<CategoryBreakdownItem> lItems)
        {
            List<DonutSlice> lSlices = new List<DonutSlice>();

            if (lItems == null || lItems.Count == 0)
            {
                return lSlices;
            }

            decimal dCursor = 0m;

            for (int i = 0; i < lItems.Count; i++)
            {
                // 与条形图共用同一条钳位规则：比值坏掉时宁可这一片画不出来，
                // 也不能让它倒着走——角度一旦倒退，后面的片全会跟着错位
                decimal dSpan = ReportForm.BarWidthRatio(lItems[i].Ratio) * FULL_CIRCLE_DEGREES;

                // 总和超出一圈（只可能来自坏数据）时把角度截在 360 上：
                // 贴图那边按 atan2 取角，永远落在 [0, 360)，越界的片只会默默画不出来
                decimal dStart = dCursor > FULL_CIRCLE_DEGREES ? FULL_CIRCLE_DEGREES : dCursor;
                decimal dEnd = dStart + dSpan;
                if (dEnd > FULL_CIRCLE_DEGREES)
                {
                    dEnd = FULL_CIRCLE_DEGREES;
                }

                lSlices.Add(new DonutSlice
                {
                    StartDegrees = dStart,
                    EndDegrees = dEnd,
                    SeriesIndex = i
                });

                dCursor = dEnd;
            }

            _close(lSlices, dCursor);

            return lSlices;
        }

        private static void _close(List<DonutSlice> lSlices, decimal dCursor)
        {
            decimal dGap = FULL_CIRCLE_DEGREES - dCursor;

            // 补的是末片：列表按金额降序排过，最后一片是最小的那块，
            // 多摊给它不到 1 度，谁也看不出来
            if (dGap <= 0m || dGap > CLOSE_GAP_LIMIT_DEGREES)
            {
                return;
            }

            lSlices[lSlices.Count - 1].EndDegrees = FULL_CIRCLE_DEGREES;
        }
    }
}
