using System.Collections.Generic;
using EasyMoney.Core;
using SQLite;

namespace EasyMoney.Data
{
    public sealed class SqliteCategoryRepository : ICategoryRepository
    {
        private const string SELECT_COLUMNS = @"
            id         AS Id,
            name       AS Name,
            kind       AS Kind,
            parent_id  AS ParentId,
            icon_name  AS IconName,
            sort_order AS SortOrder,
            is_system  AS IsSystem";

        private readonly EasyMoneyDb m_Db;

        public SqliteCategoryRepository(EasyMoneyDb oDb)
        {
            m_Db = oDb;
        }

        public List<Category> GetAll()
        {
            return m_Db.Connection.Query<Category>(
                $"SELECT {SELECT_COLUMNS} FROM category ORDER BY kind, sort_order, id");
        }

        public List<Category> GetByKind(CategoryKind oKind)
        {
            return m_Db.Connection.Query<Category>(
                $"SELECT {SELECT_COLUMNS} FROM category WHERE kind = ? ORDER BY sort_order, id", (int)oKind);
        }

        public List<Category> GetChildren(int iParentId)
        {
            return m_Db.Connection.Query<Category>(
                $"SELECT {SELECT_COLUMNS} FROM category WHERE parent_id = ? ORDER BY sort_order, id", iParentId);
        }

        public Category GetById(int iId)
        {
            List<Category> lFound = m_Db.Connection.Query<Category>(
                $"SELECT {SELECT_COLUMNS} FROM category WHERE id = ? LIMIT 1", iId);

            return lFound.Count > 0 ? lFound[0] : null;
        }

        public int Insert(Category oCategory)
        {
            m_Db.Connection.Execute(
                @"INSERT INTO category (name, kind, parent_id, icon_name, sort_order, is_system)
                  VALUES (?, ?, ?, ?, ?, ?)",
                oCategory.Name,
                (int)oCategory.Kind,
                oCategory.ParentId,
                oCategory.IconName,
                oCategory.SortOrder,
                oCategory.IsSystem ? 1 : 0);

            oCategory.Id = m_Db.Connection.ExecuteScalar<int>("SELECT last_insert_rowid()");
            return oCategory.Id;
        }

        public void Update(Category oCategory)
        {
            m_Db.Connection.Execute(
                @"UPDATE category
                     SET name = ?, kind = ?, parent_id = ?, icon_name = ?, sort_order = ?, is_system = ?
                   WHERE id = ?",
                oCategory.Name,
                (int)oCategory.Kind,
                oCategory.ParentId,
                oCategory.IconName,
                oCategory.SortOrder,
                oCategory.IsSystem ? 1 : 0,
                oCategory.Id);
        }

        public bool Delete(int iId)
        {
            Category oCategory = GetById(iId);
            if (oCategory == null || oCategory.IsSystem)
            {
                return false;
            }

            if (GetChildren(iId).Count > 0)
            {
                return false;
            }

            m_Db.Connection.Execute("DELETE FROM category WHERE id = ?", iId);
            return true;
        }

        public int CountAll()
        {
            return m_Db.Connection.ExecuteScalar<int>("SELECT COUNT(*) FROM category");
        }
    }
}
