using System.Collections.Generic;
using EasyMoney.Core;
using UnityEngine;
using UnityEngine.UI;

namespace EasyMoney.App.UI.Pages
{
    /// <summary>
    /// 账单。按月查看，顶部是月份切换与收支汇总，下面按天分组列出明细。
    ///
    /// 页面只做「取数 → 交给 Core 算 → 填界面」三件事：分组规则、金额正负号、
    /// 名称兜底都在 <see cref="StatementBuilder"/> 里，汇总在
    /// <see cref="ReportCalculator"/> 里，这里不写任何业务判断。
    /// </summary>
    public class TransactionListPage : PageBase
    {
        private const float MONTH_BAR_HEIGHT = 88f;
        private const float SUMMARY_BAR_HEIGHT = 88f;
        private const float AMOUNT_WIDTH = 210f;
        private const float NAV_BUTTON_WIDTH = 88f;
        private const float NAV_ICON_SIZE = 40f;
        private const float DELETE_BUTTON_WIDTH = 72f;
        private const float DELETE_ICON_SIZE = 36f;
        private const float EMPTY_HINT_HEIGHT = 200f;

        /// <summary>
        /// 一次取一个月，不做分页。TransactionQuery 的默认 Limit 只有 50，
        /// 不显式抬高的话记录多的月份会被悄悄截断。
        /// </summary>
        private const int MAX_ROWS_PER_MONTH = 1000;

        private int m_Year;
        private int m_Month;

        private Text m_MonthLabel;
        private Text m_IncomeValue;
        private Text m_ExpenseValue;
        private Text m_NetValue;
        private RectTransform m_ListContent;

        public override string Title => "账单";

        public override void OnShow()
        {
            // 首次进来才取当前年月：之后用户可能已经翻到别的月份，
            // 每次切回来都重置会让翻页白做
            if (m_Year == 0)
            {
                (m_Year, m_Month) = TimeUtil.CurrentYearMonth();
            }

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

            _addNavButton(oRow, "Prev", IconNames.CHEVRON_LEFT, "<", _goPreviousMonth);

            m_MonthLabel = UiFactory.CreateText(oRow, "Month", string.Empty,
                Theme.FONT_TITLE, TextAnchor.MiddleCenter);
            UiFactory.SetFlexible(m_MonthLabel.rectTransform);

            _addNavButton(oRow, "Next", IconNames.CHEVRON_RIGHT, ">", _goNextMonth);
        }

        private static void _addNavButton(
            RectTransform oRow, string sName, string sIconName, string sFallbackLabel,
            UnityEngine.Events.UnityAction oOnClick)
        {
            Button oButton = UiFactory.CreateButton(
                oRow, sName, sFallbackLabel, oOnClick, Theme.TRANSPARENT, Theme.FONT_TITLE);
            UiFactory.SetWidth(oButton.GetComponent<RectTransform>(), NAV_BUTTON_WIDTH);
            UiFactory.PaintButton(oButton, Theme.TRANSPARENT, Theme.PRIMARY);

            // 美术给了箭头图就用图，没给就是原来的 "<" ">"
            UiFactory.ReplaceButtonLabelWithIcon(oButton, sIconName, NAV_ICON_SIZE, Theme.PRIMARY);
        }

        private void _goPreviousMonth()
        {
            (m_Year, m_Month) = TimeUtil.AddMonths(m_Year, m_Month, -1);
            _refresh();
        }

        private void _goNextMonth()
        {
            (m_Year, m_Month) = TimeUtil.AddMonths(m_Year, m_Month, 1);
            _refresh();
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
            AppContext oContext = AppContext.Instance;
            if (oContext == null)
            {
                return;
            }

            m_MonthLabel.text = TimeUtil.FormatYearMonth(m_Year, m_Month);

            List<Transaction> lTransactions = oContext.Transactions.Query(new TransactionQuery
            {
                // 左闭右开：StartOfNextMonthMs 正好是下月一号零点，
                // 用它当右边界不用再减一毫秒
                StartMs = TimeUtil.StartOfMonthMs(m_Year, m_Month),
                EndMs = TimeUtil.StartOfNextMonthMs(m_Year, m_Month),
                Limit = MAX_ROWS_PER_MONTH
            });

            _refreshSummary(ReportCalculator.BuildSummary(lTransactions));

            // 账户表连已归档的一起取：归档账户的历史账单还在，名字不能显示成「未知账户」
            _renderList(StatementBuilder.BuildDays(
                lTransactions,
                oContext.Categories.GetAll(),
                oContext.Accounts.GetAll(bIncludeArchived: true)));
        }

