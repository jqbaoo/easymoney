using System.Collections.Generic;

namespace EasyMoney.Data
{
    /// <summary>
    /// 全部结构迁移，按版本升序。**加一版结构改动时改的就是这里。**
    ///
    /// 三步，缺一不可：
    ///   1. 往 <see cref="ALL"/> 追加一条 <see cref="SchemaMigration"/>：
    ///      Version 填新版本号，Statements 填把这个版本要做的变更
    ///   2. 把 <see cref="EasyMoneyDb.SCHEMA_VERSION"/> 加一
    ///   3. 同步改 <see cref="Schema"/> 的建表语句，让**全新安装**直接建出最新结构
    ///
    /// 第 2 步忘了会被 SchemaMigrationTests 拦住（迁移清单里有比 SCHEMA_VERSION
    /// 更新的版本 = 它永远不会被执行，且只有升级上来的老库会缺列）。
    ///
    /// 注意迁移语句要写成**对已经存在的库**生效的形式：新装的 App 走的是
    /// Schema.cs 的建表语句，根本不经过这里，两条路得各自都能到达同一个结构。
    ///
    /// 注意「结构」不只指表结构——补数据也算。判据是「新装的库天然就对，老库不对」，
    /// 那就只能靠迁移把它拉到同一条线上。
    /// </summary>
    public static class SchemaMigrations
    {
        /// <summary>
        /// v2：给预置分类补图标名。
        ///
        /// 第一版 <see cref="DefaultCategories"/> 里 icon_name 填的是空字符串，
        /// 于是已经装在真机上的库存着一批没有图标名的分类，界面只能显示纯文字。
        /// 新装的库走 DefaultCategories，天然带图标名；老库只能靠这条语句补。
        ///
        /// 分类名和图标名在这里**写死，不复用 DefaultCategories**：迁移是
        /// 「v1 升到 v2 那一刻该做什么」的历史快照，引用会随代码变的常量，
        /// 等于让历史迁移的行为跟着未来代码漂移。将来加新分类是 DefaultCategories
        /// 的事，不该回头改这条 SQL。
        ///
        /// 三个条件都是刻意的，缺一不可：
        ///   is_system = 1   挡住用户自建的同名分类（用户建的「餐饮」不该被改成预置图标）
        ///   icon_name = ''  已经有图标名的不覆盖
        ///   ELSE icon_name  不在清单里的分类保持原值
        /// </summary>
        private const string BACKFILL_CATEGORY_ICONS = @"
UPDATE category
   SET icon_name = CASE name
       WHEN '餐饮'     THEN 'cat_food'
       WHEN '购物'     THEN 'cat_shopping'
       WHEN '交通'     THEN 'cat_transport'
       WHEN '住房'     THEN 'cat_housing'
       WHEN '娱乐'     THEN 'cat_entertainment'
       WHEN '医疗'     THEN 'cat_medical'
       WHEN '学习'     THEN 'cat_education'
       WHEN '通讯'     THEN 'cat_communication'
       WHEN '人情'     THEN 'cat_social'
       WHEN '其他'     THEN 'cat_other'
       WHEN '工资'     THEN 'cat_salary'
       WHEN '奖金'     THEN 'cat_bonus'
       WHEN '兼职'     THEN 'cat_parttime'
       WHEN '投资收益' THEN 'cat_investment'
       WHEN '红包'     THEN 'cat_redpacket'
       ELSE icon_name
   END
 WHERE is_system = 1 AND icon_name = ''";

        public static readonly IReadOnlyList<SchemaMigration> ALL = new[]
        {
            new SchemaMigration
            {
                Version = 2,
                Statements = new[] { BACKFILL_CATEGORY_ICONS }
            }
        };
    }
}
