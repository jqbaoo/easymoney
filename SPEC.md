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
测试      ██████████ 100%   335 个用例全绿（Money 7 + Parser 17 + TimeUtil 15 + 建表 4 + 冒烟 1 + 构建配置 1 + 模型 4 + 分类仓储 16 + 账户仓储 12 + 账单仓储 10 + 校验 17 + 记账服务 11 + 筛选 18 + 报表 16 + 应用容器 7 + 快捷金额 6 + 界面工厂布局 3 + 账单展示 26 + 账户表单 13 + 报表展示 15 + 构建配置守卫 9 + 结构迁移 13 + 字体槽位 8 + 配色 10 + 月份条 10 + 月份弹窗 9 + 环形切分 14 + 视图清单 9 + 下拉列表 12 + 视图宿主 10 + 视图渲染 12）
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

| **分类图标** | `Resources/Icons/cat_*.png`、`Data/DefaultCategories.cs`、`Data/SchemaMigrations.cs`、`App/UI/UiFactory.cs` | 计划外新增。15 个预置分类的图标接进账单行 / 报表行 / 记账页分类行 / 分类选择弹窗四处；老库的 `icon_name` 靠 v2 迁移补上。见第 7 节 |
| **暖色纸感配色 + 卡片投影** | `Resources/theme.json`、`App/UI/ThemePalette.cs`、`App/UI/UiFactory.cs` | 计划外新增。原来是浅灰底 `#F2F3F5` 配白卡片，两者只差 13 个色阶、卡片又没有投影，整屏看着就是一片白。换成暖色纸感，卡片投影收进 `UiFactory.PaintCard` / `AddCardShadow`。配色在 `theme.json` 与 `ThemePalette.Light()` 两处，`ThemePaletteTests` 钉着一致。**Play 肉眼验收通过**（2026-09-13）。见第 7 节 |
| **月份条抽组件 + 点年月选月份** | `App/UI/MonthBar.cs`、`App/UI/MonthPickerDialog.cs`、`Core/TimeUtil.cs`、`App/UI/UiFactory.cs` | 计划外新增。账单页与报表页的月份条原先逐字重复（连三个常量都各定义一份），收成 `MonthBar` 后两页各一行 `new MonthBar(...)`；中间的年月文字从裸 `Text` 变成可点的「文字 + `↓`」横排，点开 `MonthPickerDialog`（年份行 + 3×4 月份网格）一次跳到目标月。`TimeUtil` 新增 `FormatYear` / `FormatMonth`，`FormatYearMonth` 改为拼这两个。**弹窗与月份条首次有了 EditMode 覆盖**（19 个用例）——原先逻辑在页面里，测试够不着。**Play 肉眼验收通过**（2026-09-13） |
| **报表环形图视图 + 下拉切换** | `Core/Reports/DonutLayout.cs`、`Core/Reports/ReportViewMode.cs`、`App/UI/DropdownButton.cs`、`App/UI/Reports/*.cs` | 计划外新增。报表页多一种画法：按占比把每一类切成扇区画成**环形图**，环中心显示合计金额；支出/收入切换旁挂一个下拉浮层切换视图，图例仍是明细列表（只是不带条形）。**视图是可扩展的**——以后再加一种，在 `ReportViews.ALL` 里挂个号、写一个 `IReportView` 实现、`ReportPage` 构造宿主时多传一个实例，页面别处不用动。环形贴图逐像素程序化生成（UGUI 没有扇形控件），色板走 `Theme.ChartColor(i)`。报表页里的明细行与条形画法一并沉进 `App/UI/Reports/`。**新增 65 个用例**（环形切分 14 + 视图清单 9 + 下拉列表 12 + 视图宿主 10 + 视图渲染 12 + 报表展示 3 + 配色 5）。⚠️ **Play 肉眼验收待做**，见 `Claude/plans/android-release-checklist.md` |

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
│   │   ├── Fonts/               字体（main / main_medium / main_bold 三档字重）
│   │   └── theme.json           配色
│   ├── Scenes/                  场景（原型阶段用不到）
│   ├── Scripts/
│   │   ├── Core/                ✅ Money / MoneyParser / TimeUtil / Models / Queries / ValidationResult / TransactionValidator / Reports / Statements / Accounts / Typography（noEngineReferences）
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
│   │           ├── MonthBar.cs         月份条（翻月 + 点年月选月），账单页与报表页共用
│   │           ├── DropdownButton.cs   就地下拉浮层（按钮正下方弹面板），报表页切视图用
│   │           ├── PickerDialog.cs     通用选择弹窗（分类 / 账户 / 日期共用）
│   │           ├── MonthPickerDialog.cs 选择月份弹窗（年份行 + 3×4 月份网格）
│   │           ├── AccountEditDialog.cs 账户新建 / 编辑弹窗（Task 15）
│   │           ├── Reports/    报表主体的几种画法，见第 8 节
│   │           │   ├── IReportView.cs      视图接口（Mode + Render）
│   │           │   ├── ReportViewHost.cs   按当前模式分发，换视图先清场
│   │           │   ├── ReportViewParts.cs  共用零件（明细行、空态提示）
│   │           │   ├── BarReportView.cs    条形图（Task 16 原样搬来）
│   │           │   ├── DonutReportView.cs  环形图 + 环中心合计
│   │           │   └── DonutSprite.cs      环形贴图，逐像素程序化生成
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
    +{static} string FormatYear(int)
    +{static} string FormatMonth(int)
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
  class MonthBar {
    +{static} float HEIGHT
    +int Year
    +int Month
  }
  class PickerDialog
  class MonthPickerDialog {
    +{static} void Show(RectTransform, int, int, Action<int,int>)
  }
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
TransactionListPage --> MonthBar : 年月由它持有
AccountPage ..> UiFactory
AccountPage ..> AccountEditDialog : 添加 / 编辑
AccountEditDialog ..> AccountForm : 表单规则
AccountEditDialog ..> PickerDialog : 选类型
ReportPage ..> ReportForm : 占比文案与条形宽度
ReportPage ..> UiFactory
ReportPage --> MonthBar : 年月由它持有
MonthBar ..> UiFactory
MonthBar ..> MonthPickerDialog : 点年月文字拉起
MonthBar ..> TimeUtil : 切月进位 / 年月文案
MonthPickerDialog ..> UiFactory
MonthPickerDialog ..> TimeUtil : 年份与月份文案
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

