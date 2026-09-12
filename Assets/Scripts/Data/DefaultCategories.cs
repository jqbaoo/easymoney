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
        public static List<Category> Build()
        {
            List<Category> lResult = new List<Category>();

            lResult.AddRange(_buildExpense());
            lResult.AddRange(_buildIncome());

            return lResult;
        }

        private static List<Category> _buildExpense()
        {
            string[] lNames = { "餐饮", "购物", "交通", "住房", "娱乐", "医疗", "学习", "通讯", "人情", "其他" };
            return _build(lNames, CategoryKind.Expense, 1000);
        }

        private static List<Category> _buildIncome()
        {
            string[] lNames = { "工资", "奖金", "兼职", "投资收益", "红包", "其他" };
            return _build(lNames, CategoryKind.Income, 2000);
        }

        private static List<Category> _build(string[] lNames, CategoryKind oKind, int iBaseSortOrder)
        {
            List<Category> lResult = new List<Category>();

            for (int i = 0; i < lNames.Length; i++)
            {
                lResult.Add(new Category
                {
                    Name = lNames[i],
                    Kind = oKind,
                    ParentId = 0,
                    IconName = string.Empty,
                    SortOrder = iBaseSortOrder + (i + 1) * 10,
                    IsSystem = true
                });
            }

            return lResult;
        }
    }
}
