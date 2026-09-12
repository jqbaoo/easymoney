# EasyMoney 规格说明

> 本文件是项目的**单一事实来源**。AI 助手或新加入的人，读这一份就能上手。
> 实施计划见 `Claude/plans/2026-09-11-账单管理MVP.md`（7300 行，含每个任务的具体代码）。
> 需求清单见 `Claude/账单管理系统功能清单.md`。

---

## 1. 项目是什么

一个 **Android 本地记账 App**。单机、无账号、无网络，所有数据存在手机本地的 SQLite 里。
目标是可以日常使用，不是玩具 demo。

**MVP 覆盖五个模块**：账户管理、账单记录、分类管理、搜索筛选、基础报表。

---

## 2. 当前状态（2026-09-13）

**这是最重要的一节。** **MVP 已完工**——四个页面全部接真实数据，APK 已在真机上完成验收。

```
Core 层   ██████████ 100%   Money / TimeUtil / 模型 / 校验 / 报表 / 账单展示投影 / 账户表单 / 报表展示（Task 2-3、5、7、9-11、14-16）
Data 层   ██████████ 100%   SQLite / 三个仓储 / 记账服务 / 筛选（Task 4、6-11）
App 层    ██████████ 100%   记账页 / 账单页 / 账户页 / 报表页全部通真实数据
测试      ██████████ 100%   209 个用例全绿（Money 7 + Parser 17 + TimeUtil 12 + 建表 4 + 冒烟 1 + 构建配置 1 + 模型 4 + 分类仓储 13 + 账户仓储 12 + 账单仓储 10 + 校验 17 + 记账服务 10 + 筛选 18 + 报表 13 + 应用容器 7 + 快捷金额 6 + 界面工厂布局 3 + 账单展示 20 + 账户表单 13 + 报表展示 12 + 构建配置守卫 9）
打包      ██████████ 100%   APK 已构建，Redmi K60 真机验收 18 项中 17 项通过
```

### 已完成

