using System.Collections.Generic;

namespace EasyMoney.Core
{
    /// <summary>
    /// 把草稿里的**名字**换成数据库里的 **Id**。
    ///
    /// 解析器只认字不认库——「餐饮」对应哪个分类，得有人拿着分类表比对。
    /// 这一步也放在 Core 而不是页面里：匹配不上该不该报错、重名取哪个、
    /// 空名字算不算，全是规则。留在页面里就只剩肉眼看，而 EditMode 测试够不着页面。
    ///
    /// 匹配分两步：
    ///
    /// ① **精确全等**（去首尾空白后）。命中即返回，与最初的实现完全一致。
    /// ② **模糊回退**——ASR 的同音错字（「张三」→「张散」）就靠这一步救回来，
    ///    规则全在 <see cref="NameMatcher.PickUniqueIndex"/>，**条条往严里走**。
    ///
    /// 两步都不中才返回 0。返回 0 与记账页「未选中」同义，调用方据此留空并报给用户，
    /// **绝不补默认值**——补了他会以为语音已经填对，直接点保存就记到别的账户上了。
    ///
    /// ⚠️ <b>接口保持返回一个 int，不告诉调用方「这是猜的」。</b>这是有意的：
    /// 页面拿到的是「该填哪个」，不是「这个结果有多可信」。真要区分时（比如以后想在
    /// 界面上把猜中的项标出来），改的是 <see cref="DraftPlan"/> 那一层，
    /// 而不是让每个调用方各自去解释一个标志位。
    /// </summary>
    public static class DraftResolver
    {
        /// <summary>把名字匹配成账户 Id，找不到返回 0。</summary>
        public static int MatchAccountId(string sName, IList<Account> lAccounts)
        {
            if (string.IsNullOrWhiteSpace(sName) || lAccounts == null || lAccounts.Count == 0)
            {
                return 0;
            }

            List<string> lNames = new List<string>(lAccounts.Count);
            foreach (Account oAccount in lAccounts)
            {
                lNames.Add(oAccount.Name);
            }

            int iIndex = NameMatcher.PickUniqueIndex(sName, lNames);
            return iIndex < 0 ? 0 : lAccounts[iIndex].Id;
        }

        /// <summary>
        /// 把名字匹配成分类 Id，找不到返回 0。
        ///
        /// 按 <paramref name="eType"/> 过滤方向：语音说「餐饮」而类型是收入时，
        /// 那个支出分类不该被选中——真选上了要等到保存才报错，而用户会以为
        /// 是别的地方出了问题。
        ///
        /// ⚠️ <b>方向过滤发生在模糊匹配之前</b>：候选表里只有正确方向的分类，
        /// 所以「支出分类不会被收入分类抢走」是**结构上**保证的，
        /// 而不是靠事后再筛一遍距离——后者在「两个方向各有一个很像的候选」时
        /// 会把它们当成并列而放弃，白白丢掉一个本来很确定的匹配。
        /// </summary>
        public static int MatchCategoryId(string sName, IList<Category> lCategories, TxType eType)
        {
            if (string.IsNullOrWhiteSpace(sName) || lCategories == null || lCategories.Count == 0)
            {
                return 0;
            }

            CategoryKind eWanted = CategoryKindFor(eType);

            List<Category> lCandidates = new List<Category>();
            List<string> lNames = new List<string>();

            foreach (Category oCategory in lCategories)
            {
                if (oCategory.Kind == eWanted)
                {
                    lCandidates.Add(oCategory);
                    lNames.Add(oCategory.Name);
                }
            }

            int iIndex = NameMatcher.PickUniqueIndex(sName, lNames);
            return iIndex < 0 ? 0 : lCandidates[iIndex].Id;
        }

        /// <summary>
        /// 账单类型对应的分类方向。
        ///
        /// 转账没有分类，返回支出方向只是为了让调用方拿到一个确定值——
        /// 记账页在转账时根本不显示分类行，这个返回值不会被用上。
        /// </summary>
        public static CategoryKind CategoryKindFor(TxType eType)
        {
            return eType == TxType.Income ? CategoryKind.Income : CategoryKind.Expense;
        }
    }
}
