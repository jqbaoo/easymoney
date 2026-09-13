using System.Collections.Generic;
using EasyMoney.App.UI;
using EasyMoney.App.UI.Reports;
using EasyMoney.Core;
using NUnit.Framework;
using UnityEngine;

namespace EasyMoney.Tests
{
    /// <summary>
    /// 报表视图宿主的测试：换视图、清场、分发。
    ///
    /// 「切了视图之后上一个视图的节点还留着」这种毛病不会报错，只会两个视图叠着画，
    /// 而它原先会写在报表页里——页面跑不到 EditMode，等于没人看着。
    /// </summary>
    public class ReportViewHostTests
    {
        private GameObject m_Root;
        private RectTransform m_Content;

        [SetUp]
        public void SetUp()
        {
            Theme.Apply(ThemePalette.Light());

            m_Root = new GameObject("TestRoot", typeof(RectTransform));
            RectTransform oParent = m_Root.GetComponent<RectTransform>();
            oParent.sizeDelta = new Vector2(Theme.REF_WIDTH, Theme.REF_HEIGHT);

            m_Content = UiFactory.CreateNode(oParent, "Content");
        }

        [TearDown]
        public void TearDown()
        {
            // EditMode 下 GameObject 不会随场景销毁，必须手动清
            Object.DestroyImmediate(m_Root);
        }

        // ── 视图台账 ────────────────────────────────

        [Test]
        public void Constructor_StartsAtDefaultMode()
        {
            ReportViewHost oHost = _createHost();

            Assert.AreEqual(ReportViews.Default, oHost.Mode);
        }

        [Test]
        public void EveryModeInCatalog_HasAView()
        {
            // 在 ReportViews.ALL 里挂了一项、却忘了写对应的视图实现时，
            // 下拉列表会多出一个点了没反应的选项——这条就是拦这个的
            ReportViewHost oHost = _createHost();

            foreach (ReportViewMode oMode in ReportViews.ALL)
            {
                Assert.IsTrue(oHost.Has(oMode),
                    $"下拉里有「{ReportViews.Title(oMode)}」，却没有对应的视图实现");
            }
        }

        [Test]
        public void SetMode_SwitchesAndReportsChange()
        {
            ReportViewHost oHost = _createHost();

            Assert.IsTrue(oHost.SetMode(ReportViewMode.Donut), "换了视图要告诉调用方，好让它重刷");
            Assert.AreEqual(ReportViewMode.Donut, oHost.Mode);
        }

        [Test]
        public void SetMode_SameMode_ReportsNoChange()
        {
            ReportViewHost oHost = _createHost();

            Assert.IsFalse(oHost.SetMode(ReportViews.Default),
                "选中的还是当前视图，不该让页面白刷一次");
        }

        [Test]
        public void SetMode_UnregisteredMode_IsRejected()
        {
            StubView oStub = new StubView { Mode = ReportViewMode.Bar };
            ReportViewHost oHost = new ReportViewHost(m_Content, oStub);

            Assert.IsFalse(oHost.SetMode(ReportViewMode.Donut), "没挂实现的视图不该切得过去");
            Assert.AreEqual(ReportViewMode.Bar, oHost.Mode, "被拒绝之后应当停在原来的视图上");
        }

        // ── 渲染与清场 ──────────────────────────────

        [Test]
        public void Render_DispatchesToCurrentView()
        {
            StubView oBar = new StubView { Mode = ReportViewMode.Bar };
            StubView oDonut = new StubView { Mode = ReportViewMode.Donut };
            ReportViewHost oHost = new ReportViewHost(m_Content, oBar, oDonut);

            oHost.Render(null, TxType.Expense);
            oHost.SetMode(ReportViewMode.Donut);
            oHost.Render(null, TxType.Expense);

            Assert.AreEqual(1, oBar.RenderCount, "条形视图该收到第一次渲染");
            Assert.AreEqual(1, oDonut.RenderCount, "环形视图该收到切换之后那次渲染");
        }

        [Test]
        public void Render_ClearsNodesLeftByThePreviousView()
        {
            ReportViewHost oHost = _createHost();

            oHost.Render(_items(), TxType.Expense);
            Assert.IsNotNull(m_Content.Find("Item_1/BarTrack"), "前提：条形视图确实建出了条形");

            oHost.SetMode(ReportViewMode.Donut);
            oHost.Render(_items(), TxType.Expense);

            Assert.IsNull(m_Content.Find("Item_1/BarTrack"),
                "换视图后上一个视图的节点必须清掉，否则两版会叠在一起");
            Assert.IsNotNull(m_Content.Find("Donut/Ring"), "新视图该画出来了");
        }

        [Test]
        public void Render_Twice_DoesNotStackRows()
        {
            // 切月份、切收支都会重走 Render——不清场的话每刷一次就多一批行
            ReportViewHost oHost = _createHost();

            oHost.Render(_items(), TxType.Expense);
            int iFirst = m_Content.childCount;

            oHost.Render(_items(), TxType.Expense);

            Assert.AreEqual(iFirst, m_Content.childCount, "重刷一次不该多出一批行");
        }

        [Test]
        public void Render_EmptyItems_ShowsHint()
        {
            ReportViewHost oHost = _createHost();

            oHost.Render(new List<CategoryBreakdownItem>(), TxType.Expense);

            Transform oEmpty = m_Content.Find("Empty");
            Assert.IsNotNull(oEmpty);
            Assert.AreEqual(ReportForm.EMPTY_HINT, oEmpty.GetComponent<UnityEngine.UI.Text>().text);
        }

        [Test]
        public void Render_EmptyItems_ShowsHintInEveryView()
        {
            // 空态是两个视图各自实现的，很容易只改一个——这条两种都走一遍
            foreach (ReportViewMode oMode in ReportViews.ALL)
            {
                ReportViewHost oHost = _createHost();
                oHost.SetMode(oMode);
                oHost.Render(new List<CategoryBreakdownItem>(), oMode == ReportViewMode.Donut
                    ? TxType.Income
                    : TxType.Expense);

                Assert.IsNotNull(m_Content.Find("Empty"),
                    $"{ReportViews.Title(oMode)} 视图下没有记录时该给出提示");
            }
        }

        // ── 辅助 ────────────────────────────────────

        private ReportViewHost _createHost()
        {
            return new ReportViewHost(m_Content, new BarReportView(), new DonutReportView());
        }

        private static List<CategoryBreakdownItem> _items()
        {
            return new List<CategoryBreakdownItem>
            {
                new CategoryBreakdownItem
                {
                    CategoryId = 1, CategoryName = "餐饮", Total = Money.FromCents(3000), Ratio = 0.6m
                },
                new CategoryBreakdownItem
                {
                    CategoryId = 2, CategoryName = "交通", Total = Money.FromCents(2000), Ratio = 0.4m
                }
            };
        }

        /// <summary>记账用的假视图：只数被渲染了几次，不建任何节点。</summary>
        private class StubView : IReportView
        {
            public ReportViewMode Mode { get; set; }

            public int RenderCount { get; private set; }

            public void Render(RectTransform oContent, IList<CategoryBreakdownItem> lItems, TxType oType)
            {
                RenderCount++;
            }
        }
    }
}
