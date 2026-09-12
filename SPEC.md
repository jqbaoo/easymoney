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

**这是最重要的一节。** 项目处在「界面原型完成、Core 层起步」的阶段。

```
Core 层   ██░░░░░░░░  20%   Money / MoneyParser 已完成并有测试
Data 层   █░░░░░░░░░   5%   仅 asmdef 骨架，无业务代码
App 层    ████████░░  80%   界面全在，缺数据绑定
测试      █████░░░░░  50%   25 个用例全绿（冒烟 1 + Money 7 + Parser 17）
打包      ░░░░░░░░░░   0%   未开始
```

### 已完成

| 内容 | 文件 | 说明 |
|---|---|---|
| UI 基础设施 | `App/Scripts/App/UI/*.cs` | Theme / UiFactory / SpriteFactory / FontProvider / SafeAreaFitter / PageBase / PageRouter / TabBar |
| **资源层** | `App/UI/AssetPaths.cs`、`AssetProvider.cs`、`IconNames.cs`、`ThemePalette.cs` | 计划外新增，见第 7 节 |
| 应用入口 | `App/AppRoot.cs` | 含 `RuntimeInitializeOnLoadMethod`，打开任意场景点 Play 即可运行 |
| 四个页面（视觉原型） | `App/UI/Pages/*.cs` | 记账页 / 账单列表页 / 账户页 / 报表页 |
| 假数据 | `App/DemoData.cs` | 撑着页面用的，接数据层后**整个文件删掉** |
| **程序集骨架 + 测试链路** | `Scripts/Core`、`Scripts/Data`、`Tests/EditMode`、`Tools/run-editmode-tests.sh` | Task 1 产出。命令行跑 EditMode 测试已实测可用 |
| **金额值类型** | `Scripts/Core/Money.cs`、`MoneyParser.cs` | Task 2 产出。`readonly struct` 内部存「分」+ 输入解析，25 个用例全绿 |

### 未开始

| 内容 | 对应计划任务 |
|---|---|
| `TimeUtil` 时间工具 | Task 3 |
| SQLite 接入与建表 | Task 4（**最高风险**） |
| 领域模型 | Task 5 |
| 三个仓储 + 记账服务 + 校验 | Task 6、7、8、9 |
| 筛选搜索、报表计算 | Task 10、11 |
| **`AppContext.cs`** | Task 12 剩余部分 |
| 页面接真实数据（改 `_refresh()`） | Task 13-16 剩余部分 |
| Android 构建与真机验收 | Task 17 |

### 关键判断

计划的 Task 12-16 是「UI 基础设施 + 四个页面」。其中**界面部分已作为视觉原型提前做完**，
但数据层（Task 3-11）还没走，所以：

- 界面上看到的每一个数字都来自 `DemoData.cs`，是假的
- 按钮点了没有实际效果（保存只清空表单）
- `Core` 程序集已有 `Money.cs` / `MoneyParser.cs`，`Library/ScriptAssemblies/`
  下已如期出现 `EasyMoney.Core.dll`；`Data` 程序集仍**只有 asmdef、没有任何 `.cs` 文件**，
  Unity 不会为没有脚本的程序集生成 DLL，所以没有 `EasyMoney.Data.dll`——这是正常的，
  等 Task 4 往 Data 里放第一个 `.cs` 后就会出现

**下一步应该从 Task 3 开始按顺序执行**，不要跳。

---

## 3. 技术栈

| 项 | 值 |
|---|---|
| Unity | 2022.3.53f1c1（中国版） |
| Unity 可执行文件 | `D:\unity\unity2022\2022.3.53f1c1\Editor\Unity.exe` |
| 模板 | 2D |
| 目标平台 | Android，IL2CPP |
| UI | UGUI（`UnityEngine.UI`），**全部代码构建，不用 Prefab** |
| 数据库 | SQLite（sqlite-net-pcl + SQLitePCLRaw.bundle_green） |
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
│   │   ├── Core/                ✅ asmdef 已建（noEngineReferences），业务代码待填
│   │   ├── Data/                ✅ asmdef 已建，业务代码待填
│   │   └── App/                 ✅ 已有
│   │       ├── EasyMoney.App.asmdef
│   │       ├── AppRoot.cs
│   │       ├── DemoData.cs      ← 接数据层后删除
│   │       └── UI/
│   │           ├── Theme.cs / ThemePalette.cs
│   │           ├── UiFactory.cs
│   │           ├── SpriteFactory.cs / FontProvider.cs
│   │           ├── AssetPaths.cs / AssetProvider.cs / IconNames.cs
│   │           ├── SafeAreaFitter.cs
│   │           ├── PageBase.cs / PageRouter.cs / TabBar.cs
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
  }
  class Account
  class Category
  class Transaction
  enum TxType { Expense=0, Income=1, Transfer=2 }
  enum CategoryKind { Expense=0, Income=1 }
  enum AccountType { Cash=0, BankCard=1, Alipay=2, WeChat=3, Other=4 }
}

package "EasyMoney.Data" #E3F2FD {
  class EasyMoneyDb {
    +Initialize(string sPath)
    +Dispose()
  }
  interface IAccountRepository
  interface ICategoryRepository
  interface ITransactionRepository
  class SqliteAccountRepository
  class SqliteCategoryRepository
  class SqliteTransactionRepository
  class TransactionService
  class TransactionValidator
  class ReportCalculator
  class TransactionQuery
}

package "EasyMoney.App" #FFF3E0 {
  class AppContext {
    +{static} AppContext Instance
    +EasyMoneyDb Db
    +void Initialize(string)
    +void NotifyDataChanged()
    +event Action DataChanged
  }
  class AppRoot
  class PageBase
  class PageRouter
  class TabBar
  class UiFactory
  class Theme
  class AssetProvider
  class RecordPage
  class TransactionListPage
  class AccountPage
  class ReportPage
}

package "Assets/Resources" #F3E5F5 {
  folder "Icons / Sprites / Fonts / theme.json"
}

Core <-- Data
Data <-- App
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
AccountPage ..> UiFactory
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

### 运行界面

打开 Unity，任意场景点 Play。`AppRoot` 有 `[RuntimeInitializeOnLoadMethod]`，
会自动创建 Canvas，不需要往场景里挂任何东西。

---

## 11. 已知风险

| 风险 | 说明 | 处置 |
|---|---|---|
| **Unity 不内置 SQLite** | Task 4 的最高风险点。需要手工导入 sqlite-net-pcl 及其原生依赖 DLL | 单独成一个任务；**走不通就停下来报告，不要硬扛** |
| **IL2CPP 代码剥离** | 会删掉 SQLite 靠反射调用的代码，表现为真机上崩 | `link.xml` + 剥离级别设 `Minimal`，双重保护 |
| **LIKE 通配符** | 用户搜索词里的 `%` `_` 会被当通配符 | SQL 里用 `ESCAPE '\'` 转义 |
| **中文字体** | `FontProvider` 的系统字体候选列表可能一个都不命中，表现为方块字 | 正式发布建议自带 `Fonts/main.ttf` |
| **编辑器占用** | 命令行跑测试时，另一个 Unity 实例不能打开同一项目 | 跑之前先关编辑器；脚本会以退出码 2 报出这个错误 |
| **命令行测试的静默失败** | `-quit` 会让 Unity 跳过测试直接退出（退出码 0）；`Temp/` 下的结果文件会被 Unity 退出时清理 | 两个坑都已规避并写进脚本，见第 10 节 |

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
