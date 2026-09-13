using System;
using System.Collections.Generic;
using EasyMoney.Core;
using UnityEngine;
using UnityEngine.UI;

namespace EasyMoney.App.UI
{
    /// <summary>
    /// 通用选择弹窗。传入一组文字标签，用户点哪一项就回调哪一项的下标。
    /// 分类、账户、转入账户、日期四处都要选东西，与其各写一套弹窗，
    /// 不如统一在这里做——样式和交互只维护一份。
    ///
    /// 弹窗挂在调用方给的父节点下（通常是页面 Root），选完或取消后自行销毁。
    /// </summary>
    public static class PickerDialog
    {
        // 面板占父容器的比例。用比例而不是固定像素，是为了在 18:9 / 20:9
        // 这类长屏上也保持同样的观感。
        private const float PANEL_WIDTH_RATIO = 0.86f;
        private const float PANEL_HEIGHT_RATIO = 0.72f;

        private const float TITLE_HEIGHT = 100f;
        private const float FOOTER_HEIGHT = 120f;
        private const float OPTION_HEIGHT = 104f;
        private const float EMPTY_HINT_HEIGHT = 140f;

        private const string DEFAULT_EMPTY_HINT = "暂无可选项";

        /// <summary>
        /// 弹出选择框。<paramref name="lOptions"/> 为空时只显示 <paramref name="sEmptyHint"/>，
        /// 不显示任何可点项。
        /// </summary>
        public static void Show(
            RectTransform oParent,
            string sTitle,
            IList<string> lOptions,
            Action<int> oOnPicked,
            string sEmptyHint = null)
        {
            RectTransform oOverlay = UiFactory.CreateNode(oParent, "PickerOverlay");
            UiFactory.Stretch(oOverlay);

            // 遮罩同时负责拦截穿透到下层的点击：Image 默认 raycastTarget = true
            Image oDim = oOverlay.gameObject.AddComponent<Image>();
            oDim.color = Theme.SCRIM;

            RectTransform oPanel = _createPanel(oOverlay, sTitle);
            _fillOptions(oPanel, lOptions, sEmptyHint, oOverlay, oOnPicked);
            _createCancelButton(oPanel, oOverlay);
        }

        private static RectTransform _createPanel(RectTransform oOverlay, string sTitle)
        {
            Image oPanelImage = UiFactory.CreatePanel(
                oOverlay, "Panel", Theme.SURFACE, bRounded: true);

            // 没有布局组的容器，尺寸只能靠锚点算：按比例居中
            RectTransform oPanel = oPanelImage.rectTransform;
            oPanel.anchorMin = new Vector2(0.5f - PANEL_WIDTH_RATIO / 2f, 0.5f - PANEL_HEIGHT_RATIO / 2f);
            oPanel.anchorMax = new Vector2(0.5f + PANEL_WIDTH_RATIO / 2f, 0.5f + PANEL_HEIGHT_RATIO / 2f);
            oPanel.offsetMin = Vector2.zero;
            oPanel.offsetMax = Vector2.zero;

            RectTransform oTitle = UiFactory.CreateNode(oPanel, "Title");
            UiFactory.AnchorTop(oTitle, TITLE_HEIGHT);

            Text oTitleText = UiFactory.CreateText(oTitle, "Text", sTitle,
                Theme.FONT_TITLE, TextAnchor.MiddleCenter, null, Theme.WEIGHT_TITLE);
            UiFactory.Stretch(oTitleText.rectTransform);

            return oPanel;
        }

        private static void _fillOptions(
            RectTransform oPanel,
            IList<string> lOptions,
            string sEmptyHint,
            RectTransform oOverlay,
            Action<int> oOnPicked)
        {
            RectTransform oListArea = UiFactory.CreateNode(oPanel, "ListArea");
            oListArea.anchorMin = Vector2.zero;
            oListArea.anchorMax = Vector2.one;
            oListArea.offsetMin = new Vector2(0f, FOOTER_HEIGHT);
            oListArea.offsetMax = new Vector2(0f, -TITLE_HEIGHT);

            UiFactory.CreateScroll(oListArea, "Scroll", out RectTransform oContent);

            if (lOptions == null || lOptions.Count == 0)
            {
                _addEmptyHint(oContent, sEmptyHint);
                return;
            }

            for (int i = 0; i < lOptions.Count; i++)
            {
                int iCaptured = i;
                _addOptionRow(oContent, i, lOptions[i],
                    () => _pick(oOverlay, oOnPicked, iCaptured));
            }
        }

        private static void _addEmptyHint(RectTransform oContent, string sEmptyHint)
        {
            Text oEmpty = UiFactory.CreateText(oContent, "Empty",
                string.IsNullOrEmpty(sEmptyHint) ? DEFAULT_EMPTY_HINT : sEmptyHint,
                Theme.FONT_CAPTION, TextAnchor.MiddleCenter, Theme.TEXT_WEAK);
            UiFactory.SetHeight(oEmpty.rectTransform, EMPTY_HINT_HEIGHT);
        }

        private static void _addOptionRow(RectTransform oContent, int iIndex, string sLabel, Action oOnClick)
        {
            RectTransform oRow = UiFactory.CreateRow(oContent, $"Option_{iIndex}", OPTION_HEIGHT);

            Text oLabel = UiFactory.CreateText(oRow, "Label", sLabel, Theme.FONT_BODY);
            UiFactory.SetFlexible(oLabel.rectTransform);

            // CreateRow 本身只有布局组、没有 Graphic，接不住点击。
            // 补一层全透明的 Image 当射线靶子，视觉上仍是纯列表。
            Image oHit = oRow.gameObject.AddComponent<Image>();
            oHit.color = Theme.TRANSPARENT;

            Button oButton = oRow.gameObject.AddComponent<Button>();
            oButton.targetGraphic = oHit;
            oButton.onClick.AddListener(() => oOnClick());

            UiFactory.CreateDivider(oContent, $"Divider_{iIndex}");
        }

        private static void _createCancelButton(RectTransform oPanel, RectTransform oOverlay)
        {
            RectTransform oFooter = UiFactory.CreateNode(oPanel, "Footer");
            oFooter.anchorMin = new Vector2(0f, 0f);
            oFooter.anchorMax = new Vector2(1f, 0f);
            oFooter.pivot = new Vector2(0.5f, 0f);
            oFooter.sizeDelta = new Vector2(-Theme.CARD_PADDING * 2f, FOOTER_HEIGHT);
            oFooter.anchoredPosition = new Vector2(0f, Theme.CARD_PADDING);

            Button oCancel = UiFactory.CreateButton(oFooter, "Cancel", "取消",
                () => UnityEngine.Object.Destroy(oOverlay.gameObject),
                Theme.SURFACE, Theme.FONT_BODY);
            UiFactory.PaintButton(oCancel, Theme.SURFACE, Theme.TEXT);
            UiFactory.Stretch(oCancel.GetComponent<RectTransform>());

            // 取消按钮铺满 Footer，要留出上下呼吸空间
            RectTransform oCancelRect = oCancel.GetComponent<RectTransform>();
            oCancelRect.offsetMin = new Vector2(0f, Theme.PAGE_GAP);
            oCancelRect.offsetMax = new Vector2(0f, -Theme.PAGE_GAP);
        }

        private static void _pick(RectTransform oOverlay, Action<int> oOnPicked, int iIndex)
        {
            UnityEngine.Object.Destroy(oOverlay.gameObject);
            oOnPicked?.Invoke(iIndex);
        }
    }
}
