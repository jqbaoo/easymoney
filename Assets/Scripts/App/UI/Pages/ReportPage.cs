using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EasyMoney.App.UI.Pages
{
    /// <summary>
    /// 报表。顶部月份切换与收支汇总，下面按分类列出占比。
    /// 条形图用「轨道 + 填充」两层 Image 实现，不引入图表库也够用。
    /// </summary>
    public class ReportPage : PageBase
    {
        private const float MONTH_BAR_HEIGHT = 88f;
        private const float SUMMARY_HEIGHT = 180f;
        private const float TOGGLE_HEIGHT = 88f;
        private const float VALUE_WIDTH = 280f;
        private const float NAV_BUTTON_WIDTH = 88f;
        private const float NAV_ICON_SIZE = 40f;

        private Text m_MonthLabel;
        private Text m_IncomeValue;
        private Text m_ExpenseValue;
        private Text m_NetValue;
        private RectTransform m_BreakdownContent;
        private Button m_ExpenseTab;
        private Button m_IncomeTab;

        private bool m_ShowExpense = true;

        public override string Title => "报表";

        public override void OnShow()
        {
            _refresh();
        }

        protected override void _build()
        {
            float fTopHeight = MONTH_BAR_HEIGHT + SUMMARY_HEIGHT + TOGGLE_HEIGHT;

            RectTransform oTop = UiFactory.CreateTopColumn(Root, "Top", fTopHeight);
            _buildMonthBar(oTop);
            _buildSummary(oTop);
            _buildToggle(oTop);

            _buildBreakdownList(fTopHeight);
        }

        // ── 月份切换 ────────────────────────────────

        private void _buildMonthBar(RectTransform oParent)
        {
            RectTransform oRow = UiFactory.CreateRowContainer(oParent, "MonthBar");
            UiFactory.SetHeight(oRow, MONTH_BAR_HEIGHT);
            oRow.gameObject.AddComponent<Image>().color = Theme.SURFACE;

            _addNavButton(oRow, "Prev", IconNames.CHEVRON_LEFT, "<");
            m_MonthLabel = UiFactory.CreateText(oRow, "Month", DemoData.MONTH_LABEL,
                Theme.FONT_TITLE, TextAnchor.MiddleCenter);
            UiFactory.SetFlexible(m_MonthLabel.rectTransform);
            _addNavButton(oRow, "Next", IconNames.CHEVRON_RIGHT, ">");
        }

        private static void _addNavButton(RectTransform oRow, string sName, string sIconName, string sFallbackLabel)
        {
            Button oButton = UiFactory.CreateButton(
                oRow, sName, sFallbackLabel, null, Theme.TRANSPARENT, Theme.FONT_TITLE);
            UiFactory.SetWidth(oButton.GetComponent<RectTransform>(), NAV_BUTTON_WIDTH);
            UiFactory.PaintButton(oButton, Theme.TRANSPARENT, Theme.PRIMARY);

            // 美术给了箭头图就用图，没给就是原来的 "<" ">"
            UiFactory.ReplaceButtonLabelWithIcon(oButton, sIconName, NAV_ICON_SIZE, Theme.PRIMARY);
        }

        // ── 收支汇总 ────────────────────────────────

        private void _buildSummary(RectTransform oParent)
        {
            RectTransform oRow = UiFactory.CreateRowContainer(oParent, "Summary");
            UiFactory.SetHeight(oRow, SUMMARY_HEIGHT);
            oRow.gameObject.AddComponent<Image>().color = Theme.SURFACE;

            m_IncomeValue = _addSummaryCell(oRow, "Income", "收入", Theme.INCOME);
            m_ExpenseValue = _addSummaryCell(oRow, "Expense", "支出", Theme.EXPENSE);
            m_NetValue = _addSummaryCell(oRow, "Net", "结余", Theme.TEXT);
        }

        private static Text _addSummaryCell(RectTransform oRow, string sName, string sCaption, Color oColor)
        {
            RectTransform oCell = UiFactory.CreateNode(oRow, sName);
            UiFactory.SetFlexible(oCell);

            VerticalLayoutGroup oLayout = oCell.gameObject.AddComponent<VerticalLayoutGroup>();
            oLayout.childControlWidth = true;
            oLayout.childControlHeight = true;
            oLayout.childForceExpandWidth = true;
            oLayout.childForceExpandHeight = false;

            Text oCaption = UiFactory.CreateText(oCell, "Caption", sCaption,
                Theme.FONT_CAPTION, TextAnchor.MiddleCenter, Theme.TEXT_WEAK);
            UiFactory.SetHeight(oCaption.rectTransform, 50f);

            Text oValue = UiFactory.CreateText(oCell, "Value", "0.00",
                Theme.FONT_TITLE, TextAnchor.MiddleCenter, oColor);
            UiFactory.SetHeight(oValue.rectTransform, 70f);

            return oValue;
        }

        // ── 支出 / 收入 切换 ─────────────────────────

        private void _buildToggle(RectTransform oParent)
        {
            RectTransform oRow = UiFactory.CreateRowContainer(oParent, "Toggle", 4f);
            UiFactory.SetHeight(oRow, TOGGLE_HEIGHT);

            HorizontalLayoutGroup oLayout = oRow.GetComponent<HorizontalLayoutGroup>();
            oLayout.padding = new RectOffset(
                (int)Theme.PAGE_PADDING, (int)Theme.PAGE_PADDING, 12, 12);

            m_ExpenseTab = UiFactory.CreateButton(oRow, "ExpenseTab", "支出构成",
                () => _setShowExpense(true), Theme.PRIMARY, Theme.FONT_BODY);
            UiFactory.SetFlexible(m_ExpenseTab.GetComponent<RectTransform>());

            m_IncomeTab = UiFactory.CreateButton(oRow, "IncomeTab", "收入构成",
                () => _setShowExpense(false), Theme.SURFACE, Theme.FONT_BODY);
            UiFactory.SetFlexible(m_IncomeTab.GetComponent<RectTransform>());

            _paintTabs();
        }

        private void _setShowExpense(bool bShowExpense)
        {
            m_ShowExpense = bShowExpense;
            _paintTabs();
            _refresh();
        }

        private void _paintTabs()
        {
            UiFactory.PaintButton(m_ExpenseTab,
                m_ShowExpense ? Theme.PRIMARY : Theme.SURFACE,
                m_ShowExpense ? Theme.WHITE : Theme.TEXT);

            UiFactory.PaintButton(m_IncomeTab,
                m_ShowExpense ? Theme.SURFACE : Theme.PRIMARY,
                m_ShowExpense ? Theme.TEXT : Theme.WHITE);
        }

        // ── 分类占比 ────────────────────────────────

        private void _buildBreakdownList(float fTopHeight)
        {
            RectTransform oArea = UiFactory.CreateNode(Root, "BreakdownArea");
            UiFactory.StretchWithInsets(oArea, fTopHeight, 0f);

            UiFactory.CreateScroll(oArea, "Scroll", out m_BreakdownContent);
        }

        private void _refresh()
        {
            m_IncomeValue.text = DemoData.SUMMARY_INCOME;
            m_ExpenseValue.text = DemoData.SUMMARY_EXPENSE;
            m_NetValue.text = DemoData.SUMMARY_NET;
            m_NetValue.color = DemoData.SUMMARY_NET.StartsWith("-") ? Theme.EXPENSE : Theme.TEXT;

            List<DemoData.DemoCategory> lItems = m_ShowExpense
                ? DemoData.BuildExpenseCategories()
                : DemoData.BuildIncomeCategories();

            _renderBreakdown(lItems);
        }

        private void _renderBreakdown(List<DemoData.DemoCategory> lItems)
        {
            _clearList();

            foreach (DemoData.DemoCategory oItem in lItems)
            {
                _addBreakdownRow(oItem);
            }
        }

        private void _addBreakdownRow(DemoData.DemoCategory oItem)
        {
            // 这一行要竖排（上面文字、下面条形），所以不能用 CreateRow——
            // 它自带 HorizontalLayoutGroup，再叠加 VerticalLayoutGroup 会打架。
            RectTransform oRow = UiFactory.CreateNode(m_BreakdownContent, $"Item_{oItem.Name}");
            UiFactory.SetHeight(oRow, Theme.ROW_HEIGHT);

            Image oBackground = oRow.gameObject.AddComponent<Image>();
            oBackground.sprite = SpriteFactory.Card();
            oBackground.type = Image.Type.Sliced;
            oBackground.color = Theme.SURFACE;

            VerticalLayoutGroup oLayout = oRow.gameObject.AddComponent<VerticalLayoutGroup>();
            oLayout.childControlWidth = true;
            oLayout.childControlHeight = true;
            oLayout.childForceExpandWidth = true;
            oLayout.childForceExpandHeight = false;
            oLayout.padding = new RectOffset(
                (int)Theme.CARD_PADDING, (int)Theme.CARD_PADDING, 14, 14);

            RectTransform oLabelLine = UiFactory.CreateNode(oRow, "LabelLine");
            UiFactory.SetHeight(oLabelLine, 46f);

            HorizontalLayoutGroup oLabelLayout = oLabelLine.gameObject.AddComponent<HorizontalLayoutGroup>();
            oLabelLayout.childControlWidth = true;
            oLabelLayout.childControlHeight = true;
            oLabelLayout.childForceExpandWidth = false;
            oLabelLayout.childForceExpandHeight = true;

            Text oName = UiFactory.CreateText(oLabelLine, "Name", oItem.Name,
                Theme.FONT_BODY, TextAnchor.MiddleLeft);
            UiFactory.SetFlexible(oName.rectTransform);

            string sRight = $"{oItem.Amount}   {oItem.Ratio * 100f:0.0}%";
            Text oValue = UiFactory.CreateText(oLabelLine, "Value", sRight,
                Theme.FONT_CAPTION, TextAnchor.MiddleRight, Theme.TEXT_WEAK);
            UiFactory.SetWidth(oValue.rectTransform, VALUE_WIDTH);

            _addBar(oRow, oItem.Ratio);
        }

        private void _addBar(RectTransform oParent, float fRatio)
        {
            RectTransform oTrack = UiFactory.CreateNode(oParent, "BarTrack");
            UiFactory.SetHeight(oTrack, Theme.BAR_TRACK_HEIGHT);

            Image oTrackImage = oTrack.gameObject.AddComponent<Image>();
            oTrackImage.sprite = SpriteFactory.Card();
            oTrackImage.type = Image.Type.Sliced;
            oTrackImage.color = Theme.BAR_TRACK;

            Image oFill = UiFactory.CreatePanel(oTrack, "Fill",
                m_ShowExpense ? Theme.EXPENSE : Theme.INCOME, bRounded: true);

            oFill.rectTransform.anchorMin = new Vector2(0f, 0f);
            oFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(fRatio), 1f);
            oFill.rectTransform.offsetMin = Vector2.zero;
            oFill.rectTransform.offsetMax = Vector2.zero;
        }

        private void _clearList()
        {
            for (int i = m_BreakdownContent.childCount - 1; i >= 0; i--)
            {
                Object.Destroy(m_BreakdownContent.GetChild(i).gameObject);
            }
        }
    }
}
