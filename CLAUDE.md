# EasyMoney — AI 助手工作指引

> **动手前先读 `SPEC.md`。** 那里有完整的技术栈、架构、数据模型和当前进度。
> 本文件只放最常用的导航和硬约束。

---

## 这个项目是什么

Android 本地记账 App，Unity 2022.3.53f1c1（中国版），UGUI + SQLite，单机无网络。

---

## 当前进度（2026-09-13）

**界面原型完成，数据层起步。**

- ✅ `Assets/Scripts/App/` —— UI 基础设施 + 四个页面（**用假数据撑着**）
- ✅ **Task 1-4 完成** —— 三层程序集骨架 + 命令行测试链路 +
  `Core`（Money / MoneyParser / TimeUtil）+ `Data`（SQLite 接入、三张表 + 5 个索引）
- ✅ **36 个 EditMode 测试全绿**，`EasyMoney.Core.dll` 与 `EasyMoney.Data.dll` 均已生成
- ❌ 还没有任何仓储，数据进不去

计划的 17 个任务里，**Task 5-11 一步都没走**。下一步从 **Task 5**（领域模型）开始，
按顺序执行，不要跳。

⚠️ **Task 17 之前必须处理**：`SQLitePCLRaw.lib.e_sqlite3` 的 2.1.x 全线不含 Android
原生库，编辑器能跑但真机会崩。详见 `SPEC.md` 第 11 节。

界面上看到的每个数字都来自 `Assets/Scripts/App/DemoData.cs`，是假的。

---

## 实施计划在哪

`Claude/plans/2026-09-11-账单管理MVP.md`（约 7300 行）

17 个任务，每个任务都有完整的 TDD 步骤和可直接抄的代码。执行某个任务前，
**完整读那一个任务的章节**，不要只读标题。

任务顺序：Core 层（2-3）→ SQLite（4，最高风险）→ 模型（5）→ 仓储（6-8）→
服务（9）→ 查询报表（10-11）→ UI 接线（12-16）→ 打包（17）。

---

## 硬约束（违反就是 bug）

### 架构
- **依赖方向单向**：Core ← Data ← App，不许反向
- **Core 层不许 `using UnityEngine`**（asmdef 里 `noEngineReferences: true` 强制保证）
- **界面全部用代码构建，不用 Prefab。** 一律经由 `UiFactory` 创建

### 数据
- **金额一律 `long` 存「分」**，禁止 `float` / `double`
- **时间一律 `long` 存 Unix 毫秒（UTC）**，时区转换只在展示层
- **转账用单条记录**（`type=Transfer, account_id=转出, to_account_id=转入`），不用两条
- **账户余额不冗余存储**，用 SQL 子查询实时聚合

### 命名（用户强制要求）
| 种类 | 规则 | 例子 |
|---|---|---|
| 私有字段 | `m_` + 大驼峰 | `m_Index` |
| 全局变量 | `g_` + 大驼峰 | `g_Config` |
| 私有方法 | `_` + 小驼峰 | `_refresh()` |
| 公开方法 | 大驼峰 | `Refresh()` |
| 局部变量 | 类型前缀 + 大驼峰 | `iCount` / `sName` / `lItems` / `oData` |
| 常量 | 全大写下划线 | `REF_WIDTH` |

### 视觉
- 设计基准 **750 × 1334**，`CanvasScaler.matchWidthOrHeight = 0`（按宽度匹配）
- 尺寸字号一律来自 `Theme.cs` 的 `const`
- 颜色一律通过 `Theme.XXX` 取，页面里不许出现 `new Color(...)` 字面量
- 图标名一律用 `IconNames.XXX`，不许写字符串

### 资源
- 全项目**只有 `AssetProvider` 能碰 `Resources.Load`**
- 资源缺失时返回 `null`，调用方走代码兜底，**不抛异常、不给占位图**
- 换资源只改 `Assets/Resources/` 下的文件，**不改代码**

---

## 常用命令

### 跑 EditMode 测试

**必须先关掉 Unity 编辑器**，否则第二个实例起不来（`Multiple Unity instances cannot open the same project`）。

```bash
bash Tools/run-editmode-tests.sh
```

退出码：**0 = 全部通过，1 = 有测试失败，2 = 环境/编译错误**。

⚠️ 两个已实测的坑，写死在脚本里了，别绕过脚本手敲命令：

- **不要加 `-quit`** —— 它会让 Unity 在跑测试之前就退出，退出码 0 但一个测试都没跑，
  是静默失败
- **`-testResults` 不要指向 `Temp/`** —— Unity 退出时会清理 `Temp/` 目录，结果文件
  会被删掉（实测写入成功后又消失），脚本会误判成编译错误

详见 `SPEC.md` 第 10 节。

### 运行界面

打开 Unity，任意场景点 Play。`AppRoot` 有 `[RuntimeInitializeOnLoadMethod]`，
自动创建 Canvas，不用往场景里挂东西。

---

## 提交前检查

1. **`.meta` 文件必须一起提交。** Unity 为每个资源和脚本生成 `.meta`，
   漏提交会导致别人拉下来后引用全断
2. 改了代码后**在 Unity Console 确认无编译报错**。命令行编译验证经常因为
   编辑器占用而跑不了，所以这一步不能省
3. 不要提交 `Temp/`、`Logs/`、`Library/`

---

## 沟通约定

- **永远用中文回答**
- **类图、流程图必须用 PlantUML 格式**
- 注释用中文，解释「为什么」而不是「这行在干什么」

---

## 文档索引

| 文件 | 内容 |
|---|---|
| `SPEC.md` | 项目事实来源：技术栈、架构、数据模型、当前进度 |
| `Claude/plans/2026-09-11-账单管理MVP.md` | 实施计划，17 个任务 |
| `Claude/账单管理系统功能清单.md` | 需求来源 |
| `Claude/资源替换指南.md` | 给美术/设计的换图指南 |
| `Assets/Scripts/App/CLAUDE.md` | App 层开发约定 |
| `Assets/Resources/CLAUDE.md` | 资源目录速查 |
