using System.Collections.Generic;
using EasyMoney.App.UI;
using EasyMoney.App.UI.Reports;
using EasyMoney.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace EasyMoney.Tests
{
    /// <summary>
    /// 两种视图各自建出什么节点。
    ///
    /// 这些断言原先没有对应的测试——整段画法都长在 ReportPage 里，而页面跑不到
    /// EditMode。搬进 IReportView 之后，节点名就是它的形状，能直接被 Find 到。
    /// </summary>
    public class ReportViewRenderTests
    {
        private const float TOLERANCE = 0.001f;

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

        // ── 条形视图 ────────────────────────────────

        [Test]
        public void Bar_RendersOneRowPerCategory()
        {
            new BarReportView().Render(m_Content, _items(), TxType.Expense);

            Assert.IsNotNull(m_Content.Find("Item_1"));
            Assert.IsNotNull(m_Content.Find("Item_2"));
            Assert.IsNull(m_Content.Find("Item_3"), "只有两个分类，不该多出一行");
        }

        [Test]
        public void Bar_RowCarriesNameAndRatioText()
        {
            new BarReportView().Render(m_Content, _items(), TxType.Expense);

            Assert.AreEqual("餐饮", m_Content.Find("Item_1/LabelLine/Name").GetComponent<Text>().text);
            Assert.AreEqual(
                ReportForm.BreakdownValueText(Money.FromCents(3000), 0.6m),
                m_Content.Find("Item_1/LabelLine/Value").GetComponent<Text>().text);
        }

        [Test]
        public void Bar_HasBarTrackWithFillMatchingRatio()
        {
            new BarReportView().Render(m_Content, _items(), TxType.Expense);

            RectTransform oFill = m_Content.Find("Item_1/BarTrack/Fill").GetComponent<RectTransform>();

            // 占比靠填充层的锚点右边界表达：0 = 一点不画，1 = 铺满整条轨道
            Assert.AreEqual(0f, oFill.anchorMin.x, TOLERANCE);
            Assert.AreEqual(0.6f, oFill.anchorMax.x, TOLERANCE);
        }

        [Test]
        public void Bar_FillUsesExpenseColorForExpenseAndIncomeColorForIncome()
        {
            new BarReportView().Render(m_Content, _items(), TxType.Expense);
            _assertColor(Theme.EXPENSE, _fillColor(1), "支出构成用支出色");

            UiFactory.ClearChildren(m_Content);

            new BarReportView().Render(m_Content, _items(), TxType.Income);
            _assertColor(Theme.INCOME, _fillColor(1), "收入构成用收入色");
        }

        [Test]
        public void Bar_RowsAreTallerThanPlainRows()
        {
            // 带条形的行要留出条的位置，比不带的行高——这是两个视图共用一行代码时
            // 唯一按视图分叉的地方，写反了只会表现为「行挤在一起」
            new BarReportView().Render(m_Content, _items(), TxType.Expense);

            Assert.AreEqual(ReportViewParts.ROW_HEIGHT_WITH_BAR, _rowHeight(1));
            Assert.Greater(ReportViewParts.ROW_HEIGHT_WITH_BAR, ReportViewParts.ROW_HEIGHT_PLAIN);
        }

        [Test]
        public void Bar_EmptyItems_ShowsHintAndNoRows()
        {
            new BarReportView().Render(m_Content, new List<CategoryBreakdownItem>(), TxType.Expense);

            Assert.IsNotNull(m_Content.Find("Empty"));
            Assert.AreEqual(0, _countRows(), "没有记录时不该建出任何明细行");
        }

        // ── 环形视图 ────────────────────────────────

        [Test]
        public void Donut_DrawsRingWithGeneratedSprite()
        {
            new DonutReportView().Render(m_Content, _items(), TxType.Expense);

            Image oRing = _ring();
            Assert.IsNotNull(oRing.sprite, "环是一张贴图，没贴图就是一片白");
            Assert.IsNotNull(m_Content.Find("Donut/Ring"), "环该挂在撑高的容器里，才能正方形居中");
        }

        [Test]
        public void Donut_CenterShowsBreakdownTotal()
        {
            new DonutReportView().Render(m_Content, _items(), TxType.Expense);

            Assert.AreEqual("50.00", _centerValue().text,
                "环中心显示的是这一圈的合计（30.00 + 20.00），不是别的什么数");
            Assert.AreEqual(ReportForm.EXPENSE_TITLE, _centerCaption().text);
        }

        [Test]
        public void Donut_CenterCaptionFollowsBreakdownType()
        {
            new DonutReportView().Render(m_Content, _items(), TxType.Income);

            Assert.AreEqual(ReportForm.INCOME_TITLE, _centerCaption().text);
            _assertColor(Theme.INCOME, _centerValue().color, "收入构成的合计用收入色");
        }

        [Test]
        public void Donut_RowsHaveNoBar()
        {
            // 环本身就是占比的图形，行里再来一排条是重复的
            new DonutReportView().Render(m_Content, _items(), TxType.Expense);

            Assert.AreEqual(2, _countRows(), "明细还是要留着的，只是没有条形");
            Assert.IsNull(m_Content.Find("Item_1/BarTrack"), "环形视图下不该有轨道");
            Assert.AreEqual(ReportViewParts.ROW_HEIGHT_PLAIN, _rowHeight(1),
                "没有条形的行要矮一截，按带条的行高排下来底下会空一块");
        }

        [Test]
        public void Donut_EmptyItems_ShowsHintWithoutDrawingRing()
        {
            new DonutReportView().Render(m_Content, new List<CategoryBreakdownItem>(), TxType.Expense);

            Assert.IsNotNull(m_Content.Find("Empty"));
            Assert.IsNull(m_Content.Find("Donut"), "一分钱都没有时不该画出一个空环");
            Assert.AreEqual(0, _countRows());
        }

        [Test]
        public void Donut_RenderedTwice_ReplacesSprite()
        {
            // 这条看着平淡，其实钉的是贴图的释放路径：贴图是原生对象，重新生成前
            // 不 Destroy 就会一路泄漏；而要是用了 Object.Destroy，EditMode 下它会打一条
            // Error——测试框架把未声明的 Error 判为失败，所以这条跑绿本身就说明
            // 释放走的是 UiFactory.DestroyObject（见那里的说明）
            DonutReportView oView = new DonutReportView();

            oView.Render(m_Content, _items(), TxType.Expense);
            Sprite oFirst = _ring().sprite;

            UiFactory.ClearChildren(m_Content);
            oView.Render(m_Content, _items(), TxType.Expense);

            Assert.IsNotNull(_ring().sprite);
            Assert.AreNotSame(oFirst, _ring().sprite, "每次重画都该是一张新贴图");
        }

        // ── 辅助 ────────────────────────────────────

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

        private int _countRows()
        {
            int iCount = 0;
            foreach (Transform oChild in m_Content)
            {
                if (oChild.name.StartsWith("Item_"))
                {
                    iCount++;
                }
            }

            return iCount;
        }

        private float _rowHeight(int iIndex)
        {
            Transform oRow = m_Content.Find($"Item_{iIndex}");
            Assert.IsNotNull(oRow, $"找不到第 {iIndex} 行");
            return oRow.GetComponent<LayoutElement>().preferredHeight;
        }

        private Color _fillColor(int iIndex)
        {
            return m_Content.Find($"Item_{iIndex}/BarTrack/Fill").GetComponent<Image>().color;
        }

        private Image _ring()
        {
            Transform oRing = m_Content.Find("Donut/Ring");
            Assert.IsNotNull(oRing, "环形视图里找不到环");
            return oRing.GetComponent<Image>();
        }

        private Text _centerCaption()
        {
            Transform oNode = m_Content.Find("Donut/Ring/Center/Caption");
            Assert.IsNotNull(oNode, "环中心的上行文字不见了");
            return oNode.GetComponent<Text>();
        }

        private Text _centerValue()
        {
            Transform oNode = m_Content.Find("Donut/Ring/Center/Value");
            Assert.IsNotNull(oNode, "环中心的金额不见了");
            return oNode.GetComponent<Text>();
        }

        private static void _assertColor(Color oExpected, Color oActual, string sMessage)
        {
            Assert.AreEqual(oExpected.r, oActual.r, TOLERANCE, sMessage);
            Assert.AreEqual(oExpected.g, oActual.g, TOLERANCE, sMessage);
            Assert.AreEqual(oExpected.b, oActual.b, TOLERANCE, sMessage);
        }
    }
}
