using System.Collections.Generic;
using EasyMoney.Core;
using NUnit.Framework;

namespace EasyMoney.Tests
{
    /// <summary>
    /// 环形图扇区切分测试。
    ///
    /// 这一块要盯的其实只有一件事：**这一圈合不拢**。占比是 decimal 除法算出来的，
    /// 累加到最后差几位、乘 360 之后就是圆心到外缘的一道细缝，肉眼一看就穿帮；
    /// 反过来，占比全是 0 的时候硬把末片铺满整圈，等于凭空画出一块不存在的支出。
    /// 两种错都长得「挺正常」，只有断言能分清。
    /// </summary>
    public class DonutLayoutTests
    {
        // ── 空输入 ──────────────────────────────────

        [Test]
        public void Build_Null_ReturnsEmpty()
        {
            Assert.AreEqual(0, DonutLayout.Build(null).Count);
        }

        [Test]
        public void Build_EmptyList_ReturnsEmpty()
        {
            Assert.AreEqual(0, DonutLayout.Build(new List<CategoryBreakdownItem>()).Count);
        }

        // ── 闭合 ────────────────────────────────────

        [Test]
        public void Build_SingleItemWithFullRatio_SweepsWholeCircle()
        {
            List<DonutSlice> lSlices = DonutLayout.Build(_items(1m));

            Assert.AreEqual(1, lSlices.Count);
            Assert.AreEqual(0m, lSlices[0].StartDegrees, "从 12 点方向起画");
            Assert.AreEqual(360m, lSlices[0].EndDegrees);
        }

        [Test]
        public void Build_RoundingError_ClosesLastSliceAtFullCircle()
        {
            // 三等分在 decimal 里除不尽。先把「确实除不尽」钉住——
            // 少了这一步，哪天 decimal 的行为变了（或者有人把比值改成能整除的），
            // 这条用例就变成空跑，看着还绿着
            decimal dRawSum = 0m;
            for (int i = 0; i < 3; i++)
            {
                dRawSum += 1m / 3m * 360m;
            }

            Assert.Less(dRawSum, 360m, "前提：这组比值累加起来确实比一整圈少一点");

            List<DonutSlice> lSlices = DonutLayout.Build(_items(1m / 3m, 1m / 3m, 1m / 3m));

            Assert.AreEqual(360m, lSlices[2].EndDegrees,
                "末片要收口到 360，否则环上会留一道细缝");
        }

        [Test]
        public void Build_ExactRatios_LastSliceEndsAtFullCircle()
        {
            List<DonutSlice> lSlices = DonutLayout.Build(_items(0.5m, 0.3m, 0.2m));

            Assert.AreEqual(360m, lSlices[2].EndDegrees);
        }

        [Test]
        public void Build_AllZeroRatios_DrawsNothing()
        {
            // 总额算不出来时 AssignRatios 会把比值全填 0。这时候把末片拉到 360
            // 等于凭空画出一整圈，而账上其实一分钱都没有
            List<DonutSlice> lSlices = DonutLayout.Build(_items(0m, 0m, 0m));

            foreach (DonutSlice oSlice in lSlices)
            {
                Assert.AreEqual(0m, oSlice.SpanDegrees, "没有任何金额时不该有扇区被画出来");
            }
        }

        [Test]
        public void Build_SumFarBelowFullCircle_LeavesGapOpen()
        {
            // 差得远说明数据本身有问题，这时候补满整圈同样是撒谎
            List<DonutSlice> lSlices = DonutLayout.Build(_items(0.1m, 0.1m, 0.1m));

            Assert.AreEqual(108m, lSlices[2].EndDegrees, "只补舍入量级的缝，不补真窟窿");
        }

        // ── 相邻片之间 ──────────────────────────────

