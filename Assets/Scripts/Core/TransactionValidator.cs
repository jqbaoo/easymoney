using System.Collections.Generic;

namespace EasyMoney.Core
{
    /// <summary>
    /// 记账规则校验。纯函数，不碰数据库——账户与分类列表由调用方查好传进来。
    /// 放 Core 而不是 Data，是因为这些规则与存储无关，纯函数测起来也快得多。
    /// </summary>
    public static class TransactionValidator
    {
        /// <summary>单笔上限 10 亿元，防止误输入一长串数字把统计冲垮。</summary>
        public const long MaxAmountCents = 100_000_000_000L;

        public static ValidationResult Validate(
            Transaction oTransaction,
            IList<Account> lAccounts,
            IList<Category> lCategories)
        {
            if (oTransaction == null)
            {
                return ValidationResult.Fail("账单数据为空");
            }

            if (oTransaction.AmountCents <= 0)
            {
                return ValidationResult.Fail("金额必须大于 0");
            }

            if (oTransaction.AmountCents > MaxAmountCents)
            {
                return ValidationResult.Fail("金额超出上限");
            }

            if (oTransaction.OccurredAtMs <= 0)
            {
                return ValidationResult.Fail("请选择账单时间");
            }

            ValidationResult oAccountResult = _validateAccounts(oTransaction, lAccounts);
            if (!oAccountResult.IsValid)
            {
                return oAccountResult;
            }

            // 转账不挂分类，账户检查通过就够了
            if (oTransaction.Type == TxType.Transfer)
            {
                return ValidationResult.Ok();
            }

            return _validateCategory(oTransaction, lCategories);
        }

        private static ValidationResult _validateAccounts(Transaction oTransaction, IList<Account> lAccounts)
        {
            Account oAccount = _findAccount(lAccounts, oTransaction.AccountId);
            if (oAccount == null)
            {
                return ValidationResult.Fail("请选择账户");
            }

            if (oTransaction.Type != TxType.Transfer)
            {
                return ValidationResult.Ok();
            }

            if (oTransaction.ToAccountId == 0)
            {
                return ValidationResult.Fail("请选择转入账户");
            }

            if (oTransaction.ToAccountId == oTransaction.AccountId)
            {
                return ValidationResult.Fail("转出与转入不能是同一个账户");
            }

            if (_findAccount(lAccounts, oTransaction.ToAccountId) == null)
            {
                return ValidationResult.Fail("转入账户不存在");
            }

            return ValidationResult.Ok();
        }

        private static ValidationResult _validateCategory(Transaction oTransaction, IList<Category> lCategories)
        {
            if (oTransaction.CategoryId == 0)
            {
                return ValidationResult.Fail("请选择分类");
            }

            Category oCategory = _findCategory(lCategories, oTransaction.CategoryId);
            if (oCategory == null)
            {
                return ValidationResult.Fail("分类不存在");
            }

            // 拿收入分类记支出会让报表的收支两条线彻底乱掉，必须拦住
            CategoryKind oExpected = oTransaction.Type == TxType.Income
                ? CategoryKind.Income
                : CategoryKind.Expense;

            if (oCategory.Kind != oExpected)
            {
                return ValidationResult.Fail("分类与收支类型不匹配");
            }

            return ValidationResult.Ok();
        }

        private static Account _findAccount(IList<Account> lAccounts, int iId)
        {
            if (lAccounts == null)
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

        private static Category _findCategory(IList<Category> lCategories, int iId)
        {
            if (lCategories == null)
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