| 内容 | 文件 | 说明 |
|---|---|---|
| UI 基础设施 | `App/Scripts/App/UI/*.cs` | Theme / UiFactory / SpriteFactory / FontProvider / SafeAreaFitter / PageBase / PageRouter / TabBar |
| **资源层** | `App/UI/AssetPaths.cs`、`AssetProvider.cs`、`IconNames.cs`、`ThemePalette.cs` | 计划外新增，见第 7 节 |
| 应用入口 | `App/AppRoot.cs` | 含 `RuntimeInitializeOnLoadMethod`，打开任意场景点 Play 即可运行 |
| 四个页面 | `App/UI/Pages/*.cs` | 记账页 / 账单列表页 / 账户页 / 报表页，**全部接真实数据** |
| **程序集骨架 + 测试链路** | `Scripts/Core`、`Scripts/Data`、`Tests/EditMode`、`Tools/run-editmode-tests.sh` | Task 1 产出。命令行跑 EditMode 测试已实测可用 |
| **金额值类型** | `Scripts/Core/Money.cs`、`MoneyParser.cs` | Task 2 产出。`readonly struct` 内部存「分」+ 输入解析，25 个用例全绿 |
| **时间工具** | `Scripts/Core/TimeUtil.cs` | Task 3 产出。Unix 毫秒互转 + 月/日边界，7 个用例 |
| **SQLite 接入 + 建表** | `Scripts/Data/EasyMoneyDb.cs`、`Schema.cs`、`Plugins/SQLite/link.xml` | Task 4 产出。依赖已升到 3.x（sqlite-net-pcl 1.11.285 + SQLitePCLRaw 3.0.3 + SourceGear.sqlite3 3.53.4），三张表 + 5 个索引 |
| **领域模型与枚举** | `Scripts/Core/Models/Enums.cs`、`Account.cs`、`Category.cs`、`Transaction.cs` | Task 5 产出。纯 POCO，**不带 SQLite 特性标注**，Data 层用手写 SQL + 列别名映射；枚举数值由 `ModelTests` 锁死 |
| **分类仓储 + 默认分类种子** | `Scripts/Data/DefaultCategories.cs`、`ICategoryRepository.cs`、`SqliteCategoryRepository.cs` | Task 6 产出。预置 10 个支出 + 6 个收入分类；`Open()` 按「category 表为空」幂等写入 |
| **账户仓储 + 实时余额聚合** | `Scripts/Data/IAccountRepository.cs`、`SqliteAccountRepository.cs`、`Scripts/Core/Models/AccountBalance.cs` | Task 7 产出。余额用相关子查询实时算，**不冗余存储**；同时补了账单仓储的最小形态（`Query` 的筛选待 Task 10） |
| **账单仓储 CRUD** | `Scripts/Data/SqliteTransactionRepository.cs` | Task 8 产出。字段往返、枚举映射、边界值共 10 个用例，均针对 Task 7 已写好的实现——本任务是计划里唯一「先实现后补测试」的一个 |
| **校验规则 + 记账服务** | `Scripts/Core/ValidationResult.cs`、`TransactionValidator.cs`、`Scripts/Data/TransactionService.cs` | Task 9 产出。校验放 Core（纯函数），编排放 Data；转账只写一条记录。附带把 `Query` 从抛异常改成时间倒序分页的最简实现（筛选留给 Task 10） |
| **账单筛选与搜索** | `Scripts/Core/Queries/TransactionQuery.cs`、`Scripts/Data/SqliteTransactionRepository.cs` | Task 10 产出。8 个维度：时间区间（左闭右开）、类型、分类、账户（含「是否连转入一起看」开关）、金额区间、关键词。`Count()` 忽略 Limit/Offset 供分页算总数 |
| **报表汇总与分类占比** | `Scripts/Core/Reports/*.cs` | Task 11 产出。`PeriodSummary` / `CategoryBreakdownItem` / `ReportCalculator`，纯函数。**转账不进收支统计**；分类被删时兜底「未分类」；占比分母为 0 时不除 |
| **应用容器** | `Scripts/App/AppContext.cs` | Task 12 产出。单例依赖容器，持有 `EasyMoneyDb` 与三个仓储、`TransactionService`；`AppRoot` 打开 `persistentDataPath` 下的 `easymoney.db`，订阅 `DataChanged` 刷新当前页 |
| **记账页** | `App/UI/Pages/RecordPage.cs`、`App/UI/PickerDialog.cs`、`Core/QuickAmountHelper.cs` | Task 13 产出。类型切换 / 金额 / 快捷金额 / 分类 / 账户 / 转入 / 日期 / 备注，保存走 `TransactionService.Save()`；进页面预选第一个账户与分类，通常只需填金额。选择弹窗由四个入口共用 |
| **账单列表页** | `App/UI/Pages/TransactionListPage.cs`、`Core/Statements/*.cs` | Task 14 产出。按月查看 + 收支汇总 + 按天分组 + 删除。**展示规则抽在 Core 的 `StatementBuilder` 里**（本地日期分组、金额正负号、名称兜底），页面只做取数与渲染 |
| **账户管理页** | `App/UI/Pages/AccountPage.cs`、`App/UI/AccountEditDialog.cs`、`Core/Accounts/*.cs` | Task 15 产出。总资产 + 各账户实时余额 + 添加 / 编辑 / 归档 / 恢复。**表单规则抽在 Core 的 `AccountForm` 里**（名称去空白后非空、余额留空按 0、允许负数），新建与编辑共用一套弹窗。比计划多做了「显示已归档」开关——计划里的归档是单向的，点错一次就找不回来 |
| **报表页** | `App/UI/Pages/ReportPage.cs`、`Core/Reports/ReportForm.cs` | Task 16 产出。月份切换 + 收支汇总 + 支出/收入分类占比条形图。**展示规则抽在 Core 的 `ReportForm` 里**（构成标题、占比文案、条形宽度钳位），月份格式统一走 `TimeUtil.FormatYearMonth`。`DemoData.cs` 已随之删除 |

### 全部完成

计划里的 17 个任务全部完成。Task 17 的验收细节、未通过项与遗留问题见
**`Claude/plans/android-release-checklist.md`**（该文档同时记录了本次验收的可信度边界——
验收是用户在真机上手动做的，没有 adb 日志佐证）。

### 关键判断

计划的 Task 12-16 是「UI 基础设施 + 四个页面」，现在五个都已完工：

- **四个页面全是真的**：建账户 → 记账 → 账单页能看到、账户页余额跟着变、报表页按分类
  算出占比，删除与归档都直接作用在库里
- **`DemoData.cs` 已删除**（Task 16）。它撑着的是报表页，报表页一接真数据就没有调用方了。
  最后一个假数据源消失，界面上看到的每个数字都来自 SQLite
- Task 15 顺带修掉一个 bug：`AccountPage` 的「添加账户」按钮 `onClick` 传的是 `null`，
  而列表数据来自 `DemoData`（假账户），库里账户数为 0——所以记账页保存时一律被
  `TransactionValidator` 拒掉，报「请选择账户」。这个 bug 在视觉原型阶段看不出来：
  界面有账户显示，只是点了没反应
- `Core` / `Data` 两个程序集都已有 `.cs`，`Library/ScriptAssemblies/` 下
  `EasyMoney.Core.dll` 与 `EasyMoney.Data.dll` 均已生成
- **整个业务逻辑层已经完工**（Task 1-11，打有标签 `data-layer-complete`）。
  记账、筛选、报表的计算结果都被测试验证过了
