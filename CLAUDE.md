# EasyMoney — AI 助手工作指引

> **动手前先读 `SPEC.md`。** 那里有完整的技术栈、架构、数据模型和当前进度。
> 本文件只放最常用的导航和硬约束。

---

## 这个项目是什么

Android 本地记账 App，Unity 2022.3.53f1c1（中国版），UGUI + SQLite，单机无网络。

---

## 当前进度（2026-09-13）

**MVP 已完工。17 个任务全部完成，APK 已在 Redmi K60 真机上验收。**

- ✅ `Assets/Scripts/App/` —— UI 基础设施 + 四个页面 + `AppContext` + `PickerDialog` + `AccountEditDialog`
- ✅ **Task 1-17 完成** —— 三层程序集骨架 + 命令行测试链路 + 整个逻辑层 + 应用容器 +
  四个页面 + Android 打包
  （`Core`：Money / TimeUtil / 模型 / 校验 / 报表 / 快捷金额 / 账单展示投影 / 账户表单 / 报表展示；
  `Data`：SQLite / 三个仓储 / 记账服务 / 多维筛选；`App`：`AppContext` / 记账页 / 账单页 / 账户页 / 报表页）
- ✅ **270 个 EditMode 测试全绿**，`EasyMoney.Core.dll` 与 `EasyMoney.Data.dll` 均已生成
- ✅ 标签 **`data-layer-complete`**（业务逻辑层封顶）、**`mvp-complete`**
- ✅ **APK 构建成功**（`bash Tools/build-android.sh` → `Builds/EasyMoney.apk`，29 MB），
  真机 18 项验收通过 17 项
- ✅ **字体风格升级**（2026-09-13）—— 自带 Noto Sans SC 三档字重（Regular / Medium /
  Bold，OFL 1.1，子集化后合计 5.7 MB），全站建立字重层次，**字形不再依赖机型 ROM**。
  字重→槽位映射抽在 `Core/Typography/FontSlots.cs`，`FontSlotsTests` 8 个用例钉着。
  ⚠️ **APK 至今没重打**——从这一版起累计五次改动没进包（三档字重 / 暖色配色 /
  月份条 / 环形图 / 图例色点），都只在编辑器里验过。包体预计从 ~29 MB 涨到 ~35 MB，
  真机复验待做，见 `Claude/plans/android-release-checklist.md` 的「后续待办」
- ✅ **分类图标接入**（2026-09-13）—— 15 个预置分类的图标接进账单行 / 报表行 /
  记账页分类行 / 分类选择弹窗四处。图标名是**数据**（存在 `category.icon_name` 里），
  所以新装的库走 `DefaultCategories` 种子、**老库走 v2 迁移补**，两条路都得改，
  只改一边会有一半用户看不到图标。
  这一版动了 `SCHEMA_VERSION`（1 → 2），老库靠迁移补上（覆盖路径不再真机验收，
  见下面「验收」）
- ✅ **暖色纸感配色 + 卡片投影**（2026-09-13）—— 原来是浅灰底 `#F2F3F5` 配白卡片，
  两者只差 13 个色阶、卡片又没有投影，整屏看着就是一片白。换成燕麦米白底 + 暖白卡片 +
  焦糖棕主色，卡片统一加一层投影（`UiFactory.PaintCard` / `AddCardShadow`）。
  配色在 `theme.json` 和 `ThemePalette.Light()` 两处，`ThemePaletteTests` 钉着一致。
  ✅ **Play 肉眼验收通过**（2026-09-13）——底色与卡片层次分得开、投影在
- ✅ **月份条抽组件 + 点年月选月份**（2026-09-13）—— 账单页和报表页的月份条
  原先逐字重复（连 `MONTH_BAR_HEIGHT` / `NAV_BUTTON_WIDTH` / `NAV_ICON_SIZE`
  三个常量都各定义一份），收成 `App/UI/MonthBar.cs`，两页各一行 `new MonthBar(...)`；
  中间的年月文字从裸 `Text` 变成可点的「文字 + `↓`」，点开 `MonthPickerDialog`
  （年份行 + 3×4 月份网格）一次跳到目标月。
  ⚠️ **顺手多了一条教训**：月份条原来在页面里，一条测试都没有；收成组件后
  `MonthBarTests` + `MonthPickerDialogTests` 共 19 个用例才够得着它——
  **能测的界面逻辑就往下沉成组件**
  ✅ **Play 肉眼验收通过**（2026-09-13）——弹窗、月份条、`↓` 提示、遮罩与居中、
  跨年进位 5 项全过，详见 `Claude/plans/android-release-checklist.md`
