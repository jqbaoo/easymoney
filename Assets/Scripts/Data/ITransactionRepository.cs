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

        int CountAll();
    }
}