### 改表结构必须走迁移（第二版起）

**建表语句全是 `CREATE TABLE IF NOT EXISTS`，对已经存在的表等于什么都不做。**
所以给旧表加一列时，新装的 App 有这一列、升级上来的没有——一查询就报
`no such column`，是**崩溃**，不是降级。而第一版已经装在真机上，里面有真实数据。

机制在 `SchemaMigrator`（`Assets/Scripts/Data/`），迁移清单在 `SchemaMigrations.ALL`。
改结构的**三步**：

1. 往 `SchemaMigrations.ALL` 追加一条 `SchemaMigration`（`Version` = 新版本号，
   `Statements` = 变更 SQL）
2. 把 `EasyMoneyDb.SCHEMA_VERSION` 加一
3. 同步改 `Schema.cs` 的建表语句——**全新安装走的是建表语句，不经过迁移**，
   两条路要各自到达同一个结构

第 2 步忘了会被 `SchemaMigrationTests` 拦住（清单里有比 `SCHEMA_VERSION` 新的版本
= 它永远不会被执行，且只有升级上来的老库缺列，全新安装和测试里全都正常）。

机制本身的行为都被测试钉着：全新库直接盖章、老库按版本升序补跑、已走过的版本不重跑、
跨版本升级不跳步、降级不盖章也不跑迁移、同一版本内失败整体回滚。

