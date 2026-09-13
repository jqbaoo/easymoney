using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EasyMoney.App.UI
{
    /// <summary>
    /// 下拉列表：按钮上显示当前项，点开在**按钮正下方**弹一块面板，选中回调下标。
    ///
    /// 与 PickerDialog 的分工：那个是居中的模态弹窗，适合「分类 / 账户 / 日期」这类
    /// 要挑一会儿的一次性选择；这个是轻量的就地下拉，适合「报表视图」这类**切换**语义——
    /// 遮罩全透明、点空白就收，不打断手上的事。
    ///
    /// 面板的位置不手算坐标，而是把按钮的框换算到遮罩的局部空间再对齐
    /// （RectTransformUtility.CalculateRelativeRectTransformBounds）。手算要自己
    /// 处理 CanvasScaler 的缩放和两级锚点，换台分辨率就会歪。
    /// ⚠️ 这条路径要求布局已经跑过一轮，所以 EditMode 测试只验「弹没弹、选没选中、
    /// 关没关掉」，**位置对不对只能 Play 里看**。
    ///
    /// 面板宽度取按钮宽度，右对齐挂在按钮右下角——也就是说它是往**左下方**展开的，
    /// 调用方得给左边的留出地方（现在用在筛选行最右侧，够）。
    /// </summary>
    public sealed class DropdownButton
    {
        /// <summary>默认宽度。要放下「条形图」加一个箭头。</summary>
        public const float WIDTH = 220f;

        private const float PANEL_MIN_WIDTH = 220f;
        private const float PANEL_ROW_HEIGHT = 88f;
        private const float PANEL_PADDING = 8f;
        private const float PANEL_GAP = 6f;
        private const float CHEVRON_SIZE = 26f;

        /// <summary>
        /// 下拉提示找不到图时的兜底字符。
        ///
        /// 只能用字体子集里真有的字：「▾」「▼」都不在（字体按 GB2312 子集化），
        /// 真机上会渲染成空白——跟 MonthBar 那条是同一个坑。「↓」在子集里。
        /// </summary>
        private const string CHEVRON_FALLBACK = "↓";

        private const string OVERLAY_NAME = "DropdownOverlay";
        private const string PANEL_NAME = "Panel";

        private readonly RectTransform m_OverlayParent;
        private readonly IList<string> m_Options;
        private readonly Action<int> m_OnPicked;
        private readonly RectTransform m_ButtonRect;
        private readonly Text m_Label;
        private readonly float m_Width;

        private RectTransform m_Overlay;

        /// <summary>当前选中项的下标。选项为空时是 -1。</summary>
        public int CurrentIndex { get; private set; }

        /// <summary>面板是不是开着。开着的时候再点按钮不该叠出第二块。</summary>
        public bool IsOpen => m_Overlay != null;

        /// <param name="oParent">按钮挂在哪（报表页是那一行筛选条）</param>
        /// <param name="oOverlayParent">弹层挂在哪（页面 Root，遮罩要盖住整个内容区）</param>
        /// <param name="lOptions">选项文字，顺序即显示顺序</param>
        /// <param name="iCurrentIndex">初始选中项，越界会被钳进范围</param>
        /// <param name="oOnPicked">选中回调，拿到的是下标——调用方自己索引回原列表</param>
        /// <param name="fWidth">按钮宽度</param>
        public DropdownButton(
            RectTransform oParent,
            RectTransform oOverlayParent,
            IList<string> lOptions,
            int iCurrentIndex,
            Action<int> oOnPicked,
            float fWidth = WIDTH)
        {
            m_OverlayParent = oOverlayParent;
            m_Options = lOptions ?? new List<string>();
            m_OnPicked = oOnPicked;
            m_Width = fWidth;
            CurrentIndex = _clamp(iCurrentIndex);

            Image oBackground = UiFactory.CreatePanel(
                oParent, "Dropdown", Theme.SURFACE, bRounded: true, SpriteFactory.Button());
            UiFactory.SetWidth(oBackground.rectTransform, fWidth);
            m_ButtonRect = oBackground.rectTransform;

            Button oButton = oBackground.gameObject.AddComponent<Button>();
            oButton.targetGraphic = oBackground;
            oButton.onClick.AddListener(_open);

            // 不用 CreateButton：它内部的 Label 是铺满居中的，旁边塞不下箭头，
            // 而给那个 Label 再加布局组会跟铺满的锚点打架。自己搭「透明底 + 内部横排」，
            // 与 MonthBar 的年月按钮同一种做法
            RectTransform oBody = UiFactory.CreateRowContainer(oBackground.transform, "Body", 8f);
            UiFactory.Stretch(oBody);

            HorizontalLayoutGroup oLayout = oBody.GetComponent<HorizontalLayoutGroup>();
            oLayout.childAlignment = TextAnchor.MiddleCenter;
            oLayout.childForceExpandWidth = false;

            m_Label = UiFactory.CreateText(oBody, "Label", string.Empty, Theme.FONT_BODY,
                TextAnchor.MiddleLeft, Theme.TEXT, Theme.WEIGHT_TITLE);

            UiFactory.CreateIconOrText(oBody, "Chevron", IconNames.CHEVRON_DOWN, CHEVRON_FALLBACK,
                CHEVRON_SIZE, Theme.FONT_CAPTION, Theme.TEXT_WEAK);

            _refreshLabel();
        }

        // ── 开合 ────────────────────────────────────

        private void _open()
        {
            // 开着的时候再点一下（遮罩没拦住的情况，比如整块界面重建）不该叠出第二块
            if (IsOpen || m_Options.Count == 0)
            {
                return;
            }

            m_Overlay = UiFactory.CreateNode(m_OverlayParent, OVERLAY_NAME);
            UiFactory.Stretch(m_Overlay);

            // 全透明的挡板：点面板以外的地方就收起来。这里刻意不压暗——
            // 下拉是「就地切换」不是模态，把整屏压暗会显得比实际重
            Image oBlocker = m_Overlay.gameObject.AddComponent<Image>();
            oBlocker.color = Theme.TRANSPARENT;

            Button oBlockerButton = m_Overlay.gameObject.AddComponent<Button>();
            oBlockerButton.targetGraphic = oBlocker;
            oBlockerButton.transition = Selectable.Transition.None;
            oBlockerButton.onClick.AddListener(_close);

            RectTransform oPanel = _buildPanel();
            _placePanel(oPanel);
        }

        private void _close()
        {
            if (m_Overlay == null)
            {
                return;
            }

            // 先把引用清掉再销毁：销毁不是立刻生效的，中间这一瞬 IsOpen 不该还骗人
            RectTransform oOverlay = m_Overlay;
            m_Overlay = null;
            UiFactory.DestroyNode(oOverlay.gameObject);
        }

        private RectTransform _buildPanel()
        {
            RectTransform oPanel = UiFactory.CreateNode(m_Overlay, PANEL_NAME);

            // 卡片底自己就是挡板（Image 默认 raycastTarget = true），
            // 免得点在两行之间的缝里穿透到遮罩上、把面板关了
            UiFactory.PaintCard(oPanel);

            VerticalLayoutGroup oLayout = oPanel.gameObject.AddComponent<VerticalLayoutGroup>();
            oLayout.childControlWidth = true;
            oLayout.childControlHeight = true;
            oLayout.childForceExpandWidth = true;
            oLayout.childForceExpandHeight = false;
            oLayout.padding = new RectOffset(
                (int)PANEL_PADDING, (int)PANEL_PADDING, (int)PANEL_PADDING, (int)PANEL_PADDING);

            for (int i = 0; i < m_Options.Count; i++)
            {
                _addOption(oPanel, i);
            }

            return oPanel;
        }

        private void _addOption(RectTransform oPanel, int iIndex)
        {
            RectTransform oRow = UiFactory.CreateNode(oPanel, $"Option_{iIndex}");
            UiFactory.SetHeight(oRow, PANEL_ROW_HEIGHT);

            // 当前项用主色 + 更重的字重标出来。没有打勾的图标资源，用颜色最省事，
            // 也免得为一个勾去等美术
            bool bCurrent = iIndex == CurrentIndex;

            Text oLabel = UiFactory.CreateText(oRow, "Label", m_Options[iIndex], Theme.FONT_BODY,
                TextAnchor.MiddleCenter,
                bCurrent ? Theme.PRIMARY : Theme.TEXT,
                bCurrent ? Theme.WEIGHT_TITLE : Theme.WEIGHT_BODY);
            UiFactory.Stretch(oLabel.rectTransform);

            // 这一行只有布局组、没有 Graphic，接不住点击，补一层全透明射线靶
            Image oHit = oRow.gameObject.AddComponent<Image>();
            oHit.color = Theme.TRANSPARENT;

            Button oButton = oRow.gameObject.AddComponent<Button>();
            oButton.targetGraphic = oHit;

            int iCaptured = iIndex;
            oButton.onClick.AddListener(() => _pick(iCaptured));
        }

        /// <summary>
        /// 把面板挂到按钮正下方。锚点定在遮罩中心，所以 anchoredPosition 是相对中心的量，
        /// 而 CalculateRelativeRectTransformBounds 给的坐标原点正是遮罩的轴心
        /// （铺满的节点轴心在正中），两边对得上。
        /// </summary>
        private void _placePanel(RectTransform oPanel)
        {
            Bounds oButtonBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(
                m_Overlay, m_ButtonRect);

            oPanel.anchorMin = new Vector2(0.5f, 0.5f);
            oPanel.anchorMax = new Vector2(0.5f, 0.5f);
            oPanel.pivot = new Vector2(1f, 1f);
            oPanel.sizeDelta = new Vector2(
                Mathf.Max(m_Width, PANEL_MIN_WIDTH),
                m_Options.Count * PANEL_ROW_HEIGHT + PANEL_PADDING * 2f);

            // 右上角对齐按钮的右下角：面板往左下方展开
            oPanel.anchoredPosition = new Vector2(
                oButtonBounds.max.x,
                oButtonBounds.min.y - PANEL_GAP);
        }

        // ── 选中 ────────────────────────────────────

        private void _pick(int iIndex)
        {
            _close();

            // 选的是当前项：收起面板就好，没必要让页面白刷一次
            // （报表页那个回调会重新查一遍库）
            if (iIndex == CurrentIndex)
            {
                return;
            }

            CurrentIndex = iIndex;
            _refreshLabel();
            m_OnPicked?.Invoke(iIndex);
        }

        private void _refreshLabel()
        {
            m_Label.text = CurrentIndex >= 0 && CurrentIndex < m_Options.Count
                ? m_Options[CurrentIndex]
                : string.Empty;
        }

        private int _clamp(int iIndex)
        {
            if (m_Options.Count == 0)
            {
                return -1;
            }

            if (iIndex < 0)
            {
                return 0;
            }

            return iIndex >= m_Options.Count ? m_Options.Count - 1 : iIndex;
        }
    }
}
