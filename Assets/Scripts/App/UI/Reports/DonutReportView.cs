using System.Collections.Generic;
using EasyMoney.Core;
using UnityEngine;
using UnityEngine.UI;

namespace EasyMoney.App.UI.Reports
{
    /// <summary>
    /// 环形视图：一张按占比分块的环，中间放这一类的合计金额，下面接同一份分类明细
    /// （只是不再有那根条——环本身就是「占比」的图形，再来一排条是重复的）。
    /// </summary>
    public class DonutReportView : IReportView
    {
        /// <summary>环的外径（设计稿像素）。</summary>
        public const float SIZE = 440f;

        /// <summary>内径占外径的比例。0.55 时环宽约 99，中间放得下「支出」加一行金额。</summary>
        public const float INNER_RATIO = 0.55f;

        private const float CENTER_WIDTH_RATIO = 0.62f;
        private const float CENTER_CAPTION_HEIGHT = 40f;
        private const float CENTER_VALUE_HEIGHT = 60f;

        public ReportViewMode Mode => ReportViewMode.Donut;

        public void Render(RectTransform oContent, IList<CategoryBreakdownItem> lItems, TxType oType)
        {
            if (lItems == null || lItems.Count == 0)
            {
                ReportViewParts.AddEmptyHint(oContent);
                return;
            }

            _addChart(oContent, lItems, oType);

            foreach (CategoryBreakdownItem oItem in lItems)
            {
                ReportViewParts.AddRow(oContent, oItem, bWithBar: false);
            }
        }

        private static void _addChart(
            RectTransform oContent, IList<CategoryBreakdownItem> lItems, TxType oType)
        {
            // 撑一行给环占位。环是个正方形，不是铺满宽度的矩形，所以它得挂在一个
            // **没有布局组**的容器里，靠锚点定成正方形居中——有布局组的话
            // 锚点和 anchoredPosition 会被布局系统覆盖掉
            RectTransform oHolder = UiFactory.CreateNode(oContent, "Donut");
            UiFactory.SetHeight(oHolder, SIZE);

            // 底色给白色：环的颜色来自贴图本身，这里只做原样呈现，不参与染色
            Image oRing = UiFactory.CreatePanel(oHolder, "Ring", Theme.WHITE);
            oRing.sprite = DonutSprite.Render(DonutLayout.Build(lItems), INNER_RATIO);
            oRing.preserveAspect = true;
            oRing.raycastTarget = false;

            RectTransform oRingRect = oRing.rectTransform;
            oRingRect.anchorMin = new Vector2(0.5f, 0.5f);
            oRingRect.anchorMax = new Vector2(0.5f, 0.5f);
            oRingRect.pivot = new Vector2(0.5f, 0.5f);
            oRingRect.anchoredPosition = Vector2.zero;
            oRingRect.sizeDelta = new Vector2(SIZE, SIZE);

            _addCenterText(oRingRect, lItems, oType);
        }

        /// <summary>
        /// 环中间的那两行字。它是环的子节点而不是兄弟节点——跟着环居中，
        /// 环的尺寸改了这里不用跟着改。
        /// </summary>
        private static void _addCenterText(
            RectTransform oRing, IList<CategoryBreakdownItem> lItems, TxType oType)
        {
            RectTransform oCenter = UiFactory.CreateNode(oRing, "Center");
            oCenter.anchorMin = new Vector2(0.5f, 0.5f);
            oCenter.anchorMax = new Vector2(0.5f, 0.5f);
            oCenter.pivot = new Vector2(0.5f, 0.5f);
            oCenter.anchoredPosition = Vector2.zero;
            oCenter.sizeDelta = new Vector2(
                SIZE * CENTER_WIDTH_RATIO, CENTER_CAPTION_HEIGHT + CENTER_VALUE_HEIGHT);

            VerticalLayoutGroup oLayout = oCenter.gameObject.AddComponent<VerticalLayoutGroup>();
            oLayout.childControlWidth = true;
            oLayout.childControlHeight = true;
            oLayout.childForceExpandWidth = true;
            oLayout.childForceExpandHeight = false;
            oLayout.childAlignment = TextAnchor.MiddleCenter;

            Text oCaption = UiFactory.CreateText(oCenter, "Caption",
                ReportForm.BreakdownTitle(oType), Theme.FONT_CAPTION,
                TextAnchor.MiddleCenter, Theme.TEXT_WEAK);
            UiFactory.SetHeight(oCaption.rectTransform, CENTER_CAPTION_HEIGHT);

            // 金额取这一圈自己的合计，不是从汇总条抄来的——见 ReportForm.BreakdownTotal
            Text oValue = UiFactory.CreateText(oCenter, "Value",
                ReportForm.BreakdownTotal(lItems).ToString(), Theme.FONT_TITLE,
                TextAnchor.MiddleCenter,
                oType == TxType.Income ? Theme.INCOME : Theme.EXPENSE,
                Theme.WEIGHT_AMOUNT);
            UiFactory.SetHeight(oValue.rectTransform, CENTER_VALUE_HEIGHT);
        }
    }
}
