using System.Collections.Generic;
using EasyMoney.Core;
using NUnit.Framework;

namespace EasyMoney.Tests
{
    /// <summary>
    /// 报表视图清单测试。下拉列表的选项就是 <see cref="ReportViews.ALL"/>，
    /// 台账和下标错一位，界面上就是「选了环形图却还是条形」这种说不清的毛病。
    /// </summary>
    public class ReportViewsTests
    {
        [Test]
        public void All_IsNotEmpty()
        {
            Assert.Greater(ReportViews.ALL.Length, 0, "报表至少得有一种视图");
        }

        [Test]
        public void All_HasNoDuplicate()
        {
            HashSet<ReportViewMode> setSeen = new HashSet<ReportViewMode>();
            foreach (ReportViewMode oMode in ReportViews.ALL)
            {
                Assert.IsTrue(setSeen.Add(oMode), $"{oMode} 在清单里出现了两次，下拉会多出一项");
            }
        }

        [Test]
        public void All_EveryModeHasItsOwnTitle()
        {
            // 两种视图的名字不能撞——撞了的话下拉里就是两个一样的选项
            HashSet<string> setTitles = new HashSet<string>();
            foreach (ReportViewMode oMode in ReportViews.ALL)
            {
                Assert.IsTrue(setTitles.Add(ReportViews.Title(oMode)),
                    $"{oMode} 的名字和别的视图重了");
                Assert.IsNotEmpty(ReportViews.Title(oMode));
            }
        }

        [Test]
        public void Title_BarAndDonut_AreDistinct()
        {
            Assert.AreEqual(ReportViews.BAR_TITLE, ReportViews.Title(ReportViewMode.Bar));
            Assert.AreEqual(ReportViews.DONUT_TITLE, ReportViews.Title(ReportViewMode.Donut));
        }

        [Test]
        public void Title_UnknownValue_FallsBackToBar()
        {
            // 枚举底层是 int，越界值是可能的；那时宁可显示成条形图，也别让下拉按钮空着
            Assert.AreEqual(ReportViews.BAR_TITLE, ReportViews.Title((ReportViewMode)99));
        }

        [Test]
        public void Default_IsInCatalog()
        {
            Assert.Contains(ReportViews.Default, ReportViews.ALL,
                "默认视图必须在下拉清单里，否则一进页面按钮上的字跟实际显示的对不上");
        }

        [Test]
        public void DefaultIndex_PointsAtDefault()
        {
            Assert.AreEqual(ReportViews.Default, ReportViews.ALL[ReportViews.DefaultIndex]);
        }

        [Test]
        public void IndexOf_RoundTripsEveryMode()
        {
            foreach (ReportViewMode oMode in ReportViews.ALL)
            {
                Assert.AreEqual(oMode, ReportViews.ALL[ReportViews.IndexOf(oMode)]);
            }
        }

        [Test]
        public void IndexOf_UnknownValue_ReturnsZero()
        {
            // 下拉组件不接受负下标，找不到就给 0
            Assert.AreEqual(0, ReportViews.IndexOf((ReportViewMode)99));
        }
    }
}