- **容器层也已完工**（Task 12）。`AppContext` 把库和仓储装配好，端到端链路
  「建账户 → 记账 → 余额正确 → 查得到记录」有测试锁住
- **每个页面都配了一个 Core 纯逻辑模块**：账单页 → `StatementBuilder`，
  账户页 → `AccountForm`，报表页 → `ReportForm`。页面只剩取数与渲染，
  「一条账单怎么显示」「占比保留几位小数」这类约定都挪进了能被测试盯住的地方

**Task 17 已完成**——APK 构建成功并在 Redmi K60 上过了验收，详见
`Claude/plans/android-release-checklist.md`。三个值得记住的结论：

- **最高风险点（SQLite 原生库）确认解除**：拆开 APK 实测，
  `lib/arm64-v8a/libe_sqlite3.so`（1.77 MB）与 `lib/armeabi-v7a/libe_sqlite3.so`（1.25 MB）
  都在包里。此前所有关于「原生库缺失」的担心到此为止
- **打包配置现在有测试守着**：`AndroidPlayerSettingsTests`（9 个用例）把 Task 17 Step 2
  那张配置表翻译成了断言。配置对不对不用再靠肉眼核对 Unity 面板，跑测试就知道
- **还有两处已知瑕疵**，都不影响功能，记在 `android-release-checklist.md` 里：
  APK 里仍有 `android.permission.INTERNET`（UnityWebRequest 模块带的，自定义 manifest
  才能去掉）；备注里的 emoji 显示为空白（字体没有 emoji 字形）

---

## 3. 技术栈

| 项 | 值 |
|---|---|
| Unity | 2022.3.53f1c1（中国版） |
| Unity 可执行文件 | `D:\unity\unity2022\2022.3.53f1c1\Editor\Unity.exe` |
| 模板 | 2D |
| 目标平台 | Android，IL2CPP |
| UI | UGUI（`UnityEngine.UI`），**全部代码构建，不用 Prefab** |
| 数据库 | SQLite（sqlite-net-pcl 1.11.285 + SQLitePCLRaw 3.0.3 + SourceGear.sqlite3 3.53.4） |
| 测试 | Unity Test Framework（NUnit，EditMode） |

### 为什么不用 Prefab

Prefab 和场景文件是二进制/YAML，无法用代码可靠地描述、diff 和 review。
记账界面的结构高度重复（列表项、表单行），代码构建可以批量改样式，
也让计划里的每一步都能被真正执行和验证。

**这条是硬约束。任何界面都必须用 `UiFactory` 在代码里搭。**

---

## 4. 目录结构

```
easymoney/
├── SPEC.md                      ← 本文件
├── CLAUDE.md                    ← AI 助手入口（指向本文件）
├── Assets/
│   ├── Resources/               资源（美术换图只碰这里，见第 7 节）
│   │   ├── Icons/               图标
│   │   ├── Sprites/             卡片 / 按钮九宫格底图
│   │   ├── Fonts/               字体
│   │   └── theme.json           配色
│   ├── Scenes/                  场景（原型阶段用不到）
│   ├── Scripts/
│   │   ├── Core/                ✅ Money / MoneyParser / TimeUtil / Models / Queries / ValidationResult / TransactionValidator / Reports / Statements / Accounts（noEngineReferences）
│   │   ├── Data/                ✅ EasyMoneyDb / Schema / 三个仓储 / TransactionService / 账单筛选
│   │   └── App/                 ✅ 已有
│   │       ├── EasyMoney.App.asmdef
│   │       ├── AppContext.cs    ✅ 单例依赖容器（Task 12）
│   │       ├── AppRoot.cs       启动入口，建库 + 搭界面 + 装配路由
│   │       └── UI/
│   │           ├── Theme.cs / ThemePalette.cs
│   │           ├── UiFactory.cs
│   │           ├── SpriteFactory.cs / FontProvider.cs
│   │           ├── AssetPaths.cs / AssetProvider.cs / IconNames.cs
│   │           ├── SafeAreaFitter.cs
│   │           ├── PageBase.cs / PageRouter.cs / TabBar.cs
│   │           ├── PickerDialog.cs     通用选择弹窗（分类 / 账户 / 日期共用）
│   │           ├── AccountEditDialog.cs 账户新建 / 编辑弹窗（Task 15）
│   │           └── Pages/       RecordPage / TransactionListPage / AccountPage / ReportPage
│   └── Tests/EditMode/          ✅ 测试程序集 + 冒烟测试
├── Tools/                       ✅ run-editmode-tests.sh
└── Claude/
    ├── 账单管理系统功能清单.md   需求来源
    ├── 资源替换指南.md           美术/设计换资源指南
    └── plans/
        └── 2026-09-11-账单管理MVP.md   实施计划（17 个任务）
```

