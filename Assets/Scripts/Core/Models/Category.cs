using System.Collections.Generic;

namespace EasyMoney.Core
{
    public class Category
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public CategoryKind Kind { get; set; } = CategoryKind.Expense;

        /// <summary>父分类 Id，0 表示顶级分类。</summary>
        public int ParentId { get; set; }

        public string IconName { get; set; } = string.Empty;

        public int SortOrder { get; set; }

        /// <summary>系统预置分类不允许删除。</summary>
        public bool IsSystem { get; set; }

        public bool IsTopLevel => ParentId == 0;

        /// <summary>
        /// 在一组分类里按 Id 找，找不到返回 null。
        ///
        /// 账单投影和报表聚合都要做这件事，两处各写一遍迟早会分头改歪；
        /// 找不到时的**兜底文案**由调用方各自决定（现在两处都是「未分类」，
        /// 但那是各自的展示约定，不该塞进查找里）。
        /// </summary>
        public static Category FindById(IList<Category> lCategories, int iId)
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