**「迁移」不只指改表结构，补数据也算。** 判据是「新装的库天然就对，老库不对」——
v2 就是这一类：第一版 `DefaultCategories` 把预置分类的 `icon_name` 填成了空字符串，
新装的库走种子代码天然带图标名，已经装在真机上的库只能靠迁移把这段数据补上。
所以上面第 3 步要连**种子**一起改（结构改 `Schema.cs`，数据改 `DefaultCategories`），
否则两条路各自到达的状态不一样。

⚠️ **数据迁移要写成可以安全重跑的形式。** `SchemaMigrator` 的版本戳是**盖在每个
版本的事务之外**的，真机上「迁移已提交、盖章前进程被杀」是会发生的（系统回收、
用户强杀），下次启动这一版会重跑。对 `ALTER TABLE` 那是事故，对数据回填则取决于
SQL 怎么写——v2 的三个条件里 `icon_name = ''` 和 `ELSE icon_name` 正是为此存在：
重跑时已有图标名的行不会被覆盖回去，清单之外的行也不会被动。

真实迁移的用例在 `SchemaMigrationTests` 后半部分（前半部分用合成迁移驱动机制本身）：
`CategoryIconMigration_BackfillsLegacyDatabase`（老库补上了）、
`..._LeavesUserCategoriesAlone`（用户自建的同名分类不动）、
`..._DoesNotOverwriteExistingIcon`（钉住可重跑性）、
`..._MatchesFreshInstallSeed`（升级路径与全新安装到达同一状态）。

⚠️ **但覆盖安装不再是验收项。** 迁移该写、该测——它防的是老库 `no such column`
崩溃，不是可选项——只是「升级上来的老库」不再列进真机验收清单：**真机一律装全新的**。
上面那四条用例已经把升级路径的行为钉在测试里，不必每次真机再走一遍。

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
| `Sprites/card.png` | 卡片九宫格底图（投影是代码画的，不在这张图里） | 程序化生成圆角矩形 |
| `Sprites/button.png` | 按钮九宫格底图 | 复用 `card.png` |
| `Fonts/main.otf` | 主字体 Regular | 系统字体 → Unity 内置字体 |
| `Fonts/main_medium.otf` | 主字体 Medium（金额、标题） | 退回 Regular |
| `Fonts/main_bold.otf` | 主字体 Bold（标签栏选中、主按钮） | 退回 Regular |
| `Fonts/LICENSE-NotoSansSC.txt` | 字体许可证（OFL 1.1 要求随分发） | —— |
| `theme.json` | 浅色配色，运行时覆盖 `ThemePalette.Light()` | 内置浅色主题 |

字体槽位名→字重的映射、以及缺文件时的回退链，在 `Core/Typography/FontSlots.cs`
（纯逻辑，`FontSlotsTests` 盯着）。改动字重分配只需改 `Theme.cs` 里的
`WEIGHT_AMOUNT` / `WEIGHT_TITLE` / `WEIGHT_BODY` / `WEIGHT_STRONG` 四个语义常量，
页面代码不出现 `FontWeight.XXX` 字面量。

完整清单（每个图标的文件名、显示尺寸、分类 slug 表、九宫格 border 怎么切）
见 **`Claude/资源替换指南.md`**。

### 分类图标怎么接

分类的图标名是**数据**：存在 `category.icon_name` 列里（值形如 `cat_food`），
不是代码里按分类名硬编码的映射——将来「分类管理」页要让用户自己改，代码管不着。
这一点决定了图标要接四层：

| 层 | 做什么 |
|---|---|
| `Data/DefaultCategories.cs` | 新装的库：种子数据带上图标名 |
| `Data/SchemaMigrations.cs` | 老的库：v2 迁移按分类名把 `icon_name` 补上 |
| `Core/StatementBuilder`、`Core/ReportCalculator` | 投影与聚合时把图标名从分类表带出来（`StatementRow.CategoryIconName`、`CategoryBreakdownItem.IconName`） |
| `App` 各页面 | 用 `IconNames.ForCategory(名字)` 解析成**真实存在**的资源名，交给 `UiFactory.CreateIconSlot` 画出来 |

