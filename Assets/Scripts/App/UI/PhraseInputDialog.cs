using System;
using System.Collections.Generic;
using EasyMoney.Core;
using UnityEngine;
using UnityEngine.UI;

namespace EasyMoney.App.UI
{
    /// <summary>
    /// 「一句话记账」输入弹窗。
    ///
    /// 眼下是手打或粘贴一句。等语音识别（ASR）接上，这里多一个按住说话的按钮就行，
    /// **后面整条链路（解析 → 计划 → 填表）一行都不用改**——识别出来的文字
    /// 走的就是下面这个输入框。
    ///
    /// 解析不通过**不关窗**：用户能看着那句话改了再试，比关掉重来、还得回忆
    /// 自己刚才说了什么强。解析规则在 <see cref="TransactionPhraseParser"/>。
    /// </summary>
    public static class PhraseInputDialog
    {
        private const float PANEL_WIDTH = 640f;
        private const float PANEL_PADDING = 32f;
        private const float TITLE_HEIGHT = 80f;
        private const float HINT_HEIGHT = 96f;
        private const float FIELD_HEIGHT = 110f;
        private const float MESSAGE_HEIGHT = 56f;
        private const float BUTTON_HEIGHT = 100f;

        /// <summary>
        /// 示例句子直接摆出来。这句式和「记一笔，账户张三…」那种顺序随意的说法是一回事，
        /// 摆一个最短的，用户看一眼就知道大概能说什么，不用去翻说明。
        /// </summary>
        private const string HINT = "说一句，比如：\n金额30，账户现金，类型餐饮，备注午饭";

        /// <summary>
        /// 弹出输入框。解析成功后把草稿交给 <paramref name="oOnParsed"/> 并自行销毁。
        /// </summary>
        public static void Show(
            RectTransform oParent, IList<string> lCategoryNames,
            Action<TransactionDraft> oOnParsed)
        {
            RectTransform oOverlay = UiFactory.CreateNode(oParent, "PhraseInputOverlay");
            UiFactory.Stretch(oOverlay);

            // 遮罩同时拦掉穿透到下层的点击：Image 默认 raycastTarget = true
            Image oDim = oOverlay.gameObject.AddComponent<Image>();
            oDim.color = Theme.SCRIM;

            RectTransform oPanel = _createPanel(oOverlay);

            InputField oInput = UiFactory.CreateInput(
                oPanel, "Phrase", "输入一句话", Theme.FONT_BODY);
            oInput.characterLimit = 60;
            UiFactory.SetHeight(oInput.GetComponent<RectTransform>(), FIELD_HEIGHT);

            Text oMessage = UiFactory.CreateText(oPanel, "Message", string.Empty,
                Theme.FONT_CAPTION, TextAnchor.MiddleCenter, Theme.EXPENSE);
            UiFactory.SetHeight(oMessage.rectTransform, MESSAGE_HEIGHT);

            RectTransform oButtons = UiFactory.CreateRowContainer(oPanel, "Buttons", 16f);
            UiFactory.SetHeight(oButtons, BUTTON_HEIGHT);

            _addButton(oButtons, "Cancel", "取消",
                () => _close(oOverlay), Theme.BACKGROUND, Theme.TEXT);

            // 「填入表单」而不是「确定」：这个按钮只把话填进表单，不保存。
            // 金额听错了是一笔真账，所以最后那一下必须留给用户自己按
            _addButton(oButtons, "Confirm", "填入表单",
                () => _confirm(oOverlay, oInput, oMessage, lCategoryNames, oOnParsed),
                Theme.PRIMARY, Theme.WHITE, Theme.WEIGHT_STRONG);
        }

        // ── 面板骨架 ────────────────────────────────

        private static RectTransform _createPanel(RectTransform oOverlay)
        {
            Image oPanelImage = UiFactory.CreatePanel(
                oOverlay, "Panel", Theme.SURFACE, bRounded: true);

            // 面板浮在遮罩上，加一层投影跟后面的页面拉开
            UiFactory.AddCardShadow(oPanelImage.gameObject);

            RectTransform oPanel = oPanelImage.rectTransform;

            // 点锚居中 + 宽度固定，高度交给 ContentSizeFitter 按内容算
            oPanel.anchorMin = new Vector2(0.5f, 0.5f);
            oPanel.anchorMax = new Vector2(0.5f, 0.5f);
            oPanel.pivot = new Vector2(0.5f, 0.5f);
            oPanel.anchoredPosition = Vector2.zero;
            oPanel.sizeDelta = new Vector2(PANEL_WIDTH, 0f);

            VerticalLayoutGroup oLayout = oPanel.gameObject.AddComponent<VerticalLayoutGroup>();
            oLayout.childControlWidth = true;
            oLayout.childControlHeight = true;
            oLayout.childForceExpandWidth = true;
            oLayout.childForceExpandHeight = false;
            oLayout.padding = new RectOffset(
                (int)PANEL_PADDING, (int)PANEL_PADDING, (int)PANEL_PADDING, (int)PANEL_PADDING);
            oLayout.spacing = 16f;

            ContentSizeFitter oFitter = oPanel.gameObject.AddComponent<ContentSizeFitter>();
            oFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Text oTitle = UiFactory.CreateText(oPanel, "Title", "一句话记账",
                Theme.FONT_TITLE, TextAnchor.MiddleCenter, null, Theme.WEIGHT_TITLE);
            UiFactory.SetHeight(oTitle.rectTransform, TITLE_HEIGHT);

            Text oHint = UiFactory.CreateText(oPanel, "Hint", HINT,
                Theme.FONT_CAPTION, TextAnchor.MiddleCenter, Theme.TEXT_WEAK);
            UiFactory.SetHeight(oHint.rectTransform, HINT_HEIGHT);

            return oPanel;
        }

        // ── 按钮与解析 ──────────────────────────────

        private static void _addButton(
            RectTransform oRow, string sName, string sLabel, UnityEngine.Events.UnityAction oOnClick,
            Color oBackground, Color oLabelColor, FontWeight eWeight = FontWeight.Regular)
        {
            Button oButton = UiFactory.CreateButton(
                oRow, sName, sLabel, oOnClick, oBackground, Theme.FONT_BODY, eWeight);
            UiFactory.SetFlexible(oButton.GetComponent<RectTransform>());
            UiFactory.PaintButton(oButton, oBackground, oLabelColor);
        }

        private static void _confirm(
            RectTransform oOverlay, InputField oInput, Text oMessage,
            IList<string> lCategoryNames, Action<TransactionDraft> oOnParsed)
        {
            if (!TransactionPhraseParser.TryParse(
                oInput.text, lCategoryNames, out TransactionDraft oDraft))
            {
                // 不关窗：看着那句话改了再试，比关掉重来、还得回忆自己刚说了什么强
                oMessage.text = "没听懂，换个说法再试一次";
                return;
            }

            _close(oOverlay);
            oOnParsed(oDraft);
        }

        private static void _close(RectTransform oOverlay)
        {
            UnityEngine.Object.Destroy(oOverlay.gameObject);
        }
    }
}
