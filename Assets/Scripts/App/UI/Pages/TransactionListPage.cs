using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EasyMoney.App.UI.Pages
{
    /// <summary>
    /// 账单。按月查看，顶部是月份切换与收支汇总，下面按天分组列出明细。
    /// </summary>
    public class TransactionListPage : PageBase
    {
        private const float MONTH_BAR_HEIGHT = 88f;
        private const float SUMMARY_BAR_HEIGHT = 88f;
        private const float AMOUNT_WIDTH = 240f;
        private const float NAV_BUTTON_WIDTH = 88f;
        private const float NAV_ICON_SIZE = 40f;

        private Text m_MonthLabel;
        private Text m_IncomeValue;
        private Text m_ExpenseValue;
        private Text m_NetValue;
        private RectTransform m_ListContent;

        public override string Title => "账单";

        public override void OnShow()
        {
            _refresh();
        }

        protected override void _build()
        {
            float fTopHeight = MONTH_BAR_HEIGHT + SUMMARY_BAR_HEIGHT + Theme.DIVIDER_HEIGHT;

            RectTransform oTop = UiFactory.CreateTopColumn(Root, "Top", fTopHeight);
            _buildMonthBar(oTop);
            _buildSummaryBar(oTop);
            UiFactory.CreateDivider(oTop, "Divider");

            _buildList(fTopHeight);
        }

        // ── 月份切换 ────────────────────────────────

        private void _buildMonthBar(RectTransform oParent)
        {
            RectTransform oRow = UiFactory.CreateRowContainer(oParent, "MonthBar");
            UiFactory.SetHeight(oRow, MONTH_BAR_HEIGHT);

            Image oBackground = oRow.gameObject.AddComponent<Image>();
            oBackground.color = Theme.SURFACE;

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

        private void _buildSummaryBar(RectTransform oParent)
        {
            RectTransform oRow = UiFactory.CreateRowContainer(oParent, "SummaryBar");
            UiFactory.SetHeight(oRow, SUMMARY_BAR_HEIGHT);

            Image oBackground = oRow.gameObject.AddComponent<Image>();
            oBackground.color = Theme.SURFACE;

            m_IncomeValue = _addSummaryCell(oRow, "Income", "收入", Theme.INCOME);
            m_ExpenseValue = _addSummaryCell(oRow, "Expense", "支出", Theme.EXPENSE);
            m_NetValue = _addSummaryCell(oRow, "Net", "结余", Theme.TEXT);
        }

        private static Text _addSummaryCell(RectTransform oRow, string sName, string sCaption, Color oColor)
        {
            RectTransform oCell = UiFactory.CreateNode(oRow, sName);
            UiFactory.SetFlexible(oCell);

            HorizontalLayoutGroup oLayout = oCell.gameObject.AddComponent<HorizontalLayoutGroup>();
            oLayout.childControlWidth = true;
            oLayout.childControlHeight = true;
            oLayout.childForceExpandWidth = false;
            oLayout.childForceExpandHeight = true;
            oLayout.spacing = 8f;

            Text oCaptionText = UiFactory.CreateText(oCell, "Caption", sCaption,
                Theme.FONT_CAPTION, TextAnchor.MiddleRight, Theme.TEXT_WEAK);
            LayoutElement oCaptionElement = UiFactory.SetWidth(oCaptionText.rectTransform, 62f);
            oCaptionElement.flexibleWidth = 0f;

            Text oValueText = UiFactory.CreateText(oCell, "Value", "0.00",
                Theme.FONT_BODY, TextAnchor.MiddleLeft, oColor);
            UiFactory.SetFlexible(oValueText.rectTransform);

            return oValueText;
        }

        // ── 明细列表 ────────────────────────────────

        private void _buildList(float fTopHeight)
        {
            RectTransform oListArea = UiFactory.CreateNode(Root, "ListArea");
            UiFactory.StretchWithInsets(oListArea, fTopHeight, 0f);

            UiFactory.CreateScroll(oListArea, "Scroll", out m_ListContent);
        }

        private void _refresh()
        {
            m_IncomeValue.text = DemoData.SUMMARY_INCOME;
            m_ExpenseValue.text = DemoData.SUMMARY_EXPENSE;
            m_NetValue.text = DemoData.SUMMARY_NET;
            m_NetValue.color = DemoData.SUMMARY_NET.StartsWith("-") ? Theme.EXPENSE : Theme.TEXT;

            _renderList(DemoData.BuildDays());
        }

        private void _renderList(List<DemoData.DemoDay> lDays)
        {
            _clearList();

            foreach (DemoData.DemoDay oDay in lDays)
            {
                _addDayHeader(oDay.DateLabel);

                foreach (DemoData.DemoTx oTx in oDay.Items)
                {
                    _addTransactionRow(oTx);
                }
            }
        }

        private void _addDayHeader(string sDateLabel)
        {
            RectTransform oRow = UiFactory.CreateRowContainer(m_ListContent, $"Day_{sDateLabel}");
            UiFactory.SetHeight(oRow, Theme.DATE_HEADER_HEIGHT);

            Text oLabel = UiFactory.CreateText(oRow, "Label", sDateLabel,
                Theme.FONT_CAPTION, TextAnchor.MiddleLeft, Theme.TEXT_WEAK);
            UiFactory.SetFlexible(oLabel.rectTransform);
        }

        private void _addTransactionRow(DemoData.DemoTx oTx)
        {
            RectTransform oRow = UiFactory.CreateRow(m_ListContent, $"Tx_{oTx.Title}", Theme.ROW_HEIGHT);

            Image oBackground = oRow.gameObject.AddComponent<Image>();
            oBackground.sprite = SpriteFactory.Card();
            oBackground.type = Image.Type.Sliced;
            oBackground.color = Theme.SURFACE;

            RectTransform oLeft = UiFactory.CreateNode(oRow, "Left");
            UiFactory.SetFlexible(oLeft);

            VerticalLayoutGroup oLeftLayout = oLeft.gameObject.AddComponent<VerticalLayoutGroup>();
            oLeftLayout.childControlWidth = true;
            oLeftLayout.childControlHeight = true;
            oLeftLayout.childForceExpandWidth = true;
            oLeftLayout.childForceExpandHeight = false;

            Text oTitle = UiFactory.CreateText(oLeft, "Title", oTx.Title,
                Theme.FONT_BODY, TextAnchor.LowerLeft);
            UiFactory.SetHeight(oTitle.rectTransform, 52f);

            Text oSubtitle = UiFactory.CreateText(oLeft, "Subtitle", oTx.Subtitle,
                Theme.FONT_CAPTION, TextAnchor.UpperLeft, Theme.TEXT_WEAK);
            UiFactory.SetHeight(oSubtitle.rectTransform, 40f);

            Text oAmount = UiFactory.CreateText(oRow, "Amount", oTx.Amount,
                Theme.FONT_TITLE, TextAnchor.MiddleRight, _amountColor(oTx.Kind));
            UiFactory.SetWidth(oAmount.rectTransform, AMOUNT_WIDTH);
        }

        private static Color _amountColor(int iKind)
        {
            switch (iKind)
            {
                case 1: return Theme.INCOME;
                case 0: return Theme.EXPENSE;
                default: return Theme.TEXT;
            }
        }

        private void _clearList()
        {
            for (int i = m_ListContent.childCount - 1; i >= 0; i--)
            {
                Object.Destroy(m_ListContent.GetChild(i).gameObject);
            }
        }
    }
}
