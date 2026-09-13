using EasyMoney.Core;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace EasyMoney.App.UI
{
    /// <summary>
    /// 用代码搭 UGUI。所有界面都经由这里创建，样式集中、可 diff、可 review。
    ///
    /// 布局约定（很重要，页面代码依赖它）：
    ///   纵向容器 Column  —— childControlHeight=true，子元素靠 LayoutElement.preferredHeight 定高，宽度自动撑满。
    ///   横向容器 Row     —— childControlWidth=true，子元素靠 preferredWidth 定宽、flexibleWidth=1 占满剩余。
    /// </summary>
    public static class UiFactory
    {
        // 按钮内部节点名。GetButtonIcon / GetButtonLabel 靠它定位，
        // 改名的话记得一起改。
        private const string BODY_NODE = "Body";
        private const string ICON_NODE = "Icon";
        private const string LABEL_NODE = "Label";

        // ── 基础节点 ────────────────────────────────

        public static RectTransform CreateNode(Transform oParent, string sName)
        {
            GameObject oGo = new GameObject(sName, typeof(RectTransform));
            RectTransform oRect = oGo.GetComponent<RectTransform>();
            oRect.SetParent(oParent, false);
            oRect.localScale = Vector3.one;
            return oRect;
        }

        public static Image CreatePanel(
            Transform oParent, string sName, Color oColor, bool bRounded = false, Sprite oSprite = null)
        {
            RectTransform oRect = CreateNode(oParent, sName);
            Image oImage = oRect.gameObject.AddComponent<Image>();

            if (bRounded)
            {
                oImage.sprite = oSprite != null ? oSprite : SpriteFactory.Card();
                oImage.type = Image.Type.Sliced;
            }

            oImage.color = oColor;
            return oImage;
        }

        /// <summary>
        /// 按名字创建一个图标（Assets/Resources/Icons/&lt;名字&gt;.png）。
        /// 资源里没有这张图时返回 <c>null</c>——不报错、不给占位图，
        /// 由调用方决定退化成什么（通常是文字符号）。
        /// </summary>
        public static Image CreateIcon(
            Transform oParent, string sName, string sIconName, float fSize, Color? oColor = null)
        {
            Sprite oSprite = AssetProvider.Icon(sIconName);
            if (oSprite == null)
            {
                return null;
            }

            Image oIcon = CreatePanel(oParent, sName, oColor ?? Theme.TEXT);
            oIcon.sprite = oSprite;
            oIcon.preserveAspect = true;
            oIcon.raycastTarget = false;
            SetWidth(oIcon.rectTransform, fSize);
            SetHeight(oIcon.rectTransform, fSize);
            return oIcon;
        }

        /// <summary>
        /// 图标优先、文字兜底地占一个方形位置。翻页箭头、行尾 chevron 用它：
        /// 有图是图，没图就是原来的 "&lt;" "&gt;"，版式完全不变。
        /// </summary>
        public static RectTransform CreateIconOrText(
            Transform oParent, string sName, string sIconName, string sFallbackText,
            float fSize, int iFontSize, Color? oColor = null,
            FontWeight eWeight = FontWeight.Regular)
        {
            Image oIcon = CreateIcon(oParent, sName, sIconName, fSize, oColor);
            if (oIcon != null)
            {
                return oIcon.rectTransform;
            }

            Text oText = CreateText(oParent, sName, sFallbackText, iFontSize,
                TextAnchor.MiddleCenter, oColor, eWeight);

            // 兜底文字也当成一个方框参与布局，跟图标占同样宽，
            // 免得美术换图后两边版式对不上。
            SetWidth(oText.rectTransform, fSize);
            SetHeight(oText.rectTransform, fSize);
            return oText.rectTransform;
        }

        /// <summary>
        /// 列表行首的图标位：固定占一格，有图标就画出来，没图标就整格透明。
        ///
        /// 没图标也要占位，是因为用户自建的分类没有图标名——那一格若干脆不建，
        /// 这行的文字会往左顶，跟上下行的左边缘参差不齐。
        ///
        /// 不走 <see cref="CreateIconOrText"/>：它没图时会改建一个 Text 兜底，
        /// 而记账页的分类行选中分类后要往这一格填图，换节点的话同帧内新旧两个
        /// 节点会在布局里各占一格。这里无论有没有图都是同一个 Image，
        /// 换图只改 sprite 和颜色，见 <see cref="SetIconSlot"/>。
        /// </summary>
        public static Image CreateIconSlot(
            Transform oParent, string sName, string sIconName, float fSize, Color? oColor = null)
        {
            Color oTint = oColor ?? Theme.TEXT;

            Image oSlot = CreatePanel(oParent, sName, oTint);
            oSlot.preserveAspect = true;
            oSlot.raycastTarget = false;

            SetWidth(oSlot.rectTransform, fSize);
            SetHeight(oSlot.rectTransform, fSize);

            SetIconSlot(oSlot, sIconName, oTint);

            return oSlot;
        }

        /// <summary>
        /// 给图标位换图。找不到图（含传 null）时把整格调成全透明——格子本身留着占位，
        /// 所以「有图标的分类」和「没图标的分类」在列表里占的宽度是一样的。
        /// </summary>
        public static void SetIconSlot(Image oSlot, string sIconName, Color oColor)
        {
            if (oSlot == null)
            {
                return;
            }

            Sprite oSprite = string.IsNullOrEmpty(sIconName) ? null : AssetProvider.Icon(sIconName);

            oSlot.sprite = oSprite;
            oSlot.color = oSprite != null ? oColor : Theme.TRANSPARENT;
        }

        /// <summary>
        /// 创建一个文本。字重参数放在参数表末尾并带默认值——全站调用点大多用位置参数，
        /// 放中间会全线破坏，放末尾才能让已有调用一行都不用改。
        /// </summary>
        public static Text CreateText(
            Transform oParent, string sName, string sContent, int iFontSize,
            TextAnchor oAnchor = TextAnchor.MiddleLeft, Color? oColor = null,
            FontWeight eWeight = FontWeight.Regular)
        {
            RectTransform oRect = CreateNode(oParent, sName);
            Text oText = oRect.gameObject.AddComponent<Text>();
            oText.font = FontProvider.Resolve(eWeight);
            oText.text = sContent;
            oText.fontSize = iFontSize;
            oText.alignment = oAnchor;
            oText.color = oColor ?? Theme.TEXT;
            oText.horizontalOverflow = HorizontalWrapMode.Overflow;
            oText.verticalOverflow = VerticalWrapMode.Truncate;
            oText.raycastTarget = false;
            oText.supportRichText = false;
            return oText;
        }

        // ── 布局容器 ────────────────────────────────

        /// <summary>纵向容器：子元素按 preferredHeight 自上而下排列，宽度撑满。</summary>
        public static RectTransform CreateColumn(Transform oParent, string sName, float fSpacing = 0f)
        {
            RectTransform oRect = CreateNode(oParent, sName);

            VerticalLayoutGroup oLayout = oRect.gameObject.AddComponent<VerticalLayoutGroup>();
            oLayout.childControlWidth = true;
            oLayout.childControlHeight = true;
            oLayout.childForceExpandWidth = true;
            oLayout.childForceExpandHeight = false;
            oLayout.spacing = fSpacing;

            return oRect;
        }

        /// <summary>纵向容器，高度随内容自适应。用于放进滚动区。</summary>
        public static RectTransform CreateAutoColumn(Transform oParent, string sName, float fSpacing = 0f)
        {
            RectTransform oRect = CreateColumn(oParent, sName, fSpacing);

            ContentSizeFitter oFitter = oRect.gameObject.AddComponent<ContentSizeFitter>();
            oFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            return oRect;
        }

        /// <summary>贴顶的纵向容器，定高、宽度撑满。页面顶部区域（月份条、汇总条）用它。</summary>
        public static RectTransform CreateTopColumn(Transform oParent, string sName, float fHeight, float fSpacing = 0f)
        {
            RectTransform oRect = CreateColumn(oParent, sName, fSpacing);
            AnchorTop(oRect, fHeight);
            return oRect;
        }

        /// <summary>横向容器：子元素左右排列，高度撑满。</summary>
        public static RectTransform CreateRowContainer(Transform oParent, string sName, float fSpacing = 0f)
        {
            RectTransform oRect = CreateNode(oParent, sName);

            HorizontalLayoutGroup oLayout = oRect.gameObject.AddComponent<HorizontalLayoutGroup>();
            oLayout.childControlWidth = true;
            oLayout.childControlHeight = true;
            oLayout.childForceExpandWidth = false;
            oLayout.childForceExpandHeight = true;
            oLayout.spacing = fSpacing;

            return oRect;
        }

        /// <summary>
        /// 纵向容器里的一行。定高、横排、白底圆角、左右留内边距。
        /// 这是列表项和表单行的标准形态。
        /// </summary>
        public static RectTransform CreateRow(Transform oParent, string sName, float fHeight, float fSpacing = 0f)
        {
            RectTransform oRect = CreateRowContainer(oParent, sName, fSpacing);

            oRect.gameObject.AddComponent<LayoutElement>().preferredHeight = fHeight;

            HorizontalLayoutGroup oLayout = oRect.GetComponent<HorizontalLayoutGroup>();
            oLayout.padding = new RectOffset(
                (int)Theme.CARD_PADDING, (int)Theme.CARD_PADDING, 0, 0);

            return oRect;
        }

        /// <summary>定高但不带内边距的行。卡片内部的表单行用它，避免与卡片内边距叠加。</summary>
        public static RectTransform CreateBareRow(Transform oParent, string sName, float fHeight, float fSpacing = 0f)
        {
            RectTransform oRect = CreateRowContainer(oParent, sName, fSpacing);
            oRect.gameObject.AddComponent<LayoutElement>().preferredHeight = fHeight;
            return oRect;
        }

        /// <summary>白底圆角卡片，高度随内容自适应，内部留 CARD_PADDING 左右内边距。</summary>
        public static RectTransform CreateCard(Transform oParent, string sName, float fSpacing = 0f)
        {
            Image oCard = CreatePanel(oParent, sName, Theme.SURFACE, bRounded: true);

            VerticalLayoutGroup oLayout = oCard.gameObject.AddComponent<VerticalLayoutGroup>();
            oLayout.childControlWidth = true;
            oLayout.childControlHeight = true;
            oLayout.childForceExpandWidth = true;
            oLayout.childForceExpandHeight = false;
            oLayout.spacing = fSpacing;
            oLayout.padding = new RectOffset((int)Theme.CARD_PADDING, (int)Theme.CARD_PADDING, 0, 0);

            ContentSizeFitter oFitter = oCard.gameObject.AddComponent<ContentSizeFitter>();
            oFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            return oCard.rectTransform;
        }

        // ── 尺寸控制 ────────────────────────────────

        public static LayoutElement SetHeight(RectTransform oRect, float fHeight)
        {
            LayoutElement oElement = _ensureLayoutElement(oRect);
            oElement.preferredHeight = fHeight;
            oElement.minHeight = fHeight;
            return oElement;
        }

        public static LayoutElement SetWidth(RectTransform oRect, float fWidth)
        {
            LayoutElement oElement = _ensureLayoutElement(oRect);
            oElement.preferredWidth = fWidth;
            oElement.minWidth = fWidth;
            return oElement;
        }

        /// <summary>让元素吃掉行内的剩余宽度（配合 SetWidth 的兄弟元素使用）。</summary>
        public static LayoutElement SetFlexible(RectTransform oRect)
        {
            LayoutElement oElement = _ensureLayoutElement(oRect);
            oElement.flexibleWidth = 1f;
            return oElement;
        }

        private static LayoutElement _ensureLayoutElement(RectTransform oRect)
        {
            LayoutElement oElement = oRect.GetComponent<LayoutElement>();
            if (oElement == null)
            {
                oElement = oRect.gameObject.AddComponent<LayoutElement>();
            }

            return oElement;
        }

        // ── 控件 ────────────────────────────────────

        public static Button CreateButton(
            Transform oParent, string sName, string sLabel, UnityAction oOnClick,
            Color? oColor = null, int iFontSize = Theme.FONT_BODY,
            FontWeight eWeight = FontWeight.Regular)
        {
            Image oBackground = CreatePanel(
                oParent, sName, oColor ?? Theme.PRIMARY, bRounded: true, SpriteFactory.Button());

            Button oButton = oBackground.gameObject.AddComponent<Button>();
            oButton.targetGraphic = oBackground;

            if (oOnClick != null)
            {
                oButton.onClick.AddListener(oOnClick);
            }

            Text oLabel = CreateText(oBackground.transform, "Label", sLabel, iFontSize,
                TextAnchor.MiddleCenter, Theme.WHITE, eWeight);
            Stretch(oLabel.rectTransform);

            return oButton;
        }

        /// <summary>
        /// 左图标 + 右文字的按钮（例如「+ 添加账户」）。
        /// 图标缺失时退化成纯文字按钮，文字用 sFallbackLabel ——
        /// 因为没图标时通常要把符号补回来（「添加账户」→「+ 添加账户」）。
        /// </summary>
        public static Button CreateIconTextButton(
            Transform oParent, string sName, string sIconName, string sLabel, UnityAction oOnClick,
            Color? oColor = null, Color? oLabelColor = null, int iFontSize = Theme.FONT_BODY,
            string sFallbackLabel = null, FontWeight eWeight = FontWeight.Regular)
        {
            Color oForeground = oLabelColor ?? Theme.WHITE;

            Image oBackground = CreatePanel(
                oParent, sName, oColor ?? Theme.PRIMARY, bRounded: true, SpriteFactory.Button());

            Button oButton = oBackground.gameObject.AddComponent<Button>();
            oButton.targetGraphic = oBackground;

            if (oOnClick != null)
            {
                oButton.onClick.AddListener(oOnClick);
            }

            if (AssetProvider.Icon(sIconName) == null)
            {
                string sPlain = string.IsNullOrEmpty(sFallbackLabel) ? sLabel : sFallbackLabel;
                Text oPlainText = CreateText(oBackground.transform, LABEL_NODE, sPlain, iFontSize,
                    TextAnchor.MiddleCenter, oForeground, eWeight);
                Stretch(oPlainText.rectTransform);
                return oButton;
            }

            RectTransform oRow = CreateRowContainer(oBackground.transform, BODY_NODE, 8f);
            Stretch(oRow);

            HorizontalLayoutGroup oLayout = oRow.GetComponent<HorizontalLayoutGroup>();
            oLayout.childAlignment = TextAnchor.MiddleCenter;
            oLayout.childForceExpandWidth = false;

            CreateIcon(oRow, ICON_NODE, sIconName, iFontSize * 1.2f, oForeground);
            CreateText(oRow, LABEL_NODE, sLabel, iFontSize, TextAnchor.MiddleLeft, oForeground,
                eWeight);

            return oButton;
        }

        /// <summary>
        /// 底部标签按钮：图标在上、文字在下，垂直居中。
        /// 图标缺失时只剩文字，依然居中，版式不会塌。
        /// </summary>
        public static Button CreateTabButton(
            Transform oParent, string sName, string sIconName, string sLabel, UnityAction oOnClick,
            FontWeight eWeight = FontWeight.Regular)
        {
            Image oBackground = CreatePanel(oParent, sName, Theme.TRANSPARENT);

            Button oButton = oBackground.gameObject.AddComponent<Button>();
            oButton.targetGraphic = oBackground;
            oButton.transition = Selectable.Transition.None;

            if (oOnClick != null)
            {
                oButton.onClick.AddListener(oOnClick);
            }

            RectTransform oColumn = CreateColumn(oBackground.transform, BODY_NODE, 4f);
            Stretch(oColumn);
            oColumn.GetComponent<VerticalLayoutGroup>().childAlignment = TextAnchor.MiddleCenter;

            // 没图标时这一步什么都不加，Column 里只剩文字，依然居中
            CreateIcon(oColumn, ICON_NODE, sIconName, Theme.TAB_ICON_SIZE, Theme.TEXT_WEAK);

            Text oLabel = CreateText(oColumn, LABEL_NODE, sLabel, Theme.FONT_TINY,
                TextAnchor.MiddleCenter, Theme.TEXT_WEAK, eWeight);
            SetHeight(oLabel.rectTransform, Theme.TAB_LABEL_HEIGHT);

            return oButton;
        }

        public static Text GetButtonLabel(Button oButton)
        {
            return oButton.GetComponentInChildren<Text>();
        }

        /// <summary>取标签按钮或图标按钮里的图标，没有则返回 null。用于切换选中态配色。</summary>
        public static Image GetButtonIcon(Button oButton)
        {
            Transform oIcon = oButton.transform.Find($"{BODY_NODE}/{ICON_NODE}");
            return oIcon != null ? oIcon.GetComponent<Image>() : null;
        }

        /// <summary>
        /// 把按钮里的文字标签换成图标。用于翻页箭头这类「有图用图、没图用字」的按钮：
        /// 找不到图标就原样保留文字，什么都不动。
        /// </summary>
        public static Image ReplaceButtonLabelWithIcon(
            Button oButton, string sIconName, float fSize, Color? oColor = null)
        {
            Sprite oSprite = AssetProvider.Icon(sIconName);
            if (oSprite == null)
            {
                return null;
            }

            Text oLabel = GetButtonLabel(oButton);
            if (oLabel != null)
            {
                oLabel.gameObject.SetActive(false);
            }

            Image oIcon = CreatePanel(oButton.transform, ICON_NODE, oColor ?? Theme.WHITE);
            oIcon.sprite = oSprite;
            oIcon.preserveAspect = true;
            oIcon.raycastTarget = false;

            // 按钮内部没有布局组，所以手动居中。
            RectTransform oRect = oIcon.rectTransform;
            oRect.anchorMin = new Vector2(0.5f, 0.5f);
            oRect.anchorMax = new Vector2(0.5f, 0.5f);
            oRect.pivot = new Vector2(0.5f, 0.5f);
            oRect.anchoredPosition = Vector2.zero;
            oRect.sizeDelta = new Vector2(fSize, fSize);

            return oIcon;
        }

        /// <summary>给按钮重新着色并同步文字颜色，用于分段控件的选中态。</summary>
        public static void PaintButton(Button oButton, Color oBackground, Color oLabelColor)
        {
            if (oButton.targetGraphic is Image oImage)
            {
                oImage.color = oBackground;
            }

            Text oLabel = GetButtonLabel(oButton);
            if (oLabel != null)
            {
                oLabel.color = oLabelColor;
            }
        }

        public static InputField CreateInput(
            Transform oParent, string sName, string sPlaceholder, int iFontSize = Theme.FONT_BODY,
            FontWeight eWeight = FontWeight.Regular)
        {
            Image oBackground = CreatePanel(oParent, sName, Theme.TRANSPARENT);

            // 占位符和可编辑文本是两个 Text，字重必须一起给——
            // 只管一个的话，输入框一聚焦就会看到字重跳变
            Text oText = CreateText(oBackground.transform, "Text", string.Empty, iFontSize,
                TextAnchor.MiddleLeft, null, eWeight);
            Stretch(oText.rectTransform);

            Text oPlaceholderText = CreateText(oBackground.transform, "Placeholder", sPlaceholder,
                iFontSize, TextAnchor.MiddleLeft, Theme.TEXT_WEAK, eWeight);
            Stretch(oPlaceholderText.rectTransform);

            InputField oInput = oBackground.gameObject.AddComponent<InputField>();
            oInput.textComponent = oText;
            oInput.placeholder = oPlaceholderText;
            oInput.targetGraphic = oBackground;
            oInput.lineType = InputField.LineType.SingleLine;

            return oInput;
        }

        /// <summary>细分割线。宽度撑满，高度 2（即 1pt）。</summary>
        public static Image CreateDivider(Transform oParent, string sName)
        {
            Image oLine = CreatePanel(oParent, sName, Theme.DIVIDER);
            SetHeight(oLine.rectTransform, Theme.DIVIDER_HEIGHT);
            return oLine;
        }

        // ── 滚动区 ──────────────────────────────────

        public static ScrollRect CreateScroll(Transform oParent, string sName, out RectTransform oContent)
        {
            Image oBackground = CreatePanel(oParent, sName, Theme.TRANSPARENT);
            // 必须铺满：调用方传进来的都是已经定好位的容器（页面主体、弹窗列表区），
            // 指望滚动区自己撑满。少了这一步，滚动区的 RectTransform 会保持新建时的
            // 默认 100x100 居中，而 Viewport 上的 RectMask2D 会把里面的文字裁掉一大半。
            Stretch(oBackground.rectTransform);

            RectTransform oViewport = CreateNode(oBackground.transform, "Viewport");
            Stretch(oViewport);
            oViewport.gameObject.AddComponent<RectMask2D>();

            oContent = CreateAutoColumn(oViewport, "Content", Theme.CARD_GAP);
            oContent.anchorMin = new Vector2(0f, 1f);
            oContent.anchorMax = new Vector2(1f, 1f);
            oContent.pivot = new Vector2(0.5f, 1f);
            oContent.anchoredPosition = Vector2.zero;
            oContent.offsetMin = new Vector2(Theme.PAGE_PADDING, oContent.offsetMin.y);
            oContent.offsetMax = new Vector2(-Theme.PAGE_PADDING, oContent.offsetMax.y);

            ScrollRect oScroll = oBackground.gameObject.AddComponent<ScrollRect>();
            oScroll.viewport = oViewport;
            oScroll.content = oContent;
            oScroll.horizontal = false;
            oScroll.vertical = true;
            oScroll.movementType = ScrollRect.MovementType.Elastic;
            oScroll.elasticity = 0.1f;
            oScroll.scrollSensitivity = 40f;

            return oScroll;
        }

        // ── 锚点工具 ────────────────────────────────

        /// <summary>铺满父容器。</summary>
        public static void Stretch(RectTransform oRect)
        {
            oRect.anchorMin = Vector2.zero;
            oRect.anchorMax = Vector2.one;
            oRect.offsetMin = Vector2.zero;
            oRect.offsetMax = Vector2.zero;
        }

        /// <summary>贴顶，定高。</summary>
        public static void AnchorTop(RectTransform oRect, float fHeight)
        {
            oRect.anchorMin = new Vector2(0f, 1f);
            oRect.anchorMax = new Vector2(1f, 1f);
            oRect.pivot = new Vector2(0.5f, 1f);
            oRect.anchoredPosition = Vector2.zero;
            oRect.sizeDelta = new Vector2(0f, fHeight);
        }

        /// <summary>贴底，定高。</summary>
        public static void AnchorBottom(RectTransform oRect, float fHeight)
        {
            oRect.anchorMin = new Vector2(0f, 0f);
            oRect.anchorMax = new Vector2(1f, 0f);
            oRect.pivot = new Vector2(0.5f, 0f);
            oRect.anchoredPosition = Vector2.zero;
            oRect.sizeDelta = new Vector2(0f, fHeight);
        }

        /// <summary>铺满父容器，但上下各让出指定高度。</summary>
        public static void StretchWithInsets(RectTransform oRect, float fTop, float fBottom)
        {
            oRect.anchorMin = Vector2.zero;
            oRect.anchorMax = Vector2.one;
            oRect.offsetMin = new Vector2(0f, fBottom);
            oRect.offsetMax = new Vector2(0f, -fTop);
        }
    }
}