- ✅ **报表环形图 + 下拉切换视图**（2026-09-13）—— 报表页多一种画法：按占比切扇形
  画成环形图，环心显示合计；筛选行挂 `DropdownButton` 切换。**视图可扩展**——
  新的视图只要在 `Core/Reports/ReportViews.ALL` 挂号、写一个 `IReportView` 实现、
  页面构造宿主时多传一个实例。环形贴图是逐像素程序化生成的（UGUI 没有扇形控件）
  ✅ **Play 肉眼验收通过**（2026-09-13，14 项）
- ✅ **环形图图例色点 + 两处修正**（2026-09-13）—— 明细行最左加色点，颜色按**行号**
  取槽位（跟扇区同一套，跟 `CategoryId` 走的话删掉一个分类颜色就整体错位）；
  修掉报表页「收入构成」未选中时**白底白字**（用户肉眼发现，当时 335 个用例全绿）——
  根因是同一段配色样板在两页各抄一遍、抄反了一个分支，收成 `TogglePalette`
  之后调用方传的是「选没选中」，这类抄错不可能再犯；记账页保存后**只清金额与备注**，
  类型/账户/分类/日期留着（连记几笔同类账不用重选）。
  ⚠️ 记账页那条**没有测试覆盖**（`_onSave` 要连着数据库才走得完），只能靠 Play 验
  ✅ **Play 肉眼验收通过**（2026-09-13，13 项）

**写账单必须走 `TransactionService.Save()` / `CreateTransfer()`**，直接调
`ITransactionRepository` 等于绕过校验。

⚠️ **打包配置现在有测试守着**（`AndroidPlayerSettingsTests`，9 个用例）。
改 Player Settings 前先看这个文件——计划里 Task 17 那张配置表的每一条都有对应断言，
配置对不对不靠肉眼核对 Unity 面板。

⚠️ **真机验收的两处已知瑕疵**（都不影响功能，详见 `Claude/plans/android-release-checklist.md`）：
APK 里仍有 `android.permission.INTERNET`（UnityWebRequest 模块带的，得自定义 AndroidManifest
才能去掉）；备注里的 emoji 显示为空白。**emoji 那条已定性为纯字体问题，数据没丢**——
真机上看到空白时别急着去查存储，写入链路三段都有测试钉着了。

⚠️ **接页面时别把展示规则写进页面里。** 页面的分组、文案拼接、兜底规则都是纯逻辑，
抽到 `Core` 才能被测试盯住——EditMode 跑不到页面，留在页面里只能靠肉眼看。
Task 14 抽的是 `Core/Statements/StatementBuilder.cs`（按本地日期分组 / 金额正负号 /
名称兜底），Task 15 抽的是 `Core/Accounts/AccountForm.cs`（类型标签 / 名称与初始
余额校验），Task 16 抽的是 `Core/Reports/ReportForm.cs`（构成标题 / 占比文案 /
条形宽度钳位），页面只管取数和渲染。

**规则能抽成纯函数就抽，抽不成纯函数的就收成组件**——组件不是页面，EditMode 里
`new` 得出来、节点名就是它的形状，也就测得了。月份条（`App/UI/MonthBar.cs`）是
后一条路的先例：原先两页各一份、零测试，收成组件后 19 个用例盯上了它的翻月进位、
回调与弹窗联动。**先问「这段逻辑能不能离开页面」，再问「该放 Core 还是该做组件」。**

⚠️ **反向验证会暴露测试自身的洞，别把「预测失败数对上了」当成通过。** Task 15 注入
4 个假 bug 后失败数确实是 5，但对不上预测的那 5 个：`Validate_EmptyBalance_MeansZero`
该红没红，因为它只断言了 `InitialBalanceCents == 0`，而校验失败的返回值那个字段同样是
默认值 0——「留空按 0 算」和「留空报错」在这个断言下没有区别。别只看失败个数，要逐个
核对**名字**对不对得上。

⚠️ **Android 真机相关的坑，详见 `SPEC.md` 第 11 节**：

- **`NuGetForUnity` 只解压 `NativeRuntimeSettings.json` 里登记过的 runtime**，
  nupkg 里的其余平台会被**静默丢弃**，编辑器里完全看不出来。新增带原生库的包时
  务必检查 `ProjectSettings/Packages/com.github-glitchenzo.nugetforunity/`
  下这个文件
- Target Architectures 已设为 **ARMv7 + ARM64**（`AndroidTargetArchitectures: 3`）
- 别把 `Project Settings → Burst AOT Settings` 里的 `ARMV8A` / `ARMV9A` 当成打包架构
  设置——那是 Burst 生成原生代码用的指令集目标，与 APK 里打包哪些 ABI 无关