⚠️ **解析放页面，不放 `PickerDialog`。** 那个弹窗是分类 / 账户 / 日期 / 账户类型
共用的，让它认识「分类图标」是没必要的耦合。页面把解析好的名字列表传进去，
元素可以是 `null`（该项没图标）。反过来，`lIconNames` 传 `null` 时弹窗**一格都不建**——
否则账户、日期那几个弹窗每行都会平白多出一格左缩进。

⚠️ **`CreateIconSlot` 固定占一格，没图标就整格透明**，不会跳过不建。用户自建的
分类没有图标名，那一格若干脆不建，这行的文字会往左顶，跟上下行的左边缘参差不齐。
它返回 `Image` 而不是 `RectTransform`，是因为记账页的分类行选中分类后要**就地换图**——
换节点的话同帧内新旧两个节点会在布局里各占一格。

图标名这份对照表在四个地方各存了一份（迁移 SQL、`DefaultCategories`、`IconNames`
常量、PNG 文件名），编译器一个都管不了。两道网兜着：
`Defaults_IconNames_ResolveToExistingResources`（每个名字都能解析到真实资源，
这条同时盯住「Data 写错了」「常量改名了」「PNG 漏放了」三种事故）
与 `CategoryIconMigration_MatchesFreshInstallSeed`（升级与全新安装一致）。

### 配色与卡片投影

浅色配色存了两份：`Resources/theme.json`（运行时覆盖）和 `ThemePalette.Light()`
（`theme.json` 缺失或解析失败时的兜底）。**两者不一致平时看不出来**——界面用的
是 `theme.json` 那套，只有兜底路径才会露脸。
`ThemePaletteTests.ShippedThemeJson_MatchesLightPalette` 逐字段钉着它们一致，
**改配色时两处都要改**。深色配色只在 `ThemePalette.Dark()` 里，不受 `theme.json` 影响。

当前是**暖色纸感**（燕麦米白底 `#F2ECE2` + 暖白卡片 `#FFFCF6` + 焦糖棕 `#A9714B`）。
底色和卡片色只差十几个色阶，**两者的边界靠卡片那层投影交代**，所以 `shadow` 别调没了。
支出红 / 收入绿也跟着暖化了：原来的纯红 `#E03E3E` 放在米白底上会「燥」。

投影由 `UiFactory.AddCardShadow` 画（UGUI 的 `Shadow` 组件，偏移 `Theme.SHADOW_OFFSET`），
**画在节点之外，不动任何布局尺寸**。不烘进底图的原因：九宫格的 border 必须连投影
一起包住，那张图被拉伸到节点尺寸时卡片本体就比节点小一圈，行高、内边距、行间距
全得跟着重量一遍。代价是只偏移、不模糊，边缘偏硬——想要柔和投影就给美术一张
`card.png`，`AssetProvider` 会优先用图。

⚠️ **卡片底一律走 `UiFactory.PaintCard`。** 原本「`AddComponent<Image>` + `Card()` +
`Sliced` + `SURFACE`」这四行在账单行 / 账户行 / 报表行各抄了一遍，加投影就得改三处，
漏一处那块卡片就平贴在底色上，而且从代码里看不出来。现在收成一个调用。
账单页的月份条、汇总条、标签栏、标题栏是**直角纯色条**，不是卡片，不走这条路。

### 图表色板（`Theme.ChartColor(i)`）

环形图按分类序号取色，走 `ThemePalette.ChartColors`（浅色 8 色，冷暖交替，
相邻两块放一起能分得开）。取色一律用 `Theme.ChartColor(i)`，**调用方拿到的是色值、
不拿到下标**——Core 层不许 `using UnityEngine`，所以「第几号分类该用哪个颜色」
这个决定只能落在 App 层。

