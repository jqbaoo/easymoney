using System.Collections.Generic;
using EasyMoney.Core;

namespace EasyMoney.Data
{
    public interface ICategoryRepository
    {
        List<Category> GetAll();

        List<Category> GetByKind(CategoryKind oKind);

        List<Category> GetChildren(int iParentId);

        Category GetById(int iId);

        int Insert(Category oCategory);

        void Update(Category oCategory);

        bool Delete(int iId);

        int CountAll();
    }
}