        [Test]
        public void Build_SlicesAreContiguous()
        {
            List<DonutSlice> lSlices = DonutLayout.Build(_items(0.5m, 0.25m, 0.25m));

            Assert.AreEqual(0m, lSlices[0].StartDegrees);
            for (int i = 1; i < lSlices.Count; i++)
            {
                Assert.AreEqual(lSlices[i - 1].EndDegrees, lSlices[i].StartDegrees,
                    "上一片的终点就是下一片的起点，中间不能空出角度");
            }
        }

        [Test]
        public void Build_SpanFollowsRatio()
        {
            List<DonutSlice> lSlices = DonutLayout.Build(_items(0.5m, 0.25m, 0.25m));

            Assert.AreEqual(180m, lSlices[0].SpanDegrees);
            Assert.AreEqual(90m, lSlices[1].SpanDegrees);
            Assert.AreEqual(90m, lSlices[2].SpanDegrees);
        }

        // ── 坏数据 ──────────────────────────────────

        [Test]
        public void Build_NegativeRatio_BecomesZeroSpanWithoutGoingBackwards()
        {
            List<DonutSlice> lSlices = DonutLayout.Build(_items(-0.5m, 1.5m));

            Assert.AreEqual(0m, lSlices[0].SpanDegrees, "负比值钳成 0，不能倒着画");
            Assert.AreEqual(lSlices[0].EndDegrees, lSlices[1].StartDegrees);
            Assert.AreEqual(360m, lSlices[1].EndDegrees, "超出的比值钳在 360 上");
        }

        [Test]
        public void Build_SumOverFullCircle_NeverExceedsFullCircle()
        {
            List<DonutSlice> lSlices = DonutLayout.Build(_items(0.8m, 0.8m));

            foreach (DonutSlice oSlice in lSlices)
            {
                Assert.GreaterOrEqual(oSlice.StartDegrees, 0m);
                Assert.LessOrEqual(oSlice.EndDegrees, 360m, "角度越界时贴图上那片会整块画不出来");
            }
        }

        [Test]
        public void Build_TinyRatio_KeepsPositiveSpan()
        {
            // 万分之一的占比画出来不到 0.04 度，屏幕上几乎看不见，
            // 但它是「有」而不是「没有」——不能变成 0
            List<DonutSlice> lSlices = DonutLayout.Build(_items(0.9999m, 0.0001m));

            Assert.Greater(lSlices[1].SpanDegrees, 0m);
        }

        [Test]
        public void Build_AnglesNeverGoBackwards()
        {
            List<DonutSlice> lSlices = DonutLayout.Build(_items(0.4m, 0.3m, 0.2m, 0.1m));

            decimal dPreviousEnd = 0m;
            foreach (DonutSlice oSlice in lSlices)
            {
                Assert.GreaterOrEqual(oSlice.StartDegrees, dPreviousEnd);
                Assert.GreaterOrEqual(oSlice.EndDegrees, oSlice.StartDegrees);
                dPreviousEnd = oSlice.EndDegrees;
            }
        }

        // ── 色板槽位 ────────────────────────────────

        [Test]
        public void Build_SeriesIndexFollowsItemOrder()
        {
            // 槽位就是明细列表里的行号，这样「第 3 行」和「第 3 块」颜色永远对得上
            List<DonutSlice> lSlices = DonutLayout.Build(_items(0.5m, 0.3m, 0.2m));

            for (int i = 0; i < lSlices.Count; i++)
            {
                Assert.AreEqual(i, lSlices[i].SeriesIndex);
            }
        }

        // ── 辅助 ────────────────────────────────────

        private static List<CategoryBreakdownItem> _items(params decimal[] lRatios)
        {
            List<CategoryBreakdownItem> lItems = new List<CategoryBreakdownItem>();
            for (int i = 0; i < lRatios.Length; i++)
            {
                lItems.Add(new CategoryBreakdownItem
                {
                    CategoryId = i + 1,
                    CategoryName = $"分类{i + 1}",
                    Ratio = lRatios[i]
                });
            }

            return lItems;
        }
    }
}