⚠️ **色板刻意不进 `theme.json`。** 它是「一组要能互相区分的颜色」，不是单值配色；
JsonUtility 解析数组要多写一层，而逐字段比对的两套配色（`theme.json` 与
`ThemePalette.Light()`）也要跟着复杂一圈。代价是这套色板**不受 `theme.json` 覆盖**——
`ThemePaletteTests.ChartColors_IgnoreThemeJson` 把这条决定钉住了，免得日后有人以为
改 `theme.json` 就能换图色。

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

⚠️ **兜底文字只能是字体子集里真有的字。** 字体按「GB2312 汉字 + ASCII + 中文标点
+ 货币符号」子集化（7594 字，脚本 `D:\font-tmp\subset.py`），挑错的字符**代码不报错、
测试也测不出来，真机上就是一片空白**——备注里的 emoji 是同一个坑。已实测：
`↓` `∨` `ˇ` `<` `>` 在子集里；`▾` `▼` `▽` `‹` `›` `«` `»` **都不在**。
月份条那个下拉提示的兜底 `↓` 就是这么挑出来的。

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
- **Unity 跑完测试经常不自己退出。** 实测多次：结果文件已经写完、测试也全绿了，
  但 `Unity.exe` 进程仍驻留（实测 1.6 GB），于是脚本一直停在等待那一行，
  看起来像卡死。这时**别再起第二次**——项目被占着，第二个实例会崩在
  `HandleProjectAlreadyOpenInAnotherInstance`，脚本报退出码 2，**长得跟编译错误一样**，
  很容易白查半天。

  判断顺序：先看 `Tools/editmode-results.xml` 在不在（在 = 这一轮其实已经跑完了），
  再清掉残留的 batchmode 进程。

  ```bash
  tasklist | grep -i unity          # 拿进程号
  taskkill //PID <pid> //F
  ```

  哪个才是残留：用
  `powershell -Command "Get-CimInstance Win32_Process -Filter \"ProcessId=<pid>\" | Select CommandLine"`
  看命令行，带 `-batchmode … -runTests` 的是。命令行**为空**的多半是 Unity Hub 的
  残留（十几 MB），不占项目，可以不管。**别凭进程名一律杀**——用户自己开着的编辑器
  也长这样，杀了会丢未保存的改动。

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

例外有三处：

1. **`UiFactoryLayoutTests`**（2026-09-13 加入）。它只 `new GameObject` 摆锚点，
   断言锚点能即时决定的尺寸——`rect` 由锚点和父容器当场算出，不需要 Canvas，也不需要
   `LayoutRebuilder`，所以 EditMode 下跑得动也不飘。**布局组算出的高度依赖一次真实的
   布局重建，不在这里测**，别往里加那类断言。
2. **`MonthBarTests` / `MonthPickerDialogTests`**（2026-09-13 加入）。界面组件**不是页面**，
   只要不经过 `AppRoot` 就能直接 `new` 出来，节点名是它们对外的形状，按名字取节点即可断言
   行为（翻月进位、回调、高亮、弹窗联动）。页面的主体部分仍然测不到。
   ⚠️ 关弹窗走的 `Object.Destroy` 在 EditMode 下**非法**（打一条 Error 且什么都不做，
   不是延迟到帧末），点到关闭按钮的用例必须先 `LogAssert.Expect` 声明这条日志。
3. **`DropdownButtonTests` / `ReportViewHostTests` / `ReportViewRenderTests`**（2026-09-13 加入）。
   同第 2 条的路子：报表的视图宿主、两个视图实现、下拉浮层都是可直接 `new` 的组件，
   节点名即形状，于是「切视图会不会叠着画」「选中的还是当前项会不会白刷一次」这类
   不报错、只画错的毛病才有断言可写。
   ⚠️ 这条路上有一处对既有约定的**有意偏离**：`UiFactory.DestroyObject` 在
   `Application.isPlaying` 为假时走 `Object.DestroyImmediate`。原本的约定是「生产代码
   保持 `Object.Destroy`，EditMode 的非法性由测试侧 `LogAssert.Expect` 吸收」——
   那条针对的是弹窗，因为那里「销毁」不可观测；而这里**「切视图要清掉旧节点」本身就是
   被测行为**，销毁是 no-op 的话这条根本验不了。弹窗仍按原约定不动。

