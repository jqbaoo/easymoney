using System.Collections.Generic;
using EasyMoney.Core;

namespace EasyMoney.Data
{
    public interface IAccountRepository
    {
        List<Account> GetAll(bool bIncludeArchived = false);

        List<AccountBalance> GetAllWithBalance(bool bIncludeArchived = false);

        Account GetById(int iId);

        int Insert(Account oAccount);

        void Update(Account oAccount);

        void SetArchived(int iId, bool bArchived);

        bool Delete(int iId);

        Money GetBalance(int iId);

        Money GetTotalAssets();
    }
}
