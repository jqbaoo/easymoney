using System.Collections.Generic;
using EasyMoney.App.UI.Reports;
using EasyMoney.Core;
using UnityEngine;
using UnityEngine.UI;

namespace EasyMoney.App.UI.Pages
{
    /// <summary>
    /// 报表。顶部月份切换与收支汇总，下面按当前视图列出占比。
    ///
    /// 与账单页同源：同一套月份边界、同一个仓储查询，汇总走 <see cref="ReportCalculator"/>，
    /// 展示规则（构成标题、占比文案、条形宽度）在 <see cref="ReportForm"/> 里，
    /// 「这一版到底建出什么节点」在 Reports/ 下那几个视图里。
    /// 页面只做「取数 → 交给 Core 算 → 交给视图画」，自己不写任何展示规则。
    /// </summary>
    public class ReportPage : PageBase
    {
        private const float SUMMARY_HEIGHT = 180f;
        private const float TOGGLE_HEIGHT = 88f;

        /// <summary>视图下拉的宽度。要放下「条形图」三个字加一个箭头。</summary>
        private const float VIEW_DROPDOWN_WIDTH = 220f;

        /// <summary>
        /// 一次取一个月来算报表，不做分页。TransactionQuery 的默认 Limit 只有 50，
        /// 不显式抬高的话记录多的月份会被悄悄截断，报表跟着偏小。
        /// </summary>
        private const int MAX_ROWS_PER_MONTH = 1000;

        private TxType m_BreakdownType = TxType.Expense;

        private MonthBar m_MonthBar;
        private Text m_IncomeValue;
        private Text m_ExpenseValue;
        private Text m_NetValue;
        private Button m_ExpenseTab;
        private Button m_IncomeTab;

        /// <summary>主体那块的画法。换视图是换它，页面不参与。</summary>
        private ReportViewHost m_ViewHost;

        public override string Title => "报表";

        public override void OnShow()
        {
            // 年月由 MonthBar 持有：它建出来就停在当前月，之后切回本页会保留用户翻到的月份，
            // 所以这里不再需要「首次才取当前年月」那套判断
            _refresh();
        }

        protected override void _build()
        {
            float fTopHeight = MonthBar.HEIGHT + SUMMARY_HEIGHT + TOGGLE_HEIGHT;

            RectTransform oTop = UiFactory.CreateTopColumn(Root, "Top", fTopHeight);

            // 与账单页同一条月份条：月份变了就重取数，回调直接是 _refresh
            m_MonthBar = new MonthBar(oTop, Root, _refresh);

            _buildSummary(oTop);
            _buildToggle(oTop);

            _buildBreakdownArea(fTopHeight);
        }

        // ── 收支汇总 ────────────────────────────────

        private void _buildSummary(RectTransform oParent)
        {
            RectTransform oRow = UiFactory.CreateRowContainer(oParent, "Summary");
            UiFactory.SetHeight(oRow, SUMMARY_HEIGHT);

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
                Theme.FONT_TITLE, TextAnchor.MiddleCenter, oColor, Theme.WEIGHT_AMOUNT);
            UiFactory.SetHeight(oValue.rectTransform, 70f);

            return oValue;
        }

        // ── 支出 / 收入 切换 + 视图下拉 ──────────────

        private void _buildToggle(RectTransform oParent)
        {
            RectTransform oRow = UiFactory.CreateRowContainer(oParent, "Toggle", 4f);
            UiFactory.SetHeight(oRow, TOGGLE_HEIGHT);

            HorizontalLayoutGroup oLayout = oRow.GetComponent<HorizontalLayoutGroup>();
            oLayout.padding = new RectOffset(
                (int)Theme.PAGE_PADDING, (int)Theme.PAGE_PADDING, 12, 12);

            m_ExpenseTab = UiFactory.CreateButton(oRow, "ExpenseTab", ReportForm.EXPENSE_TITLE,
                () => _setBreakdownType(TxType.Expense), Theme.PRIMARY, Theme.FONT_BODY);
            UiFactory.SetFlexible(m_ExpenseTab.GetComponent<RectTransform>());

            m_IncomeTab = UiFactory.CreateButton(oRow, "IncomeTab", ReportForm.INCOME_TITLE,
                () => _setBreakdownType(TxType.Income), Theme.SURFACE, Theme.FONT_BODY);
            UiFactory.SetFlexible(m_IncomeTab.GetComponent<RectTransform>());

            _buildViewDropdown(oRow);

            _paintTabs();
        }