**推论：能测的界面逻辑就往下沉成组件。** 月份条原是两页各一份的页面内代码、一条测试
都没有，收成 `MonthBar` 后才有 19 个用例盯上；报表的主体同理，原先是 `ReportPage` 里的
几个私有方法，收成 `App/UI/Reports/` 下的视图与宿主后才有 57 个用例。

结论：改完界面代码，仍然必须在 Unity 里点 Play 肉眼确认。

### 构建 Android APK

**同样必须先关掉 Unity 编辑器**（同一个项目不能被两个实例打开）。

两种跑法，底层是同一个脚本：

| 方式 | 命令 |
|---|---|
| 双击 | 项目根目录的 `build-apk.bat`（自动找 Git Bash，结束时停住让你看结果） |
| 命令行 | `bash Tools/build-android.sh` |

`.bat` 只是启动器，**不重复实现任何构建逻辑**，免得两份逻辑各改各的。它的成功判定
看的是「APK 在不在」而不是退出码——退出码 0 但没出包的情况真实发生过。

⚠️ **`.bat` 的内容必须保持纯 ASCII。** 含 UTF-8 中文的 `.bat` 会让 cmd 的批处理
解析器错位（加了 `chcp 65001` 也一样）：多字节字符被从中间劈开，碎片被当成命令执行。
这不是理论——`build-apk.bat` 的第一版就是这么坏的，还打印了「构建成功」而实际什么都没构建。
中文输出由 `.sh` 负责，本来就没问题。

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
| ~~中文字体依赖机型~~ | `FontProvider` 的系统字体候选列表可能一个都不命中，表现为方块字；命中了也不保证各机型字形一致 | ✅ **已解决**。自带 Noto Sans SC 三档字重（OFL 1.1，可随 APK 分发），字形不再看 ROM 脸色。子集化到「GB2312 汉字 + ASCII + 中文标点 + 货币符号」7594 字符，三档合计 5.7 MB |
| **字重槽位名写错是静默故障** | Medium 指到 bold 的文件不会报任何错，只会让字重错位，EditMode 跑不到页面，只能真机肉眼看出来 | 映射与回退链抽成 Core 纯函数 `FontSlots`，`FontSlotsTests` 逐个打表钉住（含「三档槽位名互不相同」）。偏离预期的反向验证已做：注入 2 个假 bug → 恰好 3 个用例变红，且**名字对得上** |
| **`FontWeight` 与 TMP 撞名** | `TMPro.FontWeight` 与 `UnityEngine.TextCore.LowLevel.FontWeight` 都存在且都含 `Regular/Medium/Bold`。当前无冲突（全项目无 `using TMPro`，枚举在 `EasyMoney.Core` 下），但 `EasyMoney.App.asmdef` 已引用 `Unity.TextMeshPro` | 将来谁为 TMP 加 `using TMPro;` 会触发 CS0104 歧义。加 `using` 前先确认是否有 `FontWeight` 的裸引用，有就写全限定名 |
| **子集字体的缺字风险** | 子集只裁了 7594 字，超出 GB2312 的人名生僻字（喆、昇、犇）会渲染成方块 | 已按 7594 走。真机遇到再扩到「通用规范汉字表」8105 字（约 2.2 MB/档），`D:\font-tmp\subset.py` 是子集脚本 |
| **emoji 显示为空白** | 真机实测：备注里填 emoji 渲染成空白。根因同上——候选字体全是中文字体（含自带的 Noto Sans SC），**都不含 emoji 字形**，而 legacy `Text` 在字形缺失时不跨字体回退 | **已定性为纯渲染问题，数据没丢**。依据是把写入链路三段都证干净了（仓储 `Note_SupportsChineseAndEmoji`、服务 `Save_KeepsEmojiNoteIntact`、投影 `BuildRow_EmojiNote_KeepsItWholeAsTitle`），且全项目没有按 `char` 截断的代码。要支持得自带 emoji 字体 + 换 TextMeshPro，**当前判断为不值得做**。注意 Unity 的 legacy `Font` 与 TMP 3.0.7 **都不支持彩色 emoji 字体**（CBDT/CBLC、COLR/CPAL 均不认），换 TMP 也只在用单色 emoji 字体时才有效 |
| **APK 里仍有 INTERNET 权限** | 已设 `Internet Access: Not Required`，测试也是绿的，但 `aapt dump badging` 实测包里仍有该权限。根因是 `com.unity.modules.unitywebrequest` 模块自己声明，manifest merger 合并进来，`ForceInternetPermission` 拦不住 | 单机 App 用不到，属瑕疵。要真正去掉需自定义 `Assets/Plugins/Android/AndroidManifest.xml` + `tools:node="remove"`——自定义 manifest 是构建失败高发区，单独一轮做 |
| **adb 连不上真机** | Task 17 验收时 USB（线缆只有电源线芯）与无线调试（路由器 AP 隔离）双双失败，最后靠手动传 APK 完成验收，**没有 logcat 佐证** | 下次接设备前先确认线能传数据、路由器没开客户端隔离。另：platform-tools v31.0.2+ 需 `ADB_MDNS_OPENSCREEN=1` 才能 `adb pair` |
| **编辑器占用 / 残留的 batchmode 进程** | 命令行跑测试或构建时，另一个 Unity 实例不能打开同一项目。除了用户开着的编辑器，**上一次跑完却没退出的 batchmode 进程**同样会占着项目（实测多次：结果都写完了、进程仍驻留 1.6 GB），而它报出来的错是 `HandleProjectAlreadyOpenInAnotherInstance`，看起来像编译错误 | 跑之前先确认没有 Unity 实例；脚本以退出码 2 报出。清理办法见第 10 节——**先看结果文件在不在**，别急着当编译错误查 |
| **命令行测试的静默失败** | `-quit` 会让 Unity 跳过测试直接退出（退出码 0）；`Temp/` 下的结果文件会被 Unity 退出时清理 | 两个坑都已规避并写进脚本，见第 10 节 |
| **`Assets/Resources/` 下的东西都会进 APK** | 放进去的每张图都算包体。应用图标源图一度放在 `Assets/Resources/Icons/AppIcon/`，等于把 47 张 PNG 白打进包里 | 图标源图已移到 `Assets/AppIcons/`（自动打包够不着），只在 Player Settings 里引用 |
| **改表结构会砸掉用户数据** | 建表全是 `CREATE TABLE IF NOT EXISTS`，对已有的表等于什么都不做。给旧表加列，升级上来的老库会 `no such column`——是崩溃，不是降级。第一版已装在真机上且有真实数据 | ✅ **已解决**。`SchemaMigrator` + `SchemaMigrations.ALL`，`EasyMoneyDb.Open()` 时按版本补跑缺失的迁移。改结构走三步，见第 6 节。机制行为有 9 个测试钉着 |
| **`.bat` 里写中文会让 cmd 解析器错位** | 含 UTF-8 多字节字符的 `.bat`，即使加了 `chcp 65001`，cmd 的批处理解析器也会错位：字符被从中间劈开，碎片被当成命令执行。`build-apk.bat` 第一版因此打印了「构建成功」但**什么都没构建** | `build-apk.bat` 保持纯 ASCII，中文交给 `.sh` 输出。成功判定改成看产物在不在，不看退出码 |

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
