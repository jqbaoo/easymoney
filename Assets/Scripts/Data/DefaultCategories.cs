using System.Collections.Generic;
using EasyMoney.Core;

namespace EasyMoney.Data
{
    /// <summary>
    /// 首次建库时写入的预置分类。全部标记 IsSystem=true，用户不可删除。
    /// SortOrder 决定界面展示顺序，取值 10/20/30... 留出插空余地。
    /// </summary>
    public static class DefaultCategories
    {
        // 图标名在这里只能写字面量，不能引用 App 层的 IconNames.CAT_*：
        // 依赖方向是 Core ← Data ← App，Data 层引用 App 是反向依赖。
        // 下面的字符串与 IconNames 里的同名常量一一对应，改一边就得改另一边。
        //
        // 之所以不像尺寸字号那样必须来自代码常量：图标名写进数据库后就成了**数据**，
        // 将来「分类管理」页会让用户自己改，代码管不着。

        private static readonly (string Name, string Icon)[] EXPENSE_CATEGORIES =
        {
            ("餐饮", "cat_food"),
            ("购物", "cat_shopping"),
            ("交通", "cat_transport"),
            ("住房", "cat_housing"),
            ("娱乐", "cat_entertainment"),
            ("医疗", "cat_medical"),
            ("学习", "cat_education"),
            ("通讯", "cat_communication"),
            ("人情", "cat_social"),

            // 支出与收入各有一个「其他」，共用同一个图标，靠 kind 区分
            ("其他", "cat_other")
        };

        private static readonly (string Name, string Icon)[] INCOME_CATEGORIES =
        {
            ("工资", "cat_salary"),
            ("奖金", "cat_bonus"),
            ("兼职", "cat_parttime"),
            ("投资收益", "cat_investment"),
            ("红包", "cat_redpacket"),
            ("其他", "cat_other")
        };

        public static List<Category> Build()
        {
            List<Category> lResult = new List<Category>();

            lResult.AddRange(_build(EXPENSE_CATEGORIES, CategoryKind.Expense, 1000));
            lResult.AddRange(_build(INCOME_CATEGORIES, CategoryKind.Income, 2000));

            return lResult;
        }

        private static List<Category> _build(
            (string Name, string Icon)[] lEntries, CategoryKind oKind, int iBaseSortOrder)
        {
            List<Category> lResult = new List<Category>();

            for (int i = 0; i < lEntries.Length; i++)
            {
                lResult.Add(new Category
                {
                    Name = lEntries[i].Name,
                    Kind = oKind,
                    ParentId = 0,
                    IconName = lEntries[i].Icon,
                    SortOrder = iBaseSortOrder + (i + 1) * 10,
                    IsSystem = true
                });
            }

            return lResult;
        }
    }
}