        /// <summary>
        /// 视图下拉。选项与文案都来自 <see cref="ReportViews.ALL"/>——以后新增一种视图，
        /// 在 Core 里挂个号、写一个 <see cref="IReportView"/> 实现就够了，这个方法不用改。
        /// </summary>
        private void _buildViewDropdown(RectTransform oRow)
        {
            List<string> lTitles = new List<string>();
            foreach (ReportViewMode oMode in ReportViews.ALL)
            {
                lTitles.Add(ReportViews.Title(oMode));
            }

            // 不留字段引用：按钮的 onClick 挂在本实例的方法上，只要那个按钮还活着，
            // 这个对象就不会被回收（留个字段反而会招来「赋值了没读过」的编译警告）
            new DropdownButton(oRow, Root, lTitles, ReportViews.DefaultIndex,
                _pickView, VIEW_DROPDOWN_WIDTH);
        }

        private void _pickView(int iIndex)
        {
            if (m_ViewHost == null || iIndex < 0 || iIndex >= ReportViews.ALL.Length)
            {
                return;
            }

            // 只有真的换了视图才重刷——选中的还是当前那项时下拉自己就把面板收了
            if (m_ViewHost.SetMode(ReportViews.ALL[iIndex]))
            {
                _refresh();
            }
        }

        private void _setBreakdownType(TxType oType)
        {
            m_BreakdownType = oType;
            _refresh();
        }

        private void _paintTabs()
        {
            bool bExpense = m_BreakdownType == TxType.Expense;

            // 两个 tab 互为反面。原先这里各写了一套「底 + 字」的三元表达式，
            // 收入那个抄反了一个分支（暖白底配白字），走 TogglePalette 就不会了
            TogglePalette.Apply(m_ExpenseTab, bExpense);
            TogglePalette.Apply(m_IncomeTab, !bExpense);
        }

        // ── 分类占比 ────────────────────────────────

        private void _buildBreakdownArea(float fTopHeight)
        {
            RectTransform oArea = UiFactory.CreateNode(Root, "BreakdownArea");
            UiFactory.StretchWithInsets(oArea, fTopHeight, 0f);

            UiFactory.CreateScroll(oArea, "Scroll", out RectTransform oContent);

            // 两种画法都先挂上，下拉里选哪个就画哪个。视图自己建节点、自己渲染，
            // 页面只管把算好的数据递过去
            m_ViewHost = new ReportViewHost(oContent, new BarReportView(), new DonutReportView());
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
                // 左闭右开：StartOfNextMonthMs 正好是下月一号零点，与账单页同一套边界
                StartMs = TimeUtil.StartOfMonthMs(m_MonthBar.Year, m_MonthBar.Month),
                EndMs = TimeUtil.StartOfNextMonthMs(m_MonthBar.Year, m_MonthBar.Month),
                Limit = MAX_ROWS_PER_MONTH
            });

            _refreshSummary(ReportCalculator.BuildSummary(lTransactions));

            _paintTabs();

            m_ViewHost.Render(ReportCalculator.BuildBreakdown(
                lTransactions, m_BreakdownType, oContext.Categories.GetAll()), m_BreakdownType);
        }

        private void _refreshSummary(PeriodSummary oSummary)
        {
            m_IncomeValue.text = oSummary.Income.ToString();
            m_ExpenseValue.text = oSummary.Expense.ToString();
            m_NetValue.text = oSummary.Net.ToString();

            // 结余为负，说明这个月是倒贴的，标红一眼能看出来
            m_NetValue.color = oSummary.Net.Cents < 0 ? Theme.EXPENSE : Theme.TEXT;
        }
    }
}