- SQLite 依赖已升到 3.x、四个 Android ABI 齐全，原「Android 原生库缺失」风险已解决

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
- **改表结构必须走迁移**。建表全是 `CREATE TABLE IF NOT EXISTS`，对已有的表等于
  什么都不做——给旧表加列会让升级上来的老库报 `no such column`（崩溃，不是降级），
  而第一版已经装在真机上且有真实数据。三步：往 `SchemaMigrations.ALL` 追加一条 →
  `EasyMoneyDb.SCHEMA_VERSION` 加一 → 同步改 `Schema.cs`（全新安装走建表语句，
  不经过迁移）。忘记加版本号会被 `SchemaMigrationTests` 拦住。详见 `SPEC.md` 第 6 节

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
- **卡片底一律用 `UiFactory.PaintCard`**，别自己拼 `Image` + `Sliced` + `SURFACE`——
  那段样板在三个页面各抄过一遍，加投影时漏一处，那块卡片就平贴在底色上，
  而且从代码里看不出来
- **「选中 / 未选中」配色一律用 `TogglePalette.Apply(oButton, bSelected)`**，
  别自己拼三元表达式。同一段样板也在两页各抄过一遍，抄第二遍时把「暖白底 + 深字」
  写成了「暖白底 + 白字」——白底白字，那个按钮看上去就是「没有文字」。
  **同一段样板抄第二遍的地方，就是 bug 长出来的地方**
- 卡片色和页面底色只差十几个色阶，层次是靠投影交代的。**改配色别把 `shadow` 调没了**

### 资源
- 全项目**只有 `AssetProvider` 能碰 `Resources.Load`**
- 资源缺失时返回 `null`，调用方走代码兜底，**不抛异常、不给占位图**
- 换资源只改 `Assets/Resources/` 下的文件，**不改代码**

### 验收
- **真机验收一律装全新的，不跑覆盖安装那条路径。** 迁移代码（`SchemaMigrator` +
  `SchemaMigrations.ALL`）照旧要写、要有测试——它防的是老库 `no such column`
  崩溃，不是可选项——但「升级上来的老库」不再列进真机验收清单

---

## 常用命令

### 跑 EditMode 测试

**必须先关掉 Unity 编辑器**，否则第二个实例起不来（`Multiple Unity instances cannot open the same project`）。

```bash
bash Tools/run-editmode-tests.sh
```

退出码：**0 = 全部通过，1 = 有测试失败，2 = 环境/编译错误**。

⚠️ 三个已实测的坑，前两个写死在脚本里了，别绕过脚本手敲命令：

- **不要加 `-quit`** —— 它会让 Unity 在跑测试之前就退出，退出码 0 但一个测试都没跑，
  是静默失败
- **`-testResults` 不要指向 `Temp/`** —— Unity 退出时会清理 `Temp/` 目录，结果文件
  会被删掉（实测写入成功后又消失），脚本会误判成编译错误
- **Unity 跑完测试经常不自己退出**，脚本就一直等着（实测多次，进程驻留 1.6 GB）。
  这时别再起第二次：项目被占着，第二个实例崩在
  `HandleProjectAlreadyOpenInAnotherInstance`，报退出码 2，**看起来跟编译错误一样**。
  先看 `Tools/editmode-results.xml` 在不在（在 = 这轮已经跑完了），再清残留进程——
  用 `Get-CimInstance Win32_Process` 看命令行，**只杀带 `-batchmode … -runTests` 的那个**，
  用户自己开着的编辑器也长得一样，杀了会丢未保存的改动

详见 `SPEC.md` 第 10 节。

### 构建 Android APK

**同样必须先关掉 Unity 编辑器。**

**双击项目根目录的 `build-apk.bat`**，或命令行 `bash Tools/build-android.sh`。
两者底层是同一个脚本，`.bat` 只是启动器。

产物 `Builds/EasyMoney.apk`，退出码 **0 = 成功 / 1 = 构建失败 / 2 = 环境问题**。
首次 IL2CPP 构建实测约 4.5 分钟。日志在 `Tools/build-android.log`（不放 `Temp/`，
那个目录 Unity 退出时会清）。

⚠️ **别绕过脚本手敲 `Unity.exe`**，四个坑写死在里面了。⚠️ **`build-apk.bat`
的内容必须保持纯 ASCII**——含中文的 `.bat` 会让 cmd 解析器错位，第一版因此
打印了「构建成功」而实际什么都没构建。详见 `SPEC.md` 第 10 节。

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
| `Claude/图标资源库.md` | 开源图标库速查：启动图标工具、界面图标库、许可证对照 |
| `Claude/plans/android-release-checklist.md` | Android 真机验收记录：结果、未通过项、环境问题、偏离计划处 |
| `Assets/Scripts/App/CLAUDE.md` | App 层开发约定 |
| `Assets/Resources/CLAUDE.md` | 资源目录速查 |
