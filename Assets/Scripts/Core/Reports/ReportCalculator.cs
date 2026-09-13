using System.Collections.Generic;

namespace EasyMoney.Core
{
    /// <summary>
    /// 报表计算。纯函数，输入是已经查好的账单列表——它不碰数据库，
    /// 所以「本月支出到底怎么算的」可以在测试里逐条断言，不用起库。
    /// </summary>
    public static class ReportCalculator
    {
        public const string UNCATEGORIZED_NAME = "未分类";

        public static PeriodSummary BuildSummary(IEnumerable<Transaction> lTransactions)
        {
            PeriodSummary oSummary = new PeriodSummary();

            if (lTransactions == null)
            {
                return oSummary;
            }

            long iIncomeCents = 0;
            long iExpenseCents = 0;
            int iCount = 0;

            foreach (Transaction oTx in lTransactions)
            {
                // 转账只是钱在自己账户间挪窝，算进收支会让报表虚高
                if (oTx.Type == TxType.Transfer)
                {
                    continue;
                }

                if (oTx.Type == TxType.Income)
                {
                    iIncomeCents += oTx.AmountCents;
                }
                else
                {
                    iExpenseCents += oTx.AmountCents;
                }

                iCount++;
            }

            oSummary.Income = Money.FromCents(iIncomeCents);
            oSummary.Expense = Money.FromCents(iExpenseCents);
            oSummary.TxCount = iCount;

            return oSummary;
        }

        public static List<CategoryBreakdownItem> BuildBreakdown(
            IEnumerable<Transaction> lTransactions,
            TxType oType,
            IList<Category> lCategories)
        {
            Dictionary<int, CategoryBreakdownItem> dItems = new Dictionary<int, CategoryBreakdownItem>();

            if (lTransactions != null)
            {
                foreach (Transaction oTx in lTransactions)
                {
                    // 转账没有分类，即便调用方传了 Transfer 也筛不出东西
                    if (oTx.Type != oType || oTx.Type == TxType.Transfer)
                    {
                        continue;
                    }

                    if (!dItems.TryGetValue(oTx.CategoryId, out CategoryBreakdownItem oItem))
                    {
                        Category oCategory = Category.FindById(lCategories, oTx.CategoryId);

                        oItem = new CategoryBreakdownItem
                        {
                            CategoryId = oTx.CategoryId,
                            CategoryName = oCategory?.Name ?? UNCATEGORIZED_NAME,
                            IconName = oCategory?.IconName ?? string.Empty,
                            Total = Money.Zero,
                            TxCount = 0
                        };

                        dItems[oTx.CategoryId] = oItem;
                    }

                    oItem.Total = oItem.Total + Money.FromCents(oTx.AmountCents);
                    oItem.TxCount++;
                }
            }

            List<CategoryBreakdownItem> lResult = new List<CategoryBreakdownItem>(dItems.Values);
            lResult.Sort(_compareByTotalDescending);

            AssignRatios(lResult);

            return lResult;
        }

        public static void AssignRatios(List<CategoryBreakdownItem> lItems)
        {
            if (lItems == null || lItems.Count == 0)
            {
                return;
            }

            long iGrandTotalCents = 0;
            foreach (CategoryBreakdownItem oItem in lItems)
            {
                iGrandTotalCents += oItem.Total.Cents;
            }

            if (iGrandTotalCents <= 0)
            {
                // 全是 0 元（或理论上不可能出现的负数）时不能做除法，否则整块饼图变 NaN
                foreach (CategoryBreakdownItem oItem in lItems)
                {
                    oItem.Ratio = 0m;
                }
                return;
            }

            foreach (CategoryBreakdownItem oItem in lItems)
            {
                oItem.Ratio = (decimal)oItem.Total.Cents / iGrandTotalCents;
            }
        }

        private static int _compareByTotalDescending(CategoryBreakdownItem oLeft, CategoryBreakdownItem oRight)
        {
            int iCompare = oRight.Total.Cents.CompareTo(oLeft.Total.Cents);
            if (iCompare != 0)
            {
                return iCompare;
            }

            // 金额相同时按分类 Id 稳定排序，避免界面顺序在多次查询间抖动
            return oLeft.CategoryId.CompareTo(oRight.CategoryId);
        }
    }
}