        private void _refreshSummary(PeriodSummary oSummary)
        {
            m_IncomeValue.text = oSummary.Income.ToString();
            m_ExpenseValue.text = oSummary.Expense.ToString();
            m_NetValue.text = oSummary.Net.ToString();

            // 结余为负时标红，一眼能看出这个月是倒贴的
            m_NetValue.color = oSummary.Net.Cents < 0 ? Theme.EXPENSE : Theme.TEXT;
        }

        private void _renderList(List<StatementDay> lDays)
        {
            _clearList();

            if (lDays.Count == 0)
            {
                _addEmptyHint();
                return;
            }

            foreach (StatementDay oDay in lDays)
            {
                _addDayHeader(oDay);

                foreach (StatementRow oRow in oDay.Items)
                {
                    _addTransactionRow(oRow);
                }
            }
        }

        private void _addEmptyHint()
        {
            Text oEmpty = UiFactory.CreateText(m_ListContent, "Empty", "这个月还没有记录",
                Theme.FONT_BODY, TextAnchor.MiddleCenter, Theme.TEXT_WEAK);
            UiFactory.SetHeight(oEmpty.rectTransform, EMPTY_HINT_HEIGHT);
        }

        private void _addDayHeader(StatementDay oDay)
        {
            // 节点名用 ISO 日期：DateLabel 里带中文和空格，当名字不好读也不好搜
            RectTransform oRow = UiFactory.CreateRowContainer(
                m_ListContent, $"Day_{oDay.Date:yyyy-MM-dd}");
            UiFactory.SetHeight(oRow, Theme.DATE_HEADER_HEIGHT);

            Text oLabel = UiFactory.CreateText(oRow, "Label", oDay.DateLabel,
                Theme.FONT_CAPTION, TextAnchor.MiddleLeft, Theme.TEXT_WEAK);
            UiFactory.SetFlexible(oLabel.rectTransform);
        }

        private void _addTransactionRow(StatementRow oStatementRow)
        {
            RectTransform oRow = UiFactory.CreateRow(
                m_ListContent, $"Tx_{oStatementRow.Transaction.Id}", Theme.ROW_HEIGHT);

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

            Text oTitle = UiFactory.CreateText(oLeft, "Title", oStatementRow.Title,
                Theme.FONT_BODY, TextAnchor.LowerLeft);
            UiFactory.SetHeight(oTitle.rectTransform, 52f);

            Text oSubtitle = UiFactory.CreateText(oLeft, "Subtitle", oStatementRow.Subtitle,
                Theme.FONT_CAPTION, TextAnchor.UpperLeft, Theme.TEXT_WEAK);
            UiFactory.SetHeight(oSubtitle.rectTransform, 40f);

            Text oAmount = UiFactory.CreateText(oRow, "Amount", oStatementRow.AmountText,
                Theme.FONT_TITLE, TextAnchor.MiddleRight, _amountColor(oStatementRow.Type));
            UiFactory.SetWidth(oAmount.rectTransform, AMOUNT_WIDTH);

            _addDeleteButton(oRow, oStatementRow.Transaction);
        }

        private void _addDeleteButton(RectTransform oRow, Transaction oTransaction)
        {
            Button oDelete = UiFactory.CreateButton(
                oRow, "Delete", "删", () => _delete(oTransaction), Theme.TRANSPARENT, Theme.FONT_CAPTION);
            UiFactory.SetWidth(oDelete.GetComponent<RectTransform>(), DELETE_BUTTON_WIDTH);
            UiFactory.PaintButton(oDelete, Theme.TRANSPARENT, Theme.TEXT_WEAK);

            // 有删除图就用图，没给就是原来的「删」字
            UiFactory.ReplaceButtonLabelWithIcon(
                oDelete, IconNames.DELETE, DELETE_ICON_SIZE, Theme.TEXT_WEAK);
        }

        private void _delete(Transaction oTransaction)
        {
            AppContext oContext = AppContext.Instance;
            if (oContext == null)
            {
                return;
            }

            oContext.Transactions.Delete(oTransaction.Id);

            // 不用自己再刷一次：AppRoot 收到通知后会重走当前页的 OnShow
            oContext.NotifyDataChanged();
        }

        private static Color _amountColor(TxType oType)
        {
            switch (oType)
            {
                case TxType.Income: return Theme.INCOME;
                case TxType.Expense: return Theme.EXPENSE;
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
