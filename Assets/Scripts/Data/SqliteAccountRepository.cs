using System.Collections.Generic;
using EasyMoney.Core;
using SQLite;

namespace EasyMoney.Data
{
    public sealed class SqliteAccountRepository : IAccountRepository
    {
        private const string SELECT_COLUMNS = @"
            id              AS Id,
            name            AS Name,
            type            AS Type,
            initial_balance AS InitialBalanceCents,
            is_archived     AS IsArchived,
            sort_order      AS SortOrder,
            created_at      AS CreatedAtMs";

        /// <summary>
        /// 余额 = 初始余额 + 收入 - 支出 - 转出 + 转入。
        /// 全部用相关子查询实时聚合，不做冗余存储。
        /// </summary>
        private const string SELECT_BALANCE_EXPRESSION = @"
            a.initial_balance
            + COALESCE((SELECT SUM(t.amount) FROM tx t WHERE t.account_id = a.id AND t.type = 1), 0)
            - COALESCE((SELECT SUM(t.amount) FROM tx t WHERE t.account_id = a.id AND t.type = 0), 0)
            - COALESCE((SELECT SUM(t.amount) FROM tx t WHERE t.account_id = a.id AND t.type = 2), 0)
            + COALESCE((SELECT SUM(t.amount) FROM tx t WHERE t.to_account_id = a.id AND t.type = 2), 0)";

        private readonly EasyMoneyDb m_Db;

        public SqliteAccountRepository(EasyMoneyDb oDb)
        {
            m_Db = oDb;
        }

        public List<Account> GetAll(bool bIncludeArchived = false)
        {
            string sWhere = bIncludeArchived ? string.Empty : "WHERE is_archived = 0";
            return m_Db.Connection.Query<Account>(
                $"SELECT {SELECT_COLUMNS} FROM account {sWhere} ORDER BY sort_order, id");
        }

        public List<AccountBalance> GetAllWithBalance(bool bIncludeArchived = false)
        {
            string sWhere = bIncludeArchived ? string.Empty : "WHERE a.is_archived = 0";

            List<AccountBalanceRow> lRows = m_Db.Connection.Query<AccountBalanceRow>(
                $@"SELECT {SELECT_COLUMNS},
                          {SELECT_BALANCE_EXPRESSION} AS BalanceCents
                     FROM account a
                     {sWhere}
                    ORDER BY a.sort_order, a.id");

            List<AccountBalance> lResult = new List<AccountBalance>();
            foreach (AccountBalanceRow oRow in lRows)
            {
                lResult.Add(new AccountBalance(oRow.ToAccount(), Money.FromCents(oRow.BalanceCents)));
            }

            return lResult;
        }

        public Account GetById(int iId)
        {
            List<Account> lFound = m_Db.Connection.Query<Account>(
                $"SELECT {SELECT_COLUMNS} FROM account WHERE id = ? LIMIT 1", iId);

            return lFound.Count > 0 ? lFound[0] : null;
        }

        public int Insert(Account oAccount)
        {
            m_Db.Connection.Execute(
                @"INSERT INTO account (name, type, initial_balance, is_archived, sort_order, created_at)
                  VALUES (?, ?, ?, ?, ?, ?)",
                oAccount.Name,
                (int)oAccount.Type,
                oAccount.InitialBalanceCents,
                oAccount.IsArchived ? 1 : 0,
                oAccount.SortOrder,
                oAccount.CreatedAtMs);

            oAccount.Id = m_Db.Connection.ExecuteScalar<int>("SELECT last_insert_rowid()");
            return oAccount.Id;
        }

        public void Update(Account oAccount)
        {
            m_Db.Connection.Execute(
                @"UPDATE account
                     SET name = ?, type = ?, initial_balance = ?, is_archived = ?, sort_order = ?, created_at = ?
                   WHERE id = ?",
                oAccount.Name,
                (int)oAccount.Type,
                oAccount.InitialBalanceCents,
                oAccount.IsArchived ? 1 : 0,
                oAccount.SortOrder,
                oAccount.CreatedAtMs,
                oAccount.Id);
        }

        public void SetArchived(int iId, bool bArchived)
        {
            m_Db.Connection.Execute(
                "UPDATE account SET is_archived = ? WHERE id = ?", bArchived ? 1 : 0, iId);
        }

        public bool Delete(int iId)
        {
            int iTxCount = m_Db.Connection.ExecuteScalar<int>(
                "SELECT COUNT(*) FROM tx WHERE account_id = ? OR to_account_id = ?", iId, iId);

            if (iTxCount > 0)
            {
                return false;
            }

            return m_Db.Connection.Execute("DELETE FROM account WHERE id = ?", iId) > 0;
        }

        public Money GetBalance(int iId)
        {
            long iCents = m_Db.Connection.ExecuteScalar<long>(
                $"SELECT {SELECT_BALANCE_EXPRESSION} FROM account a WHERE a.id = ?", iId);

            return Money.FromCents(iCents);
        }

        public Money GetTotalAssets()
        {
            long iCents = m_Db.Connection.ExecuteScalar<long>(
                $@"SELECT COALESCE(SUM({SELECT_BALANCE_EXPRESSION}), 0)
                     FROM account a
                    WHERE a.is_archived = 0");

            return Money.FromCents(iCents);
        }

        /// <summary>sqlite-net 的查询映射需要扁平结构，此内部类专门承接带余额的查询结果。</summary>
        private class AccountBalanceRow
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public int Type { get; set; }
            public long InitialBalanceCents { get; set; }
            public int IsArchived { get; set; }
            public int SortOrder { get; set; }
            public long CreatedAtMs { get; set; }
            public long BalanceCents { get; set; }

            public Account ToAccount()
            {
                return new Account
                {
                    Id = Id,
                    Name = Name,
                    Type = (AccountType)Type,
                    InitialBalanceCents = InitialBalanceCents,
                    IsArchived = IsArchived != 0,
                    SortOrder = SortOrder,
                    CreatedAtMs = CreatedAtMs
                };
            }
        }
    }
}
