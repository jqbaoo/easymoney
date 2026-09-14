using System;
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
    /// **匹配不上一律返回 0**（与记账页「未选中」同义），不抛异常也不猜。
    /// 宁可让用户补一下账户，也不能猜错把账记到别的账户上——
    /// 填错了他不一定发现，留空他一定看得见。
    /// </summary>
    public static class DraftResolver
    {
        /// <summary>把名字匹配成账户 Id，找不到返回 0。</summary>
        public static int MatchAccountId(string sName, IList<Account> lAccounts)
        {
            if (string.IsNullOrWhiteSpace(sName) || lAccounts == null)
            {
                return 0;
            }

            string sWanted = sName.Trim();

            foreach (Account oAccount in lAccounts)
            {
                if (string.Equals(oAccount.Name, sWanted, StringComparison.Ordinal))
                {
                    return oAccount.Id;
                }
            }

            return 0;
        }

        /// <summary>
        /// 把名字匹配成分类 Id，找不到返回 0。
        ///
        /// 按 <paramref name="eType"/> 过滤方向：语音说「餐饮」而类型是收入时，
        /// 那个支出分类不该被选中——真选上了要等到保存才报错，而用户会以为
        /// 是别的地方出了问题。
        /// </summary>
        public static int MatchCategoryId(string sName, IList<Category> lCategories, TxType eType)
        {
            if (string.IsNullOrWhiteSpace(sName) || lCategories == null)
            {
                return 0;
            }

            CategoryKind eWanted = CategoryKindFor(eType);
            string sWanted = sName.Trim();

            foreach (Category oCategory in lCategories)
            {
                if (oCategory.Kind == eWanted
                    && string.Equals(oCategory.Name, sWanted, StringComparison.Ordinal))
                {
                    return oCategory.Id;
                }
            }

            return 0;
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