**Task 1 已把三层 asmdef、测试程序集、命令行脚本建好**，进度见第 2 节。

---

## 5. 架构分层

```plantuml
@startuml
skinparam packageStyle rectangle
skinparam classAttributeIconSize 0

package "EasyMoney.Core  (noEngineReferences: true)" #E8F5E9 {
  class Money {
    +long Cents
    +{static} Money FromCents(long)
    +{static} Money FromYuan(decimal)
    +{static} bool TryParseYuan(string, out Money)
  }
  class TimeUtil {
    +{static} long ToUnixMs(DateTime)
    +{static} DateTime FromUnixMs(long)
    +{static} long StartOfDayMs(DateTime)
    +{static} long StartOfMonthMs(int, int)
    +{static} long StartOfNextMonthMs(int, int)
    +{static} (int,int) CurrentYearMonth()
    +{static} string FormatYearMonth(int, int)
    +{static} (int,int) AddMonths(int, int, int)
  }
  class Account
  class Category
  class Transaction
  class AccountBalance {
    +Account Account
    +Money Balance
  }
  class TransactionQuery {
    +long? StartMs
    +long? EndMs
    +TxType? Type
    +int? CategoryId
    +int? AccountId
    +bool AccountIncludesTransfers
    +long? MinCents
    +long? MaxCents
    +string Keyword
    +int Limit
    +int Offset
  }
  class ValidationResult {
    +bool IsValid
    +string ErrorMessage
    +{static} ValidationResult Ok()
    +{static} ValidationResult Fail(string)
  }
  class TransactionValidator {
    +{static} long MaxAmountCents
    +{static} ValidationResult Validate(Transaction, IList<Account>, IList<Category>)
  }
  class PeriodSummary {
    +Money Income
    +Money Expense
    +Money Net
    +int TxCount
  }
  class CategoryBreakdownItem {
    +int CategoryId
    +string CategoryName
    +Money Total
    +int TxCount
    +decimal Ratio
  }
  class ReportCalculator {
    +{static} PeriodSummary BuildSummary(IEnumerable<Transaction>)
    +{static} List<CategoryBreakdownItem> BuildBreakdown(IEnumerable<Transaction>, TxType, IList<Category>)
    +{static} void AssignRatios(List<CategoryBreakdownItem>)
  }
  class StatementBuilder {
    +{static} List<StatementDay> BuildDays(IEnumerable<Transaction>, IList<Category>, IList<Account>)
    +{static} StatementRow BuildRow(Transaction, IList<Category>, IList<Account>)
    +{static} string FormatDayLabel(DateTime)
  }
  class StatementDay {
    +DateTime Date
    +string DateLabel
    +List<StatementRow> Items
  }
  class StatementRow {
    +Transaction Transaction
    +string Title
    +string Subtitle
    +string AmountText
    +TxType Type
  }
  class AccountForm {
    +{static} string TypeLabel(AccountType)
    +{static} AccountFormResult Validate(string, string)
  }
  class AccountFormResult {
    +bool IsValid
    +string ErrorMessage
    +string Name
    +long InitialBalanceCents
    +{static} AccountFormResult Ok(string, long)
    +{static} AccountFormResult Fail(string)
  }
  class ReportForm {
    +{static} string BreakdownTitle(TxType)
    +{static} string BreakdownValueText(Money, decimal)
    +{static} decimal BarWidthRatio(decimal)
  }
  enum TxType { Expense=0, Income=1, Transfer=2 }
  enum CategoryKind { Expense=0, Income=1 }
  enum AccountType { Cash=0, BankCard=1, Alipay=2, WeChat=3, Other=4 }
}

package "EasyMoney.Data" #E3F2FD {
  class EasyMoneyDb {
    +Connection
    +Open()
    +Close()
    +Dispose()
  }
  class DefaultCategories
  interface IAccountRepository
  interface ICategoryRepository
  interface ITransactionRepository
  class SqliteAccountRepository
  class SqliteCategoryRepository
  class SqliteTransactionRepository
  class TransactionService {
    +ValidationResult Save(Transaction, long)
    +ValidationResult CreateTransfer(int, int, long, string, long, long)
  }
}

package "EasyMoney.App" #FFF3E0 {
  class AppContext {
    +{static} AppContext Instance
    +EasyMoneyDb Db
    +IAccountRepository Accounts
    +ICategoryRepository Categories
    +ITransactionRepository Transactions
    +TransactionService TxService
    +void Initialize(string)
    +void NotifyDataChanged()
    +event Action DataChanged
  }
  class AppRoot
  class PageBase
  class PageRouter
  class TabBar
  class PickerDialog
  class UiFactory
  class Theme
  class AssetProvider
  class RecordPage
  class TransactionListPage
  class AccountPage
  class AccountEditDialog
  class ReportPage
}

package "Assets/Resources" #F3E5F5 {
  folder "Icons / Sprites / Fonts / theme.json"
}

Core <-- Data
Data <-- App
TransactionService ..> TransactionValidator : 校验规则在 Core
App --> "Assets/Resources" : AssetProvider 是唯一入口

AppContext --> EasyMoneyDb
AppContext --> IAccountRepository
AppContext --> ICategoryRepository
AppContext --> ITransactionRepository
AppContext --> TransactionService
AppRoot --> AppContext
AppRoot --> PageRouter
PageBase <|-- RecordPage
PageBase <|-- TransactionListPage
PageBase <|-- AccountPage
PageBase <|-- ReportPage
RecordPage ..> UiFactory
TransactionListPage ..> UiFactory
TransactionListPage ..> StatementBuilder : 分组与文案规则
AccountPage ..> UiFactory
AccountPage ..> AccountEditDialog : 添加 / 编辑
AccountEditDialog ..> AccountForm : 表单规则
AccountEditDialog ..> PickerDialog : 选类型
ReportPage ..> ReportForm : 占比文案与条形宽度
ReportPage ..> UiFactory
UiFactory ..> Theme
UiFactory ..> AssetProvider
UiFactory ..> SpriteFactory
UiFactory ..> FontProvider

note bottom of AppContext
  页面唯一的取数入口。
  数据变了调 NotifyDataChanged()，
  页面订阅 DataChanged 后刷新。
  接数据层时只改各页面的 _refresh()。
end note

@enduml
```

