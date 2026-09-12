using System.Collections.Generic;
using EasyMoney.Core;

namespace EasyMoney.Data
{
    public interface ITransactionRepository
    {
        int Insert(Transaction oTransaction);

        void Update(Transaction oTransaction);

        Transaction GetById(int iId);

        bool Delete(int iId);

        List<Transaction> Query(TransactionQuery oQuery);

        /// <summary>满足筛选条件的总条数。分页时用来算总页数，所以忽略 Limit / Offset。</summary>
        int Count(TransactionQuery oQuery);

        int CountAll();
    }
}
