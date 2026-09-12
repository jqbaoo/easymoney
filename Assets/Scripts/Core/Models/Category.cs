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
    }
}