### 依赖方向是单向的

```
Core  ←  Data  ←  App
```

- **Core**：`noEngineReferences: true`，编译器强制不许 `using UnityEngine`。纯 C#，测试跑得飞快
- **Data**：只用 Core，不碰 Unity UI
- **App**：唯一能碰 UnityEngine 和 UI 的层

**不许反向依赖。** Core 里出现 `UnityEngine` 就是 bug。

---

## 6. 数据模型

### 三张表

表名用 `tx` 而不是 `transaction`——后者是 SQL 保留字。

```sql
CREATE TABLE account (
    id              INTEGER PRIMARY KEY AUTOINCREMENT,
    name            TEXT    NOT NULL,
    type            INTEGER NOT NULL DEFAULT 0,   -- AccountType
    initial_balance INTEGER NOT NULL DEFAULT 0,   -- 分
    is_archived     INTEGER NOT NULL DEFAULT 0,
    sort_order      INTEGER NOT NULL DEFAULT 0,
    created_at      INTEGER NOT NULL DEFAULT 0    -- Unix 毫秒
);

CREATE TABLE category (
    id         INTEGER PRIMARY KEY AUTOINCREMENT,
    name       TEXT    NOT NULL,
    kind       INTEGER NOT NULL DEFAULT 0,        -- CategoryKind
    parent_id  INTEGER NOT NULL DEFAULT 0,        -- 0 = 顶级
    icon_name  TEXT    NOT NULL DEFAULT '',       -- 对应 Resources/Icons/<名字>.png
    sort_order INTEGER NOT NULL DEFAULT 0,
    is_system  INTEGER NOT NULL DEFAULT 0         -- 系统预置，不可删
);

CREATE TABLE tx (
    id            INTEGER PRIMARY KEY AUTOINCREMENT,
    type          INTEGER NOT NULL DEFAULT 0,     -- TxType
    amount        INTEGER NOT NULL DEFAULT 0,     -- 分，恒为正
    account_id    INTEGER NOT NULL DEFAULT 0,     -- 转账时是转出账户
    to_account_id INTEGER NOT NULL DEFAULT 0,     -- 只有转账用
    category_id   INTEGER NOT NULL DEFAULT 0,     -- 转账时为 0
    note          TEXT    NOT NULL DEFAULT '',
    occurred_at   INTEGER NOT NULL DEFAULT 0,     -- 账单发生时间
    created_at    INTEGER NOT NULL DEFAULT 0,
    updated_at    INTEGER NOT NULL DEFAULT 0
);

CREATE INDEX idx_tx_occurred  ON tx(occurred_at);
CREATE INDEX idx_tx_account   ON tx(account_id);
CREATE INDEX idx_tx_toaccount ON tx(to_account_id);
CREATE INDEX idx_tx_category  ON tx(category_id);
CREATE INDEX idx_category_kind ON category(kind);
```

### 三条设计决策（不要推翻）

**① 金额一律 `long` 存「分」，禁止 `float` / `double`。**
浮点数存钱必然出现 0.1 + 0.2 ≠ 0.3 的问题。展示时再转成「元」的字符串。

**② 时间一律 `long` 存 Unix 毫秒（UTC）。**
时区转换只在展示层做。

