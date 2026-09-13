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
        private const float SUMMARY_BAR_HEIGHT = 88f;
        private const float AMOUNT_WIDTH = 210f;
        private const float DELETE_BUTTON_WIDTH = 72f;
        private const float DELETE_ICON_SIZE = 36f;
        private const float EMPTY_HINT_HEIGHT = 200f;

        /// <summary>
        /// 一次取一个月，不做分页。TransactionQuery 的默认 Limit 只有 50，
        /// 不显式抬高的话记录多的月份会被悄悄截断。
        /// </summary>
        private const int MAX_ROWS_PER_MONTH = 1000;

        private MonthBar m_MonthBar;

        private Text m_IncomeValue;
        private Text m_ExpenseValue;
        private Text m_NetValue;
        private RectTransform m_ListContent;

        public override string Title => "账单";

        public override void OnShow()
        {
            // 年月由 MonthBar 持有：它建出来就停在当前月，之后切回本页会保留用户翻到的月份，
            // 所以这里不再需要「首次才取当前年月」那套判断
            _refresh();
        }

        protected override void _build()
        {
            float fTopHeight = MonthBar.HEIGHT + SUMMARY_BAR_HEIGHT + Theme.DIVIDER_HEIGHT;

            RectTransform oTop = UiFactory.CreateTopColumn(Root, "Top", fTopHeight);

            // 月份变了就重取数——回调直接就是 _refresh
            m_MonthBar = new MonthBar(oTop, Root, _refresh);

            _buildSummaryBar(oTop);
            UiFactory.CreateDivider(oTop, "Divider");

            _buildList(fTopHeight);
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
                Theme.FONT_BODY, TextAnchor.MiddleLeft, oColor, Theme.WEIGHT_AMOUNT);
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

            // 月份条上的文字由 MonthBar 自己维护，这里只管按它的年月取数
            List<Transaction> lTransactions = oContext.Transactions.Query(new TransactionQuery
            {
                // 左闭右开：StartOfNextMonthMs 正好是下月一号零点，
                // 用它当右边界不用再减一毫秒
                StartMs = TimeUtil.StartOfMonthMs(m_MonthBar.Year, m_MonthBar.Month),
                EndMs = TimeUtil.StartOfNextMonthMs(m_MonthBar.Year, m_MonthBar.Month),
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
                m_ListContent, $"Tx_{oStatementRow.Transaction.Id}", Theme.ROW_HEIGHT,
                Theme.CATEGORY_ICON_GAP);

            UiFactory.PaintCard(oRow);

            // 分类图标放在行首。转账、分类被删、自建分类都没有图标名，
            // 那时留一个透明空位，各行的标题左边缘仍然对齐
            UiFactory.CreateIconSlot(oRow, "Icon",
                IconNames.ForCategory(oStatementRow.CategoryIconName),
                Theme.CATEGORY_ICON_SIZE, Theme.TEXT_WEAK);

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
                Theme.FONT_TITLE, TextAnchor.MiddleRight, _amountColor(oStatementRow.Type),
                Theme.WEIGHT_AMOUNT);
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
