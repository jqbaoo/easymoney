using System;
using System.Collections.Generic;
using EasyMoney.Core;
using SQLite;

namespace EasyMoney.Data
{
    public sealed class SqliteTransactionRepository : ITransactionRepository
    {
        internal const string SELECT_COLUMNS = @"
            id            AS Id,
            type          AS Type,
            amount        AS AmountCents,
            account_id    AS AccountId,
            to_account_id AS ToAccountId,
            category_id   AS CategoryId,
            note          AS Note,
            occurred_at   AS OccurredAtMs,
            created_at    AS CreatedAtMs,
            updated_at    AS UpdatedAtMs";

        private readonly EasyMoneyDb m_Db;

        public SqliteTransactionRepository(EasyMoneyDb oDb)
        {
            m_Db = oDb;
        }

        public int Insert(Transaction oTransaction)
        {
            m_Db.Connection.Execute(
                @"INSERT INTO tx (type, amount, account_id, to_account_id, category_id, note,
                                  occurred_at, created_at, updated_at)
                  VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)",
                (int)oTransaction.Type,
                oTransaction.AmountCents,
                oTransaction.AccountId,
                oTransaction.ToAccountId,
                oTransaction.CategoryId,
                oTransaction.Note,
                oTransaction.OccurredAtMs,
                oTransaction.CreatedAtMs,
                oTransaction.UpdatedAtMs);

            oTransaction.Id = m_Db.Connection.ExecuteScalar<int>("SELECT last_insert_rowid()");
            return oTransaction.Id;
        }

        public void Update(Transaction oTransaction)
        {
            m_Db.Connection.Execute(
                @"UPDATE tx
                     SET type = ?, amount = ?, account_id = ?, to_account_id = ?, category_id = ?,
                         note = ?, occurred_at = ?, created_at = ?, updated_at = ?
                   WHERE id = ?",
                (int)oTransaction.Type,
                oTransaction.AmountCents,
                oTransaction.AccountId,
                oTransaction.ToAccountId,
                oTransaction.CategoryId,
                oTransaction.Note,
                oTransaction.OccurredAtMs,
                oTransaction.CreatedAtMs,
                oTransaction.UpdatedAtMs,
                oTransaction.Id);
        }

        public Transaction GetById(int iId)
        {
            List<Transaction> lFound = m_Db.Connection.Query<Transaction>(
                $"SELECT {SELECT_COLUMNS} FROM tx WHERE id = ? LIMIT 1", iId);

            return lFound.Count > 0 ? lFound[0] : null;
        }

        public bool Delete(int iId)
        {
            int iAffected = m_Db.Connection.Execute("DELETE FROM tx WHERE id = ?", iId);
            return iAffected > 0;
        }

        public int CountAll()
        {
            return m_Db.Connection.ExecuteScalar<int>("SELECT COUNT(*) FROM tx");
        }

        public List<Transaction> Query(TransactionQuery oQuery)
        {
            throw new NotImplementedException("在 Task 10 实现");
        }
    }
}
