using System.Collections.Generic;
using EasyMoney.Core;

namespace EasyMoney.Data
{
    /// <summary>
    /// 记账编排：查齐校验所需的账户与分类，跑校验，通过后落库并维护时间戳。
    /// 所有写账单的入口都应该走这里，不要直接调 ITransactionRepository——
    /// 绕过服务层就等于绕过校验。
    /// </summary>
    public sealed class TransactionService
    {
        private readonly ITransactionRepository m_TxRepo;
        private readonly IAccountRepository m_AccountRepo;
        private readonly ICategoryRepository m_CategoryRepo;

        public TransactionService(
            ITransactionRepository oTxRepo,
            IAccountRepository oAccountRepo,
            ICategoryRepository oCategoryRepo)
        {
            m_TxRepo = oTxRepo;
            m_AccountRepo = oAccountRepo;
            m_CategoryRepo = oCategoryRepo;
        }

        /// <summary>新建或更新一条账单。Id 为 0 视为新建。</summary>
        public ValidationResult Save(Transaction oTransaction, long iNowMs)
        {
            // 已归档账户也要带上：改一条挂在旧账户下的历史账单，不该因为账户归档就失败
            List<Account> lAccounts = m_AccountRepo.GetAll(bIncludeArchived: true);
            List<Category> lCategories = m_CategoryRepo.GetAll();

            ValidationResult oResult = TransactionValidator.Validate(oTransaction, lAccounts, lCategories);
            if (!oResult.IsValid)
            {
                return oResult;
            }

            bool bIsNew = oTransaction.Id == 0;

            if (bIsNew)
            {
                oTransaction.CreatedAtMs = iNowMs;
                oTransaction.UpdatedAtMs = iNowMs;
                m_TxRepo.Insert(oTransaction);
            }
            else
            {
                // 创建时间是不可变的，只推进更新时间
                oTransaction.UpdatedAtMs = iNowMs;
                m_TxRepo.Update(oTransaction);
            }

            return ValidationResult.Ok();
        }

        /// <summary>
        /// 账户间转账。只写一条 tx 记录（type=Transfer，account_id=转出，to_account_id=转入），
        /// 因此天然原子，不存在「转出成功转入失败」的中间态。
        /// </summary>
        public ValidationResult CreateTransfer(
            int iFromAccountId,
            int iToAccountId,
            long iAmountCents,
            string sNote,
            long iOccurredAtMs,
            long iNowMs)
        {
            Transaction oTx = new Transaction
            {
                Type = TxType.Transfer,
                AmountCents = iAmountCents,
                AccountId = iFromAccountId,
                ToAccountId = iToAccountId,
                CategoryId = 0,
                Note = sNote ?? string.Empty,
                OccurredAtMs = iOccurredAtMs
            };

            return Save(oTx, iNowMs);
        }
    }
}
