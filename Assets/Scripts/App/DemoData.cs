using System.Collections.Generic;

namespace EasyMoney.App
{
    /// <summary>
    /// 原型阶段的假数据，只为把布局撑出真实观感。
    /// 接上数据层后整个文件删掉，页面改为从 AppContext 取数。
    /// </summary>
    public static class DemoData
    {
        public const string TOTAL_ASSETS = "964.50";

        public const string MONTH_LABEL = "2026年9月";

        public const string SUMMARY_INCOME = "8000.00";
        public const string SUMMARY_EXPENSE = "235.50";
        public const string SUMMARY_NET = "7764.50";

        public sealed class DemoAccount
        {
            public string Name { get; set; }

            public string TypeLabel { get; set; }

            public string Balance { get; set; }

            public bool IsNegative { get; set; }
        }

        public sealed class DemoTx
        {
            public string Title { get; set; }

            public string Subtitle { get; set; }

            public string Amount { get; set; }

            /// <summary>0=支出 1=收入 2=转账</summary>
            public int Kind { get; set; }
        }

        public sealed class DemoDay
        {
            public string DateLabel { get; set; }

            public List<DemoTx> Items { get; set; } = new List<DemoTx>();
        }

        public sealed class DemoCategory
        {
            public string Name { get; set; }

            public string Amount { get; set; }

            public float Ratio { get; set; }
        }

        public static List<DemoAccount> BuildAccounts()
        {
            return new List<DemoAccount>
            {
                new DemoAccount { Name = "现金", TypeLabel = "现金账户", Balance = "764.50" },
                new DemoAccount { Name = "支付宝", TypeLabel = "支付宝账户", Balance = "200.00" },
                new DemoAccount { Name = "招商银行", TypeLabel = "银行卡", Balance = "12,480.00" }
            };
        }

        public static List<DemoDay> BuildDays()
        {
            return new List<DemoDay>
            {
                new DemoDay
                {
                    DateLabel = "9月11日 周四",
                    Items = new List<DemoTx>
                    {
                        new DemoTx { Title = "午饭", Subtitle = "餐饮 · 现金", Amount = "-35.50", Kind = 0 },
                        new DemoTx { Title = "地铁", Subtitle = "交通 · 支付宝", Amount = "-6.00", Kind = 0 }
                    }
                },
                new DemoDay
                {
                    DateLabel = "9月10日 周三",
                    Items = new List<DemoTx>
                    {
                        new DemoTx { Title = "八月工资", Subtitle = "工资 · 招商银行", Amount = "+8000.00", Kind = 1 },
                        new DemoTx { Title = "超市采购", Subtitle = "购物 · 支付宝", Amount = "-128.00", Kind = 0 },
                        new DemoTx { Title = "转账", Subtitle = "现金 → 支付宝", Amount = "200.00", Kind = 2 }
                    }
                },
                new DemoDay
                {
                    DateLabel = "9月8日 周一",
                    Items = new List<DemoTx>
                    {
                        new DemoTx { Title = "买书", Subtitle = "学习 · 招商银行", Amount = "-66.00", Kind = 0 }
                    }
                }
            };
        }

        public static List<DemoCategory> BuildExpenseCategories()
        {
            return new List<DemoCategory>
            {
                new DemoCategory { Name = "购物", Amount = "128.00", Ratio = 0.543f },
                new DemoCategory { Name = "学习", Amount = "66.00", Ratio = 0.280f },
                new DemoCategory { Name = "餐饮", Amount = "35.50", Ratio = 0.151f },
                new DemoCategory { Name = "交通", Amount = "6.00", Ratio = 0.026f }
            };
        }

        public static List<DemoCategory> BuildIncomeCategories()
        {
            return new List<DemoCategory>
            {
                new DemoCategory { Name = "工资", Amount = "8000.00", Ratio = 1.0f }
            };
        }

        public static readonly string[] ExpenseCategoryNames =
            { "餐饮", "购物", "交通", "住房", "娱乐", "医疗", "学习", "通讯" };

        public static readonly string[] IncomeCategoryNames =
            { "工资", "奖金", "兼职", "投资收益", "红包" };
    }
}