**③ 转账用单条记录表示，不用两条。**
`type = Transfer, account_id = 转出, to_account_id = 转入`。
单条记录天然原子，不会出现「只改了转出没改转入」的脏数据。

**④ 账户余额不冗余存储。**
用 SQL 相关子查询实时聚合。这样改历史账单不会导致余额对不上，
代价是账户数量多时查询稍慢——记账 App 的账户数量不会超过几十个，可以接受。

---

## 7. 资源层（美术换图用）

**核心理念：资源优先、代码兜底。**

```
Resources 里有图  →  用图
Resources 里没图  →  代码画一个 / 用文字符号顶替
```

于是「美术还没给图」和「美术已经给了图」是**同一份代码**。
把文件按约定名字丢进 `Assets/Resources/`，重启即生效，**零代码改动**。

### 目录与具名资源

| 路径 | 用途 | 缺失时的兜底 |
|---|---|---|
| `Icons/<名字>.png` | 图标 | 文字符号（`<` `>` `+`）或纯文字标签 |
| `Icons/<名字>_on.png` | 标签栏选中态（可选） | 把普通图标染成主色 |
| `Sprites/card.png` | 卡片九宫格底图 | 程序化生成圆角矩形 |
| `Sprites/button.png` | 按钮九宫格底图 | 复用 `card.png` |
| `Fonts/main.ttf` | 主字体 | 系统字体 → Unity 内置字体 |
| `theme.json` | 配色 | 内置浅色主题 |

完整清单（每个图标的文件名、显示尺寸、分类 slug 表、九宫格 border 怎么切）
见 **`Claude/资源替换指南.md`**。

### 代码侧

| 类 | 职责 |
|---|---|
| `AssetProvider` | 全项目**唯一**碰 `Resources.Load` 的地方。找不到返回 `null`，不抛异常、不给占位图 |
| `AssetPaths` | 路径与具名资源常量 |
| `IconNames` | 图标名常量。**不许在页面里写图标名字符串** |
| `ThemePalette` | 配色对象，含 `Light()` / `Dark()` / `FromJson()` |

### 深色模式

代码层面已经就绪：

```csharp
Theme.Apply(ThemePalette.Dark());   // 界面会自动整体重建
```

`AppRoot` 订阅了 `Theme.PaletteChanged`，收到通知后重建整棵界面树
（颜色是构建时写进每个 Graphic 的，重建比维护增量改色逻辑更不容易出错）。

**目前只差设置页里放个开关。** 页面代码不需要任何改动。

---

## 8. 视觉规范

- **设计基准分辨率 750 × 1334**（iPhone 6/7/8，即 375×667pt @2x）
- `CanvasScaler.matchWidthOrHeight = 0`（**按宽度匹配**）
  - 竖屏 App 的宽度恒定、高度随屏幕比例伸缩。用 0.5 会在 18:9 / 20:9 机器上横向拉伸
- 安全区适配：`SafeAreaFitter` 比较 `Screen.safeArea` 与屏幕尺寸，变化才重算

**所有尺寸、字号都是 `Theme.cs` 里的 `const`。**
**所有颜色都通过 `Theme.XXX` 取，页面里不许出现 `new Color(...)` 字面量。**

（本轮改造前页面里散落过 `">"` `"<"` `"+ 添加账户"` 这类硬编码符号，已经全部消除。
发现新的硬编码就该挪进 `Theme` 或 `IconNames`。）

---

## 9. 命名与编码规范（用户强制要求）

| 种类 | 规则 | 例子 |
|---|---|---|
| 私有字段 | `m_` + 大驼峰 | `m_Index`、`m_Data` |
| 全局变量 | `g_` + 大驼峰 | `g_Config` |
| 私有方法 | `_` + 小驼峰 | `_refresh()`、`_buildSkeleton()` |
| 公开方法 | 大驼峰 | `Refresh()`、`Show()` |
| 局部变量 | 类型前缀 + 大驼峰 | 见下 |
| 常量 | 全大写下划线 | `REF_WIDTH`、`TAB_ICON_SIZE` |

**局部变量类型前缀**：`i` int / `s` string / `f` float / `b` bool / `d` dict / `l` list / `t` tuple / `o` 对象

```csharp
foreach ((string sKey, string sIcon, string sLabel) in TABS) { ... }
```

**注释用中文。** 解释「为什么这么做」，不解释「这行在干什么」。

---

## 10. 常用命令

### 跑 EditMode 测试

**必须先关掉 Unity 编辑器**——同一个项目不能同时被两个 Unity 实例打开。

```bash
bash Tools/run-editmode-tests.sh
```

退出码：**0 = 全部通过，1 = 有测试失败，2 = 环境/编译错误**。

脚本内部封装的就是下面这条命令，两个坑都别再踩：

