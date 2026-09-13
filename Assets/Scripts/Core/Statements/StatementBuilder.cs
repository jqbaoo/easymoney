using System;
using System.Collections.Generic;

namespace EasyMoney.Core
{
    /// <summary>
    /// 把查出来的账单投影成界面要显示的样子：按天分组，每行拼好三段文案。
    ///
    /// 放 Core 是因为这些规则与界面无关——「转账不带正负号」「备注为空时显示分类名」
    /// 「分类被删了显示未分类」都是业务约定，纯函数才好测；留在页面里就只能靠肉眼验，
    /// 而 EditMode 测试根本跑不到页面。
    ///
    /// 输入需要已按时间倒序排好（仓储的 Query 就是这么返回的），这里不再排序：
    /// 排序规则要和 SQL 的 ORDER BY 保持一致，两处各排一次迟早会不一致。
    /// </summary>
    public static class StatementBuilder
    {
        /// <summary>转账没有备注时的主标题。</summary>
        public const string TRANSFER_TITLE = "转账";

        /// <summary>分类已不存在时的兜底名。与报表共用一个词，免得两处显示不一样。</summary>
        public const string UNCATEGORIZED_NAME = ReportCalculator.UNCATEGORIZED_NAME;

        /// <summary>账户已不存在时的兜底名。</summary>
        public const string UNKNOWN_ACCOUNT_NAME = "未知账户";

        /// <summary>副标题里分类与账户之间的分隔符。</summary>
        public const string SUBTITLE_SEPARATOR = " · ";

        /// <summary>转账副标题里转出与转入之间的箭头。</summary>
        public const string TRANSFER_ARROW = " → ";

        private static readonly string[] WEEKDAY_NAMES =
            { "周日", "周一", "周二", "周三", "周四", "周五", "周六" };

        /// <summary>
        /// 按本地日期分组，组内保持输入顺序，组的顺序也跟随输入（即最新的一天在最前）。
        /// </summary>
        public static List<StatementDay> BuildDays(
            IEnumerable<Transaction> lTransactions, IList<Category> lCategories, IList<Account> lAccounts)
        {
            List<StatementDay> lDays = new List<StatementDay>();

            if (lTransactions == null)
            {
                return lDays;
            }

            StatementDay oCurrentDay = null;

            foreach (Transaction oTransaction in lTransactions)
            {
                // 按本地日期而不是 UTC 日期分组：一笔本地时间早上 7 点的账单，
                // UTC 下已经是前一天了，但用户认定它属于今天
                DateTime oDate = TimeUtil.FromUnixMs(oTransaction.OccurredAtMs).Date;

                if (oCurrentDay == null || oCurrentDay.Date != oDate)
                {
                    oCurrentDay = new StatementDay
                    {
                        Date = oDate,
                        DateLabel = FormatDayLabel(oDate)
                    };

                    lDays.Add(oCurrentDay);
                }

                oCurrentDay.Items.Add(BuildRow(oTransaction, lCategories, lAccounts));
            }

            return lDays;
        }

        /// <summary>把一条账单投影成一行。分类 / 账户表可以传 null，那时名称走兜底。</summary>
        public static StatementRow BuildRow(
            Transaction oTransaction, IList<Category> lCategories, IList<Account> lAccounts)
        {
            // 一次找出分类对象，名称和图标名都从它身上取——为了两个字段遍历两遍不划算
            Category oCategory = Category.FindById(lCategories, oTransaction.CategoryId);

            string sCategoryName = oCategory?.Name ?? UNCATEGORIZED_NAME;

            return new StatementRow
            {
                Transaction = oTransaction,
                Title = _title(oTransaction, sCategoryName),
                Subtitle = oTransaction.IsTransfer
                    ? _accountName(oTransaction.AccountId, lAccounts)
                        + TRANSFER_ARROW + _accountName(oTransaction.ToAccountId, lAccounts)
                    : sCategoryName + SUBTITLE_SEPARATOR + _accountName(oTransaction.AccountId, lAccounts),
                AmountText = _formatAmount(oTransaction),

                // 转账的 CategoryId 是 0、分类被删掉时也找不到，两种都落到空字符串
                CategoryIconName = oCategory?.IconName ?? string.Empty
            };
        }

        /// <summary>日期标题，形如「9月11日 周五」。</summary>
        public static string FormatDayLabel(DateTime oDate)
        {
            // DayOfWeek 是 0=周日，与 WEEKDAY_NAMES 的顺序一致
            return $"{oDate.Month}月{oDate.Day}日 {WEEKDAY_NAMES[(int)oDate.DayOfWeek]}";
        }

        private static string _title(Transaction oTransaction, string sCategoryName)
        {
            if (!string.IsNullOrWhiteSpace(oTransaction.Note))
            {
                return oTransaction.Note;
            }

            // 转账没有分类，兜底文案只能是「转账」而不是「未分类」
            return oTransaction.IsTransfer ? TRANSFER_TITLE : sCategoryName;
        }

        private static string _formatAmount(Transaction oTransaction)
        {
            string sSign = oTransaction.Type switch
            {
                TxType.Income => "+",
                TxType.Expense => "-",

                // 转账只是钱换了个账户，总资产没变，所以不带正负号
                _ => string.Empty
            };

            return sSign + Money.FromCents(oTransaction.AmountCents);
        }

        private static string _accountName(int iAccountId, IList<Account> lAccounts)
        {
            if (lAccounts != null)
            {
                foreach (Account oAccount in lAccounts)
                {
                    if (oAccount.Id == iAccountId)
                    {
                        return oAccount.Name;
                    }
                }
            }

            return UNKNOWN_ACCOUNT_NAME;
        }
    }
}
