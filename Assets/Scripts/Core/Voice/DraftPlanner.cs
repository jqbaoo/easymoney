using System.Collections.Generic;

namespace EasyMoney.Core
{
    /// <summary>
    /// 把 <see cref="TransactionDraft"/> 翻译成 <see cref="DraftPlan"/>：
    /// 对着库里的账户与分类把名字换成对象，说清楚这一句到底要改表单上的哪几项。
    ///
    /// 放在 Core 而不是页面里，因为这里全是**规则**，而规则留在页面里就只剩肉眼看：
    /// 分类该按哪个方向找、说了「类型收入」之后「工资」还算不算数、转账时冒出来的
    /// 分类要不要理——每一条都能单独写个用例钉住。
    /// </summary>
    public static class DraftPlanner
    {
        /// <summary>
        /// 生成计划。<paramref name="eCurrentType"/> 是表单**此刻**选着的账单类型，
        /// 只在这一句没说类型时用来兜底（见下面找分类那段）。
        /// </summary>
        public static DraftPlan Build(
            TransactionDraft oDraft, TxType eCurrentType,
            IList<Account> lAccounts, IList<Category> lCategories)
        {
            DraftPlan oPlan = new DraftPlan();

            if (oDraft == null)
            {
                return oPlan;
            }

            oPlan.Type = oDraft.Type;
            oPlan.AmountCents = oDraft.AmountCents;
            oPlan.DateIndex = oDraft.DateIndex;
            oPlan.MentionsAccount = oDraft.AccountName.Length > 0;
            oPlan.MentionsCategory = oDraft.CategoryName.Length > 0;

            if (oDraft.Note.Length > 0)
            {
                oPlan.Note = oDraft.Note;
            }

            // 分类按**这一句自己说出来的类型**找，而不是表单此刻选着的那个：
            // 「类型收入，分类工资」是一句话里自带的方向，拿表单默认的「支出」去找
            // 「工资」只会找不到——用户明明说全了，却被告知没听懂。
            // 这一句没说类型时，才退回表单现值
            TxType eType = oDraft.Type ?? eCurrentType;

            // 转账没有分类可言（记账页那一行根本不显示），填上只会留一份看不见的脏数据：
            // 用户事后把类型改回支出，分类行一亮，里面是个他没选过的分类
            if (eType != TxType.Transfer && oPlan.MentionsCategory)
            {
                oPlan.Category = _categoryById(lCategories,
                    DraftResolver.MatchCategoryId(oDraft.CategoryName, lCategories, eType));

                if (oPlan.Category == null)
                {
                    oPlan.Unhandled.Add(oDraft.CategoryName);
                }
            }

            // 转入反过来：不是转账时它没有意义
            if (eType == TxType.Transfer && oDraft.ToAccountName.Length > 0)
            {
                oPlan.ToAccount = _accountById(lAccounts,
                    DraftResolver.MatchAccountId(oDraft.ToAccountName, lAccounts));

                if (oPlan.ToAccount == null)
                {
                    oPlan.Unhandled.Add(oDraft.ToAccountName);
                }
            }

            if (oPlan.MentionsAccount)
            {
                oPlan.Account = _accountById(lAccounts,
                    DraftResolver.MatchAccountId(oDraft.AccountName, lAccounts));

                if (oPlan.Account == null)
                {
                    oPlan.Unhandled.Add(oDraft.AccountName);
                }
            }

            // 没标签的散字也报给用户：那几个字一个字都没用上，而屏幕上别处都填好了，
            // 不说的话他会以为整句都听懂了
            oPlan.Unhandled.AddRange(oDraft.Unrecognized);

            return oPlan;
        }

        /// <summary>把 Id 换回对象。找不到（含 Id 为 0）返回 null。</summary>
        private static Account _accountById(IList<Account> lAccounts, int iId)
        {
            if (lAccounts == null || iId == 0)
            {
                return null;
            }

            foreach (Account oAccount in lAccounts)
            {
                if (oAccount.Id == iId)
                {
                    return oAccount;
                }
            }

            return null;
        }

        /// <summary>把 Id 换回对象。找不到（含 Id 为 0）返回 null。</summary>
        private static Category _categoryById(IList<Category> lCategories, int iId)
        {
            if (lCategories == null || iId == 0)
            {
                return null;
            }

            foreach (Category oCategory in lCategories)
            {
                if (oCategory.Id == iId)
                {
                    return oCategory;
                }
            }

            return null;
        }
    }
}
