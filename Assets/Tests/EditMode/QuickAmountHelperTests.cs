using System.Collections.Generic;
using EasyMoney.Core;
using NUnit.Framework;

namespace EasyMoney.Tests
{
    /// <summary>
    /// 快捷金额档位的生成规则。这是记账页「少打字」的关键：
    /// 档位要贴着用户平时的消费水平，但无历史、均值离谱时都不能生成
    /// 0 元或超上限这种按不下去的按钮。
    /// </summary>
    public class QuickAmountHelperTests
    {
        [Test]
        public void BuildQuickAmounts_ReturnsFourCandidates()
        {
            List<Money> lAmounts = QuickAmountHelper.BuildQuickAmounts(3000);
            Assert.AreEqual(4, lAmounts.Count);
        }

        [Test]
        public void BuildQuickAmounts_AreAscending()
        {
            List<Money> lAmounts = QuickAmountHelper.BuildQuickAmounts(3000);
            for (int i = 1; i < lAmounts.Count; i++)
            {
                Assert.Less(lAmounts[i - 1].Cents, lAmounts[i].Cents,
                    "快捷金额应当递增");
            }
        }

        [Test]
        public void BuildQuickAmounts_AllPositive()
        {
            foreach (Money oAmount in QuickAmountHelper.BuildQuickAmounts(0))
            {
                Assert.Greater(oAmount.Cents, 0, "即便是零均值也不应出现 0 元按钮");
            }
        }

        [Test]
        public void BuildQuickAmounts_FallsBackWhenNoHistory()
        {
            List<Money> lAmounts = QuickAmountHelper.BuildQuickAmounts(0);
            Assert.AreEqual(4, lAmounts.Count);
            Assert.AreEqual(1000, lAmounts[0].Cents, "无历史时第一个应为 10 元");
        }

        [Test]
        public void BuildQuickAmounts_CentersOnRecentAverage()
        {
            // 平均值 50 元时，候选里应当有 50 元这一档
            List<Money> lAmounts = QuickAmountHelper.BuildQuickAmounts(5000);
            bool bHasFifty = false;
            foreach (Money oAmount in lAmounts)
            {
                if (oAmount.Cents == 5000)
                {
                    bHasFifty = true;
                }
            }

            Assert.IsTrue(bHasFifty, "应当包含「最近均值」这一档");
        }

        [Test]
        public void BuildQuickAmounts_HandlesHugeAverageWithoutOverflow()
        {
            // 这条实际锁的是 _roundToNiceNumber 的「超出档位表就封顶」：
            // 正因为先封了顶，iBase * 4 才不可能溢出，_clamp 的上限分支走不到
            List<Money> lAmounts = QuickAmountHelper.BuildQuickAmounts(9_000_000_000L);
            foreach (Money oAmount in lAmounts)
            {
                Assert.Greater(oAmount.Cents, 0);
                Assert.LessOrEqual(oAmount.Cents, TransactionValidator.MaxAmountCents);
            }
        }
    }
}