- **不要加 `-quit`。** 实测加上它 Unity 会在跑测试之前就退出，退出码 0 但一个测试都没跑——
  静默失败，比报错更危险。
- **`-testResults` 不要指向 `Temp/`。** Unity 退出时会清理 `Temp/` 目录，结果文件会被删掉
  （实测：文件在 10 秒时写入成功、2.7KB，进程结束后消失），脚本会把它误判成「编译错误」。
  所以产物统一放 `Tools/`，并已在 `.gitignore` 里忽略 `Tools/*.log`、`Tools/*.xml`。

```bash
"/d/unity/unity2022/2022.3.53f1c1/Editor/Unity.exe" \
  -batchmode -nographics \
  -projectPath "E:/Projects/easymoney" \
  -runTests -testPlatform EditMode \
  -testResults "Tools/editmode-results.xml" \
  -logFile "Tools/editmode.log"
```

**覆盖边界（别误读「全绿」）**：EditMode 测试只覆盖 Core 与 Data。App 层的页面、
`PickerDialog`、`AppRoot` 在 EditMode 下**不会被实例化**（界面全部由代码在运行时构建，
不走 `AppRoot.Awake`），所以对它们来说「编译通过」就是测试能给的上限。

唯一的例外是 `UiFactoryLayoutTests`（2026-09-13 加入）。它只 `new GameObject` 摆锚点，
断言锚点能即时决定的尺寸——`rect` 由锚点和父容器当场算出，不需要 Canvas，也不需要
`LayoutRebuilder`，所以 EditMode 下跑得动也不飘。**布局组算出的高度依赖一次真实的
布局重建，不在这里测**，别往里加那类断言。

结论：改完界面代码，仍然必须在 Unity 里点 Play 肉眼确认。

### 构建 Android APK

**同样必须先关掉 Unity 编辑器**（同一个项目不能被两个实例打开）。

```bash
bash Tools/build-android.sh
```

产物 `Builds/EasyMoney.apk`。退出码：**0 = 成功，1 = 构建失败，2 = 环境问题**。

⚠️ 脚本里写死了三件踩过的事，别绕过脚本手敲 `Unity.exe`：

- **日志放 `Tools/build-android.log` 而不是 `Temp/`** —— 理由同测试脚本，
  Unity 退出会清理 `Temp/`
- **构建前先 `rm` 旧 APK** —— 否则这次构建失败了也会因为「文件在」被判成成功，
  正是最该避免的静默失败
- **`-executeMethod` 要写全命名空间**：
  `EasyMoney.App.EditorTools.BuildScript.BuildAndroid`

首次 IL2CPP 构建约 4-5 分钟（实测 4.5 分钟，产物 29 MB）。

### 运行界面

打开 Unity，任意场景点 Play。`AppRoot` 有 `[RuntimeInitializeOnLoadMethod]`，
会自动创建 Canvas，不需要往场景里挂任何东西。

---

## 11. 已知风险

