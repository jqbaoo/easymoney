using System;
using EasyMoney.Core;
using UnityEngine;
using UnityEngine.UI;

namespace EasyMoney.App.UI
{
    /// <summary>
    /// 选择月份弹窗。顶部一行切年份，下面 12 个月份摆成 3 列 × 4 行，
    /// 当前正在查看的月份高亮，点一下即跳转并关闭。
    ///
    /// 不并进 <see cref="PickerDialog"/>：那个是「不定长的一列选项 + 滚动」，
    /// 这个是「固定的月份网格 + 年份翻页」，形状对不上，硬塞会把两边都搞乱。
    ///
    /// 弹窗挂在调用方给的父节点下（通常是页面 Root），选完或取消后自行销毁。
    /// </summary>
    public static class MonthPickerDialog
    {
        // 宽度用固定值而不是父容器的比例：CanvasScaler 按宽度匹配，
        // 逻辑宽度恒为 750，640 在任何机型上都撑不出去。
        // 高度则交给 ContentSizeFitter——内容是固定的（标题 + 年份行 + 4 行格子 + 取消），
        // 不像 PickerDialog 那样有不定长的列表，用比例高度反而会留白。
        private const float PANEL_WIDTH = 640f;
        private const float PANEL_PADDING = 32f;
        private const float TITLE_HEIGHT = 80f;
        private const float YEAR_ROW_HEIGHT = 88f;
        private const float CELL_HEIGHT = 88f;
        private const float SPACING = 16f;
        private const float NAV_BUTTON_WIDTH = 88f;
        private const float NAV_ICON_SIZE = 40f;
        private const float CANCEL_HEIGHT = 100f;

        private const int COLUMNS = 3;
        private const int MONTHS_PER_YEAR = 12;

        /// <summary>
        /// 弹出选择框。<paramref name="iYear"/> / <paramref name="iMonth"/> 是打开弹窗时
        /// 正在查看的那个月（高亮的就是它），选定后经 <paramref name="oOnPicked"/> 回传新年月。
        /// </summary>
        public static void Show(
            RectTransform oParent, int iYear, int iMonth, Action<int, int> oOnPicked)
        {
            // 「年份行现在停在哪一年」是弹窗自己的状态，静态方法里没处放，
            // 所以开一个一次性实例来搭界面——它与弹窗同生共死
            new Picker(oParent, iYear, iMonth, oOnPicked);
        }

        private sealed class Picker
        {
            private readonly RectTransform m_Overlay;
            private readonly Action<int, int> m_OnPicked;
            private readonly Button[] m_MonthButtons = new Button[MONTHS_PER_YEAR];
            private readonly Text m_YearLabel;

            /// <summary>打开弹窗时正在查看的年月，整场不变——高亮认的是它。</summary>
            private readonly int m_SelectedYear;
            private readonly int m_SelectedMonth;

            /// <summary>年份行当前停在哪一年。切年份只动它，不动选中项。</summary>
            private int m_DisplayYear;

            public Picker(RectTransform oParent, int iYear, int iMonth, Action<int, int> oOnPicked)
            {
                m_SelectedYear = iYear;
                m_SelectedMonth = iMonth;
                m_DisplayYear = iYear;
                m_OnPicked = oOnPicked;

                m_Overlay = UiFactory.CreateNode(oParent, "MonthPickerOverlay");
                UiFactory.Stretch(m_Overlay);

                // 遮罩同时负责拦截穿透到下层的点击：Image 默认 raycastTarget = true
                Image oDim = m_Overlay.gameObject.AddComponent<Image>();
                oDim.color = Theme.SCRIM;

                RectTransform oPanel = _createPanel();
                m_YearLabel = _createYearRow(oPanel);
                _createMonthGrid(oPanel);
                _createCancelButton(oPanel);

                _paintMonths();
            }

            // ── 面板骨架 ────────────────────────────────

            private RectTransform _createPanel()
            {
                Image oPanelImage = UiFactory.CreatePanel(
                    m_Overlay, "Panel", Theme.SURFACE, bRounded: true);

                // 面板浮在遮罩上，加一层投影跟后面的页面拉开
                UiFactory.AddCardShadow(oPanelImage.gameObject);

                RectTransform oPanel = oPanelImage.rectTransform;

                // 点锚居中 + 宽度固定，高度由 ContentSizeFitter 按内容算
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
                oLayout.spacing = SPACING;

                ContentSizeFitter oFitter = oPanel.gameObject.AddComponent<ContentSizeFitter>();
                oFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                Text oTitle = UiFactory.CreateText(oPanel, "Title", "选择月份",
                    Theme.FONT_TITLE, TextAnchor.MiddleCenter, null, Theme.WEIGHT_TITLE);
                UiFactory.SetHeight(oTitle.rectTransform, TITLE_HEIGHT);

                return oPanel;
            }

