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
            Show(oParent, sTitle, lOptions, null, oOnPicked, sEmptyHint);
        }

        /// <summary>
        /// 带图标的选择框。<paramref name="lIconNames"/> 与 <paramref name="lOptions"/> 按下标对应，
        /// 元素可以是 null（该项没图标，留一个等宽空位）；传 null 则整列都没有图标。
        ///
        /// 做成重载而不是往原签名末尾加参数：分类 / 账户 / 转入 / 日期 / 账户类型
        /// 五个调用点一行都不用改，只有分类那一处用得上图标。
        ///
        /// 图标名的解析（哪张图在 Resources 里真的存在）由调用方负责——
        /// 这个弹窗是分类、账户、日期共用的，让它认识「分类图标」是没必要的耦合。
        /// </summary>
        public static void Show(
            RectTransform oParent,
            string sTitle,
            IList<string> lOptions,
            IList<string> lIconNames,
            Action<int> oOnPicked,
            string sEmptyHint = null)
        {
            RectTransform oOverlay = UiFactory.CreateNode(oParent, "PickerOverlay");
            UiFactory.Stretch(oOverlay);

            // 遮罩同时负责拦截穿透到下层的点击：Image 默认 raycastTarget = true
            Image oDim = oOverlay.gameObject.AddComponent<Image>();
            oDim.color = Theme.SCRIM;

            RectTransform oPanel = _createPanel(oOverlay, sTitle);
            _fillOptions(oPanel, lOptions, lIconNames, sEmptyHint, oOverlay, oOnPicked);
            _createCancelButton(oPanel, oOverlay);
        }

        private static RectTransform _createPanel(RectTransform oOverlay, string sTitle)
        {
            Image oPanelImage = UiFactory.CreatePanel(
                oOverlay, "Panel", Theme.SURFACE, bRounded: true);

            // 面板浮在遮罩上，加一层投影跟后面的页面拉开
            UiFactory.AddCardShadow(oPanelImage.gameObject);

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
            IList<string> lIconNames,
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
                // 图标名列表可能比选项少（或压根没传），按下标取、越界就当作没图标
                string sIconName = lIconNames != null && i < lIconNames.Count
                    ? lIconNames[i]
                    : null;

                int iCaptured = i;
                _addOptionRow(oContent, i, lOptions[i], lIconNames != null, sIconName,
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

        private static void _addOptionRow(
            RectTransform oContent, int iIndex, string sLabel, bool bWithIconSlot,
            string sIconName, Action oOnClick)
        {
            // 传了图标名列表才建图标位：账户 / 转入账户 / 日期 / 账户类型四个弹窗都没传，
            // 它们每一行都该跟改动前长得一模一样，不能平白多出一格左缩进。
            // 反过来，一旦建了这一格，它就跟当前这项有没有图无关——自建分类混在
            // 预置分类中间时，各行文字的左边缘才是齐的。
            RectTransform oRow = UiFactory.CreateRow(
                oContent, $"Option_{iIndex}", OPTION_HEIGHT,
                bWithIconSlot ? Theme.CATEGORY_ICON_GAP : 0f);

            if (bWithIconSlot)
            {
                UiFactory.CreateIconSlot(
                    oRow, "Icon", sIconName, Theme.CATEGORY_ICON_SIZE, Theme.TEXT_WEAK);
            }

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