| 风险 | 说明 | 处置 |
|---|---|---|
| **Unity 不内置 SQLite** | Task 4 的最高风险点。需要手工导入 sqlite-net-pcl 及其原生依赖 DLL | ✅ 已解决（Task 4 完成），命令行测试全绿 |
| ~~Android 原生库缺失~~ | `SQLitePCLRaw.lib.e_sqlite3` 2.1.x 全线不含 Android 的 `libe_sqlite3.so`（实测 2.1.2 / 2.1.13 的 nupkg 里只有 linux/osx/win） | ✅ **已解决**。升级到 3.x 依赖树（`sqlite-net-pcl 1.11.285` + `SQLitePCLRaw 3.0.3` + `SourceGear.sqlite3 3.53.4`）。注意 3.x 移除了 `batteries_v2`，`SQLitePCL.Batteries_V2` 类型不复存在，新版 sqlite-net 自己完成 provider 注册 |
| **NuGetForUnity 会静默过滤 runtime** | 它只解压 `ProjectSettings/Packages/com.github-glitchenzo.nugetforunity/NativeRuntimeSettings.json` 里登记过的 runtime，nupkg 里的其余平台**无任何提示地丢弃**。`SourceGear.sqlite3 3.53.4` 含 30 个平台，实际只落地 7 个 | 已补登记 `android-arm` / `android-x86` / `android-x64`，并加 `AndroidBuildConfigTests` 守卫。**以后新增任何带原生库的包，都要先看这个 json** |
| ~~Target Architectures 缺 ARM64~~ | 项目原先 `AndroidTargetArchitectures: 1`，只勾了 ARMv7。纯 32 位不满足 Google Play 的 64 位要求，且新设备陆续移除 32 位兼容层 | ✅ **已解决**。改为 `3`（ARMv7 \| ARM64）。arm64 的原生库本来就在，不用额外装东西；守卫测试现在同时校验 `android-arm` 与 `android-arm64` |
| **Burst AOT Settings 极易与 Target Architectures 混淆** | `Project Settings → Burst AOT Settings` 里的 `ARMV8A` / `ARMV8A_HALFFP` / `ARMV9A` 是 Burst 生成原生代码用的指令集目标，tooltip 却写着 "target architectures to support for the currently selected platform"，看起来像打包架构设置 | 两者无关，Burst 页保持默认即可。真正决定打包哪些 ABI 的是 `PlayerSettings.Android.targetArchitectures` |
| **IL2CPP 代码剥离** | 会删掉 SQLite 靠反射调用的代码，表现为真机上崩 | `link.xml` + 剥离级别设 `Minimal`，双重保护 |
| ~~LIKE 通配符~~ | 用户搜索词里的 `%` `_` 会被当通配符 | ✅ **已解决**（Task 10）。`_escapeLike` 先转义反斜杠、再转义 `%` 和 `_`，SQL 侧配 `ESCAPE '\'`。`Keyword_EscapesLikeWildcards` 守着 |
| **SQLite-net 参数按出现顺序绑定** | `_buildWhere` 里 `lClauses.Add` 与 `lArgs.Add` 一旦错位，SQLite 不会报错，只会**静默筛出错误的行**——比崩溃更难发现 | 加条件时两者必须成对书写，顺序严格一致。排序、分页参数（Limit/Offset）必须拼在 `_buildWhere` 返回之后 |
| **中文字体** | `FontProvider` 的系统字体候选列表可能一个都不命中，表现为方块字 | 正式发布建议自带 `Fonts/main.ttf` |
| **emoji 显示为空白** | 真机实测：备注里填 emoji 渲染成空白。根因同上——`FontProvider` 的候选全是中文字体，**都不含 emoji 字形**，而 legacy `Text` 在字形缺失时不跨字体回退 | 观感问题，不影响数据。要支持得自带 emoji 字体，且 legacy `Text` 不支持彩色 emoji，彻底解决需换 TextMeshPro |
| **APK 里仍有 INTERNET 权限** | 已设 `Internet Access: Not Required`，测试也是绿的，但 `aapt dump badging` 实测包里仍有该权限。根因是 `com.unity.modules.unitywebrequest` 模块自己声明，manifest merger 合并进来，`ForceInternetPermission` 拦不住 | 单机 App 用不到，属瑕疵。要真正去掉需自定义 `Assets/Plugins/Android/AndroidManifest.xml` + `tools:node="remove"`——自定义 manifest 是构建失败高发区，单独一轮做 |
| **adb 连不上真机** | Task 17 验收时 USB（线缆只有电源线芯）与无线调试（路由器 AP 隔离）双双失败，最后靠手动传 APK 完成验收，**没有 logcat 佐证** | 下次接设备前先确认线能传数据、路由器没开客户端隔离。另：platform-tools v31.0.2+ 需 `ADB_MDNS_OPENSCREEN=1` 才能 `adb pair` |
| **编辑器占用** | 命令行跑测试或构建时，另一个 Unity 实例不能打开同一项目 | 跑之前先关编辑器；两个脚本都会以退出码 2 报出这个错误 |
| **命令行测试的静默失败** | `-quit` 会让 Unity 跳过测试直接退出（退出码 0）；`Temp/` 下的结果文件会被 Unity 退出时清理 | 两个坑都已规避并写进脚本，见第 10 节 |
| **`Assets/Resources/` 下的东西都会进 APK** | 放进去的每张图都算包体。应用图标源图一度放在 `Assets/Resources/Icons/AppIcon/`，等于把 47 张 PNG 白打进包里 | 图标源图已移到 `Assets/AppIcons/`（自动打包够不着），只在 Player Settings 里引用 |

---

## 12. 文档索引

| 文件 | 内容 |
|---|---|
| `SPEC.md` | 本文件。项目事实来源 |
| `CLAUDE.md` | AI 助手入口，精简导航 |
| `Assets/Scripts/App/CLAUDE.md` | App 层开发约定 |
| `Assets/Resources/CLAUDE.md` | 资源目录速查 |
| `Claude/账单管理系统功能清单.md` | 需求来源（12 个模块 + 5 个场景 + 5 个特色功能） |
| `Claude/plans/2026-09-11-账单管理MVP.md` | 实施计划，17 个任务，含完整代码 |
| `Claude/资源替换指南.md` | 给美术/设计的换图指南 |
| `Claude/图标资源库.md` | 开源图标库 / 插画 / 素材速查（含许可证对照与 SVG→PNG 处理） |
| `Claude/plans/android-release-checklist.md` | Android 真机验收记录：18 项结果、未通过项、验收环境问题、偏离计划处 |
