using System.Collections.Generic;

namespace EasyMoney.Core
{
    /// <summary>
    /// 生成记账页的快捷金额档位。以「最近同类消费均值」为基准上下各铺一档，
    /// 让常见金额一次点击就能填上，减少日常记账的打字量。
    ///
    /// 放 Core 是因为这套档位规则与界面无关，纯函数也好测。
    /// </summary>
    public static class QuickAmountHelper
    {
        /// <summary>
        /// 档位数量。界面照这个数一次把按钮建好，之后只改文案不重建，
        /// 所以 BuildQuickAmounts 的返回条数必须与它一致。
        /// </summary>
        public const int QUICK_AMOUNT_COUNT = 4;

        /// <summary>没有历史账单时的兜底档位（分）：10 / 30 / 50 / 100 元。</summary>
        private static readonly long[] FALLBACK_STEPS = { 1000, 3000, 5000, 10000 };

        /// <summary>
        /// 对齐用的「好记」档位（分）。基准值只会落到这些数上，
        /// 免得出现 4730 分这种用户看着别扭的按钮。
        /// </summary>
        private static readonly long[] NICE_STEPS =
        {
            100, 500, 1000, 2000, 5000, 10000, 20000, 50000, 100000
        };

        /// <summary>
        /// 返回 4 个递增的候选金额。均值非正（没有历史）时走兜底档位。
        /// </summary>
        public static List<Money> BuildQuickAmounts(long iRecentAverageCents)
        {
            List<Money> lResult = new List<Money>();

            if (iRecentAverageCents <= 0)
            {
                foreach (long iCents in FALLBACK_STEPS)
                {
                    lResult.Add(Money.FromCents(iCents));
                }

                return lResult;
            }

            long iBase = _roundToNiceNumber(iRecentAverageCents);

            lResult.Add(Money.FromCents(_clamp(iBase / 2)));
            lResult.Add(Money.FromCents(_clamp(iBase)));
            lResult.Add(Money.FromCents(_clamp(iBase * 2)));
            lResult.Add(Money.FromCents(_clamp(iBase * 4)));

            return lResult;
        }

        /// <summary>
        /// 向上对齐到最近的「好记」档位。超过最大档位时封顶，
        /// 这样 _clamp 面对任何 long 输入都不会溢出。
        /// </summary>
        private static long _roundToNiceNumber(long iCents)
        {
            foreach (long iStep in NICE_STEPS)
            {
                if (iCents <= iStep)
                {
                    return iStep;
                }
            }

            return NICE_STEPS[NICE_STEPS.Length - 1];
        }

        /// <summary>
        /// 夹到 [1, 单笔上限]。下限取 1 分而不是 0，避免生成按下去必然校验失败的按钮。
        ///
        /// 注意：受 NICE_STEPS 的取值范围（1 元 ~ 1000 元）约束，iBase / 2 恒 ≥ 50 分、
        /// iBase * 4 恒 ≤ 4000 元，两个分支目前都进不来——实测把它整个换成直通，
        /// 147 个用例依然全绿。保留是为了将来调档位表时仍有兜底，但别指望测试看住它。
        /// </summary>
        private static long _clamp(long iCents)
        {
            if (iCents < 1)
            {
                return 1;
            }

            if (iCents > TransactionValidator.MaxAmountCents)
            {
                return TransactionValidator.MaxAmountCents;
            }

            return iCents;
        }
    }
}
