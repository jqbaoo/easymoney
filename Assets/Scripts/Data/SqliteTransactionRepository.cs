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
            List<object> lArgs = new List<object>();
            string sWhere = _buildWhere(oQuery, lArgs);

            lArgs.Add(oQuery.Limit);
            lArgs.Add(oQuery.Offset);

            return m_Db.Connection.Query<Transaction>(
                $@"SELECT {SELECT_COLUMNS}
                     FROM tx
                     {sWhere}
                    ORDER BY occurred_at DESC, id DESC
                    LIMIT ? OFFSET ?",
                lArgs.ToArray());
        }

        public int Count(TransactionQuery oQuery)
        {
            List<object> lArgs = new List<object>();
            string sWhere = _buildWhere(oQuery, lArgs);

            return m_Db.Connection.ExecuteScalar<int>($"SELECT COUNT(*) FROM tx {sWhere}", lArgs.ToArray());
        }

        /// <summary>
        /// 拼装 WHERE 子句。所有值都走参数占位符，不做字符串拼接。
        /// 每加一个条件，lClauses 和 lArgs 必须成对添加——SQLite-net 是按
        /// 参数出现顺序逐个绑定的，顺序错位不会报错，只会静默筛出错误的行。
        /// </summary>
        private static string _buildWhere(TransactionQuery oQuery, List<object> lArgs)
        {
            List<string> lClauses = new List<string>();

            if (oQuery.StartMs.HasValue)
            {
                lClauses.Add("occurred_at >= ?");
                lArgs.Add(oQuery.StartMs.Value);
            }

            if (oQuery.EndMs.HasValue)
            {
                lClauses.Add("occurred_at < ?");
                lArgs.Add(oQuery.EndMs.Value);
            }

            if (oQuery.Type.HasValue)
            {
                lClauses.Add("type = ?");
                lArgs.Add((int)oQuery.Type.Value);
            }

            if (oQuery.CategoryId.HasValue)
            {
                lClauses.Add("category_id = ?");
                lArgs.Add(oQuery.CategoryId.Value);
            }

            if (oQuery.AccountId.HasValue)
            {
                if (oQuery.AccountIncludesTransfers)
                {
                    lClauses.Add("(account_id = ? OR (type = 2 AND to_account_id = ?))");
                    lArgs.Add(oQuery.AccountId.Value);
                    lArgs.Add(oQuery.AccountId.Value);
                }
                else
                {
                    lClauses.Add("account_id = ?");
                    lArgs.Add(oQuery.AccountId.Value);
                }
            }

            if (oQuery.MinCents.HasValue)
            {
                lClauses.Add("amount >= ?");
                lArgs.Add(oQuery.MinCents.Value);
            }

            if (oQuery.MaxCents.HasValue)
            {
                lClauses.Add("amount <= ?");
                lArgs.Add(oQuery.MaxCents.Value);
            }

            if (!string.IsNullOrWhiteSpace(oQuery.Keyword))
            {
                // ESCAPE '\' 让用户输入的 % 和 _ 退化为普通字符，
                // 否则搜 "100%" 会把所有以 100 开头的备注都捞出来。
                string sPattern = "%" + _escapeLike(oQuery.Keyword.Trim()) + "%";
                lClauses.Add(@"note LIKE ? ESCAPE '\'");
                lArgs.Add(sPattern);
            }

            if (lClauses.Count == 0)
            {
                return string.Empty;
            }

            return "WHERE " + string.Join(" AND ", lClauses);
        }

        /// <summary>反斜杠必须最先转义，否则会把后面新加的反斜杠又转义一遍。</summary>
        private static string _escapeLike(string sText)
        {
            return sText
                .Replace(@"\", @"\\")
                .Replace("%", @"\%")
                .Replace("_", @"\_");
        }
    }
}
