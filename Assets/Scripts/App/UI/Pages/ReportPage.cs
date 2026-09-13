using System.Collections.Generic;
using EasyMoney.Core;
using UnityEngine;
using UnityEngine.UI;

namespace EasyMoney.App.UI.Pages
{
    /// <summary>
    /// 报表。顶部月份切换与收支汇总，下面按分类列出占比。
    ///
    /// 与账单页同源：同一套月份边界、同一个仓储查询，汇总走 <see cref="ReportCalculator"/>，
    /// 展示规则（构成标题、占比文案、条形宽度）在 <see cref="ReportForm"/> 里。
    /// 页面只做「取数 → 交给 Core 算 → 填充界面」，不写任何业务判断。
    ///
    /// 条形图用「轨道 + 填充」两层 Image 实现，靠填充层的 anchorMax.x 表达占比，
    /// 比引入图表库轻得多，也够用。
    /// </summary>
    public class ReportPage : PageBase
    {
        private const float SUMMARY_HEIGHT = 180f;
        private const float TOGGLE_HEIGHT = 88f;
        private const float VALUE_WIDTH = 280f;
        private const float EMPTY_HINT_HEIGHT = 200f;

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
        private RectTransform m_BreakdownContent;
        private Button m_ExpenseTab;
        private Button m_IncomeTab;

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

            _buildBreakdownList(fTopHeight);
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

        // ── 支出 / 收入 切换 ─────────────────────────

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

            _paintTabs();
        }

        private void _setBreakdownType(TxType oType)
        {
            m_BreakdownType = oType;
            _refresh();
        }

        private void _paintTabs()
        {
            bool bExpense = m_BreakdownType == TxType.Expense;

            UiFactory.PaintButton(m_ExpenseTab,
                bExpense ? Theme.PRIMARY : Theme.SURFACE,
                bExpense ? Theme.WHITE : Theme.TEXT);

            UiFactory.PaintButton(m_IncomeTab,
                bExpense ? Theme.SURFACE : Theme.PRIMARY,
                bExpense ? Theme.TEXT : Theme.WHITE);
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

            _renderBreakdown(ReportCalculator.BuildBreakdown(
                lTransactions, m_BreakdownType, oContext.Categories.GetAll()));
        }

        private void _refreshSummary(PeriodSummary oSummary)
        {
            m_IncomeValue.text = oSummary.Income.ToString();
            m_ExpenseValue.text = oSummary.Expense.ToString();
            m_NetValue.text = oSummary.Net.ToString();

            // 结余为负，说明这个月是倒贴的，标红一眼能看出来
            m_NetValue.color = oSummary.Net.Cents < 0 ? Theme.EXPENSE : Theme.TEXT;
        }

        private void _renderBreakdown(List<CategoryBreakdownItem> lItems)
        {
            _clearList();

            if (lItems.Count == 0)
            {
                Text oEmpty = UiFactory.CreateText(m_BreakdownContent, "Empty", ReportForm.EMPTY_HINT,
                    Theme.FONT_BODY, TextAnchor.MiddleCenter, Theme.TEXT_WEAK);
                UiFactory.SetHeight(oEmpty.rectTransform, EMPTY_HINT_HEIGHT);
                return;
            }

            foreach (CategoryBreakdownItem oItem in lItems)
            {
                _addBreakdownRow(oItem);
            }
        }

        private void _addBreakdownRow(CategoryBreakdownItem oItem)
        {
            // 这一行要竖排（上面文字、下面条形），所以不能用 CreateRow——
            // 它自带 HorizontalLayoutGroup，再叠加 VerticalLayoutGroup 会打架。
            RectTransform oRow = UiFactory.CreateNode(m_BreakdownContent, $"Item_{oItem.CategoryId}");
            UiFactory.SetHeight(oRow, Theme.ROW_HEIGHT);

            UiFactory.PaintCard(oRow);

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
            oLabelLayout.spacing = Theme.CATEGORY_ICON_GAP;

            // 与账单列表同一个道理：分类被删或没配图标时留透明空位，名字的左边仍然对齐
            UiFactory.CreateIconSlot(oLabelLine, "Icon",
                IconNames.ForCategory(oItem.IconName),
                Theme.CATEGORY_ICON_SIZE, Theme.TEXT_WEAK);

            Text oName = UiFactory.CreateText(oLabelLine, "Name", oItem.CategoryName,
                Theme.FONT_BODY, TextAnchor.MiddleLeft);
            UiFactory.SetFlexible(oName.rectTransform);

            Text oValue = UiFactory.CreateText(oLabelLine, "Value",
                ReportForm.BreakdownValueText(oItem.Total, oItem.Ratio),
                Theme.FONT_CAPTION, TextAnchor.MiddleRight, Theme.TEXT_WEAK);
            UiFactory.SetWidth(oValue.rectTransform, VALUE_WIDTH);

            _addBar(oRow, oItem.Ratio);
        }

        private void _addBar(RectTransform oParent, decimal dRatio)
        {
            RectTransform oTrack = UiFactory.CreateNode(oParent, "BarTrack");
            UiFactory.SetHeight(oTrack, Theme.BAR_TRACK_HEIGHT);

            Image oTrackImage = oTrack.gameObject.AddComponent<Image>();
            oTrackImage.sprite = SpriteFactory.Card();
            oTrackImage.type = Image.Type.Sliced;
            oTrackImage.color = Theme.BAR_TRACK;

            Image oFill = UiFactory.CreatePanel(oTrack, "Fill",
                m_BreakdownType == TxType.Expense ? Theme.EXPENSE : Theme.INCOME, bRounded: true);

            // 用锚点右边界表达占比：0 = 一点不画，1 = 铺满整条轨道
            float fRatio = (float)ReportForm.BarWidthRatio(dRatio);

            oFill.rectTransform.anchorMin = new Vector2(0f, 0f);
            oFill.rectTransform.anchorMax = new Vector2(fRatio, 1f);
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
