using EasyMoney.Core;
using UnityEngine;
using UnityEngine.UI;

namespace EasyMoney.App.UI.Reports
{
    /// <summary>
    /// 各种视图共用的零件：一行分类明细，和「这个月还没有记录」那句提示。
    ///
    /// 明细行原先整段写在 ReportPage 里，而现在两个视图都要它——再抄一份的话，
    /// 改个行高、换个兜底文案就得记着改两处，漏一处从代码上看不出来。
    /// </summary>
    public static class ReportViewParts
    {
        /// <summary>带条形的行高，沿用报表页原来的值。</summary>
        public const float ROW_HEIGHT_WITH_BAR = Theme.ROW_HEIGHT;

        /// <summary>
        /// 不带条形时行要矮一截。竖排布局按 112 排下来，行里只剩一行文字时
        /// 底下会空出一块，一行挨一行看着很散。
        /// </summary>
        public const float ROW_HEIGHT_PLAIN = 88f;

        private const float LABEL_LINE_HEIGHT = 46f;
        private const float VALUE_WIDTH = 280f;
        private const float EMPTY_HINT_HEIGHT = 200f;

        /// <summary>
        /// 加一行明细：色点 + 图标 + 分类名 + 金额(占比)。返回这一行，
        /// 条形视图在它下面再接一根条。
        /// </summary>
        /// <param name="iSeriesIndex">
        /// 这一行在列表里的下标，用来取图例色点的颜色；传负数表示这种视图不需要图例。
        /// </param>
        public static RectTransform AddRow(
            RectTransform oContent, CategoryBreakdownItem oItem, bool bWithBar, int iSeriesIndex)
        {
            // 这一行要竖排（上面文字、下面条形），所以不能用 CreateRow——
            // 它自带 HorizontalLayoutGroup，再叠加 VerticalLayoutGroup 会打架
            RectTransform oRow = UiFactory.CreateNode(oContent, $"Item_{oItem.CategoryId}");
            UiFactory.SetHeight(oRow, bWithBar ? ROW_HEIGHT_WITH_BAR : ROW_HEIGHT_PLAIN);

            UiFactory.PaintCard(oRow);

            VerticalLayoutGroup oLayout = oRow.gameObject.AddComponent<VerticalLayoutGroup>();
            oLayout.childControlWidth = true;
            oLayout.childControlHeight = true;
            oLayout.childForceExpandWidth = true;
            oLayout.childForceExpandHeight = false;

            // 没有条形时行里只剩一行文字，靠中缝对齐才不会贴在顶上
            oLayout.childAlignment = bWithBar ? TextAnchor.UpperCenter : TextAnchor.MiddleCenter;
            oLayout.padding = new RectOffset(
                (int)Theme.CARD_PADDING, (int)Theme.CARD_PADDING, 14, 14);

            RectTransform oLabelLine = UiFactory.CreateNode(oRow, "LabelLine");
            UiFactory.SetHeight(oLabelLine, LABEL_LINE_HEIGHT);

            HorizontalLayoutGroup oLabelLayout = oLabelLine.gameObject.AddComponent<HorizontalLayoutGroup>();
            oLabelLayout.childControlWidth = true;
            oLabelLayout.childControlHeight = true;
            oLabelLayout.childForceExpandWidth = false;
            oLabelLayout.childForceExpandHeight = true;
            oLabelLayout.spacing = Theme.CATEGORY_ICON_GAP;

            // 色点排在最左，在图标之前：环上没有文字，颜色与分类的对应全靠它，
            // 摆在行首扫一列下来就是一张图例
            if (iSeriesIndex >= 0)
            {
                _addLegendDot(oLabelLine, iSeriesIndex);
            }

            // 与账单列表同一个道理：分类被删或没配图标时留透明空位，名字的左边仍然对齐
            UiFactory.CreateIconSlot(oLabelLine, "Icon",
                IconNames.ForCategory(oItem.IconName),
                Theme.CATEGORY_ICON_SIZE, Theme.TEXT_WEAK);

            Text oName = UiFactory.CreateText(oLabelLine, "Name", oItem.CategoryName,
                Theme.FONT_BODY, TextAnchor.MiddleLeft);
            UiFactory.SetFlexible(oName.rectTransform);

            Text oValue = UiFactory.CreateText(oLabelLine, "Value",
                ReportForm.BreakdownValueText(oItem.Total, oItem.Ratio),
                Theme.FONT_CAPTION, TextAnchor.MiddleRight, Theme.TEXT_WEAK);
            UiFactory.SetWidth(oValue.rectTransform, VALUE_WIDTH);

            return oRow;
        }

        /// <summary>
        /// 图例色点。颜色按**行号**取槽位，与扇区同一套（<see cref="DonutLayout"/> 里
        /// `SeriesIndex = i`）——跟 CategoryId 走的话，中间删掉一个分类，颜色就整体错位了，
        /// 而环和行都还在、只是对不上，从界面上很难归因。
        /// </summary>
        private static void _addLegendDot(RectTransform oLabelLine, int iSeriesIndex)
        {
            Image oDot = UiFactory.CreatePanel(oLabelLine, "Dot", Theme.ChartColor(iSeriesIndex));
            oDot.sprite = SpriteFactory.Circle();

            // 行高比色点大，横向布局组会把它拉成椭圆——跟图标位一样靠 preserveAspect
            // 保住正方形（这里不能靠布局组的 forceExpandHeight 关掉来解决，
            // 那会连带影响同一行里的两段文字）
            oDot.preserveAspect = true;
            oDot.raycastTarget = false;

            UiFactory.SetWidth(oDot.rectTransform, Theme.CHART_DOT_SIZE);
            UiFactory.SetHeight(oDot.rectTransform, Theme.CHART_DOT_SIZE);
        }

        /// <summary>这个类型下一条记录都没有时的提示。各个视图共用同一句话。</summary>
        public static void AddEmptyHint(RectTransform oContent)
        {
            Text oEmpty = UiFactory.CreateText(oContent, "Empty", ReportForm.EMPTY_HINT,
                Theme.FONT_BODY, TextAnchor.MiddleCenter, Theme.TEXT_WEAK);
            UiFactory.SetHeight(oEmpty.rectTransform, EMPTY_HINT_HEIGHT);
        }
    }
}