            // ── 年份行 ──────────────────────────────────

            private Text _createYearRow(RectTransform oPanel)
            {
                RectTransform oRow = UiFactory.CreateRowContainer(oPanel, "YearRow");
                UiFactory.SetHeight(oRow, YEAR_ROW_HEIGHT);

                UiFactory.CreateNavButton(oRow, "PrevYear", IconNames.CHEVRON_LEFT, "<",
                    NAV_BUTTON_WIDTH, NAV_ICON_SIZE, () => _shiftYear(-1));

                Text oLabel = UiFactory.CreateText(oRow, "Year",
                    TimeUtil.FormatYear(m_DisplayYear),
                    Theme.FONT_TITLE, TextAnchor.MiddleCenter, Theme.TEXT, Theme.WEIGHT_TITLE);
                UiFactory.SetFlexible(oLabel.rectTransform);

                UiFactory.CreateNavButton(oRow, "NextYear", IconNames.CHEVRON_RIGHT, ">",
                    NAV_BUTTON_WIDTH, NAV_ICON_SIZE, () => _shiftYear(1));

                return oLabel;
            }

            private void _shiftYear(int iDelta)
            {
                m_DisplayYear += iDelta;
                m_YearLabel.text = TimeUtil.FormatYear(m_DisplayYear);

                // 换了年份就得重着色：选中的那个月只属于原来那一年，
                // 不重跑的话高亮会留在同一格上，看着像是「2025年9月」被选中了
                _paintMonths();
            }

            // ── 月份网格 ────────────────────────────────

            private void _createMonthGrid(RectTransform oPanel)
            {
                // 手搭 4 行 × 3 列，不用 GridLayoutGroup：全项目没用过它，
                // 而且它的 cellSize 是固定值，这边让布局组按面板宽度分配更省事。
                // 「一行若干个各占等宽」的写法与记账页的类型分段同源。
                for (int iRow = 0; iRow < MONTHS_PER_YEAR / COLUMNS; iRow++)
                {
                    RectTransform oRow = UiFactory.CreateRowContainer(oPanel, $"Row{iRow}", SPACING);
                    UiFactory.SetHeight(oRow, CELL_HEIGHT);

                    for (int iCol = 0; iCol < COLUMNS; iCol++)
                    {
                        int iMonth = iRow * COLUMNS + iCol + 1;
                        int iCaptured = iMonth;

                        // 未选中用页面底色而不是卡片色：面板本身就是卡片色，
                        // 用同色的话 12 个格子会糊成一片，看不出哪里能点
                        Button oButton = UiFactory.CreateButton(
                            oRow, $"Month{iMonth}", TimeUtil.FormatMonth(iMonth),
                            () => _pick(iCaptured), Theme.BACKGROUND, Theme.FONT_BODY);
                        UiFactory.SetFlexible(oButton.GetComponent<RectTransform>());

                        m_MonthButtons[iMonth - 1] = oButton;
                    }
                }
            }

            /// <summary>
            /// 给 12 个格子重新着色。切年份后必须重跑——否则高亮会留在原来那一格上。
            /// 选中的判定要**同时**看年份和月份，只看月份的话翻到任何一年，那个月都是亮的。
            /// </summary>
            private void _paintMonths()
            {
                for (int i = 0; i < m_MonthButtons.Length; i++)
                {
                    bool bSelected = m_DisplayYear == m_SelectedYear && i + 1 == m_SelectedMonth;

                    UiFactory.PaintButton(m_MonthButtons[i],
                        bSelected ? Theme.PRIMARY : Theme.BACKGROUND,
                        bSelected ? Theme.WHITE : Theme.TEXT);
                }
            }

            private void _pick(int iMonth)
            {
                _close();
                m_OnPicked?.Invoke(m_DisplayYear, iMonth);
            }

            // ── 取消 ────────────────────────────────────

            private void _createCancelButton(RectTransform oPanel)
            {
                Button oCancel = UiFactory.CreateButton(
                    oPanel, "Cancel", "取消", _close, Theme.BACKGROUND, Theme.FONT_BODY);
                UiFactory.SetHeight(oCancel.GetComponent<RectTransform>(), CANCEL_HEIGHT);
                UiFactory.PaintButton(oCancel, Theme.BACKGROUND, Theme.TEXT);
            }

            private void _close()
            {
                UnityEngine.Object.Destroy(m_Overlay.gameObject);
            }
        }
    }
}
