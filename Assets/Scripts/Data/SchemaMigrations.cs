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
    /// 现在还是空的：第一版之后还没发生过结构变更。这个清单先立在这里，
    /// 是为了第一次改结构时有个明确的落点，而不是临时去想迁移该写在哪。
    /// </summary>
    public static class SchemaMigrations
    {
        public static readonly IReadOnlyList<SchemaMigration> ALL = new SchemaMigration[0];
    }
}
