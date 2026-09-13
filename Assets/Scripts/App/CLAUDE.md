# EasyMoney.App — 应用层

**能碰 UnityEngine 和 UI 的唯一一层。** 上层没有别的东西了，下层是 Data / Core。

命名空间 `EasyMoney.App`，程序集 `EasyMoney.App`，引用 `UnityEngine.UI`。

---

## 目录

```
App/
├── EasyMoney.App.asmdef
├── AppContext.cs       单例依赖容器（库 + 三个仓储 + TransactionService）
├── AppRoot.cs          应用入口：建库 + 界面骨架 + 标签栏装配
└── UI/
    ├── Theme.cs            尺寸 / 字号 / 颜色（颜色转发给 ThemePalette）
    ├── ThemePalette.cs     配色对象，含 Light() / Dark() / FromJson()
    ├── UiFactory.cs        ★ 所有 UI 构件的工厂
    ├── SpriteFactory.cs    卡片 / 按钮底图（资源优先，程序化兜底）
    ├── FontProvider.cs     字体（按字重解析：Resources → 系统 → 内置）
    ├── AssetPaths.cs       资源路径常量
    ├── AssetProvider.cs    唯一的 Resources.Load 入口
    ├── IconNames.cs        图标名常量
    ├── SafeAreaFitter.cs   安全区适配
    ├── PageBase.cs         页面抽象基类
    ├── PageRouter.cs       页面注册与切换
    ├── TabBar.cs           底部标签栏
    ├── PickerDialog.cs     通用选择弹窗（分类 / 账户 / 日期共用）
    ├── AccountEditDialog.cs 账户新建 / 编辑弹窗（Task 15）
    └── Pages/
        ├── RecordPage.cs
        ├── TransactionListPage.cs
        ├── AccountPage.cs
        └── ReportPage.cs
```

---

## 写一个页面

`PageBase` 是**纯类，不是 MonoBehaviour**：

```csharp
public class XxxPage : PageBase
{
    public override string Title => "标题";       // 显示在顶栏

    public override void OnShow() { _refresh(); } // 每次切到本页时调用

    protected override void _build()              // 只调用一次
    {
        // 在这里用 UiFactory 搭界面，挂在 Root 下面
    }

    private void _refresh() { /* 取数并填界面 */ }
}
```

在 `AppRoot._buildPages()` 里注册：

```csharp
_register(TABS[2].Key, new XxxPage());
```

---

## UiFactory 布局约定（★ 最容易踩坑的地方）

### 纵向容器 Column
`childControlHeight = true` —— 子元素靠 `LayoutElement.preferredHeight` 定高，宽度自动撑满。

```csharp
RectTransform oCol = UiFactory.CreateColumn(parent, "Col", spacing: 12f);
UiFactory.SetHeight(child, 112f);
```

- `CreateColumn` —— 定高容器
- `CreateAutoColumn` —— 高度随内容自适应（放进滚动区用）
- `CreateTopColumn` —— 贴顶定高（页面顶部区域用）
- `CreateCard` —— 白底圆角卡片，高度自适应

### 横向容器 Row
`childControlWidth = true` —— 子元素靠 `preferredWidth` 定宽、`flexibleWidth = 1` 占满剩余。

```csharp
RectTransform oRow = UiFactory.CreateRow(parent, "Row", fHeight: 112f);
UiFactory.SetWidth(left, 140f);       // 定宽
UiFactory.SetFlexible(right);         // 吃掉剩余
```

- `CreateRow` —— 定高、白底圆角、左右带 `CARD_PADDING` 内边距（列表项/表单行的标准形态）
- `CreateBareRow` —— 定高、不带内边距（卡片内部用，避免与卡片内边距叠加）
- `CreateRowContainer` —— 纯横向容器，无高度无内边距

### 滚动区
`CreateScroll(parent, name, out content)`，四个页面和 `PickerDialog` 共用。

- **滚动区自己铺满父容器**，不用调用方 `Stretch`。父容器可以是页面主体，
  也可以是弹窗里已经定好位的列表区
- **返回的 `content` 是内容容器**，已经左右各缩进一个 `PAGE_PADDING`，
  宽度不用自己算；往里塞 `CreateAutoColumn` 之类的自适应高度容器即可
- Viewport 上挂了 `RectMask2D`，**超出部分真的会被裁掉**——滚动区尺寸算错不会报错，
  只会让文字少半截

同名的 `UiFactoryLayoutTests` 锁着这个契约（`Assets/Tests/EditMode/`）。
它只断言锚点能即时决定的尺寸，布局组算出的高度测不了，那些仍要靠 Play 肉眼验。

### ⚠️ 三个已知陷阱

**1. 一个节点上不要同时挂两种 LayoutGroup。**
`HorizontalLayoutGroup` + `VerticalLayoutGroup` 会打架。
需要「上面文字、下面条形」这种竖排结构时，用 `CreateNode` + 手动 `AddComponent<VerticalLayoutGroup>()`，
**不要**先 `CreateRow` 再 `Destroy` 掉它的 HorizontalLayoutGroup——
`Destroy` 延迟到帧末执行，同一帧内两个布局组会共存。

**2. 锚点驱动和布局驱动不要混用。**
父节点有 LayoutGroup 时，子节点的 `anchorMin/anchorMax/anchoredPosition` 会被布局系统覆盖。
想让一个元素参与布局，就把它作为正常子元素加进去（用 `LayoutElement` 控制尺寸），
不要用 `AnchorBottom` 之类去定位。

**3. `Theme` 的颜色是属性，不是字段。**
`Theme.PRIMARY` 每次访问都会走一层属性转发。构建 UI 时无所谓，
但别放在每帧执行的循环里。

---

---

## 字重怎么给（★ 别写 `FontWeight.XXX` 字面量）

`UiFactory` 每个建 Text 的工厂方法（`CreateText` / `CreateButton` / `CreateIconOrText` /
`CreateIconTextButton` / `CreateTabButton` / `CreateInput`）**末尾**都有一个

```csharp
FontWeight eWeight = FontWeight.Regular
```

参数，往后加是为了不破坏全站约 60 个位置参数调用点。**页面里一律传 `Theme.WEIGHT_*`**，
不写字面量——理由同「页面里不许出现写死的颜色字号」：

| 语义常量 | 值 | 用在哪 |
|---|---|---|
| `Theme.WEIGHT_AMOUNT` | Medium | 金额数字（记账框、总资产、列表行金额、各类汇总） |
| `Theme.WEIGHT_TITLE` | Medium | 标题（页标题、月份、弹窗标题） |
| `Theme.WEIGHT_STRONG` | Bold | 主操作按钮（保存、添加账户） |
| `Theme.WEIGHT_BODY` | Regular | 正文与次要说明 **（就是默认值，不用显式传）** |

⚠️ **金额输入框的占位符和可编辑文本是两个 `Text`，共用同一个 `iFontSize`。**
`UiFactory.CreateInput` 会把字重一起给到两边；自己另建 InputField 时忘了给
placeholder，就会出现「一聚焦占位符变粗、一输入又变细」的跳动。

⚠️ **`fontStyle` 不要再写。** 全项目唯一写过它的 `TabBar` 已改成切换真字体文件——
Unity 的合成粗体是给笔画描边，小字号中文会糊。缺字体文件时 `FontProvider` 退回
Regular，不会退回合成粗体。

`FontProvider.Resolve(FontWeight)` 按字重缓存；`Reset()` 清缓存（改完资源配置后调）。
字重→槽位名的映射在 `Core/Typography/FontSlots.cs`，`FontSlotsTests` 盯着。

---

## 页面怎么取数

**唯一的取数点是各页面的 `_refresh()`。** 四个页面都接完了，随便挑一个当范例。

```csharp
private void _refresh()
{
    // 先向仓储取数，再交给 Core 的纯函数算
    List<Transaction> lTxs = AppContext.Instance.Transactions.Query(
        new TransactionQuery { StartMs = iStartMs, EndMs = iEndMs, Limit = 1000 });
    PeriodSummary oSummary = ReportCalculator.BuildSummary(lTxs);
    m_IncomeValue.text = oSummary.Income.ToString();   // Money.ToString() → "35.50"
}
```

⚠️ **`TransactionQuery.Limit` 默认只有 50**，而且超了不报错、只是结果变少。
按月取数就得显式抬高（`TransactionListPage` 与 `ReportPage` 都用 1000）。这个坑很隐蔽：
界面不崩，只是「某个月少了几十条记录」。

`AppContext` 已经在位：`AppContext.Instance` 由 `AppRoot` 在 `Awake` 里初始化，
页面只管用。

页面**不用自己订阅 `DataChanged`**——`AppRoot` 已经统一订阅，收到通知后重走当前页的
`OnShow()`。写数据的一方负责在落库成功后喊一声：

```csharp
oContext.NotifyDataChanged();
```

页面里**只做展示和取数调用**，不要写业务逻辑。校验、聚合都在 Core / Data 层。

更要紧的是：**「一条账单该怎么显示」这种规则也别写在页面里。** 备注为空时显示分类名、
转账不带正负号、分类被删了显示「未分类」——这些是业务约定，不是版式。`TransactionListPage`
把它们放在 `Core/Statements/StatementBuilder.cs`，页面只拿现成的 `Title / Subtitle /
AmountText` 往 `Text` 里塞。理由很实在：EditMode 测试根本跑不到页面，规则留在页面里就只能
靠肉眼看；抽成纯函数才能被 `StatementBuilderTests` 逐条盯住。Task 15 也照这个来：
`AccountEditDialog` 只摆控件，`Core/Accounts/AccountForm.cs` 管类型标签与表单校验
（`AccountFormTests`）。Task 16 同理：`ReportPage` 只摆控件，`Core/Reports/ReportForm.cs`
管构成标题、占比文案与条形宽度（`ReportFormTests`）。

页面之间重复的写法也该往上收：月份标题原先账单页与报表页各拼一遍，现在统一走
`TimeUtil.FormatYearMonth`。

弹窗里还有个容易写错的点：**不要就地改传进来的 `Account`**。它是引用类型，就地改会让
「点取消」变成改了一半的假取消——表单状态放局部变量，点保存再组装一个新对象写库。

---

## 换资源改哪里

| 想改什么 | 改哪里 |
|---|---|
| 尺寸、间距、字号 | `Theme.cs` 上半部分（都是 `const`） |
| 配色 | `Assets/Resources/theme.json` 或 `ThemePalette.cs` |
| 图标名字 | `IconNames.cs` |
| 新增具名资源 | `AssetPaths.cs` |
| 加载逻辑 | `AssetProvider.cs` |
| 组件样式（按钮/卡片/滚动区长什么样） | `UiFactory.cs` |
| 圆角兜底图形的画法 | `SpriteFactory.cs` |
| 系统字体候选顺序 | `FontProvider.cs` |

**页面代码里不应该出现任何写死的颜色、字号或图片路径。**
如果发现要改页面才能换资源，那是个 bug——把那个东西挪进 `Theme` 或 `IconNames`。

---

## 图标 API

```csharp
// 图标（找不到返回 null）
Image oIcon = UiFactory.CreateIcon(parent, "Icon", IconNames.ADD, 48f, Theme.PRIMARY);

// 图标或文字兜底（翻页箭头、行尾 chevron）
UiFactory.CreateIconOrText(parent, "Chevron", IconNames.CHEVRON_RIGHT, ">",
    44f, Theme.FONT_BODY, Theme.TEXT_WEAK);

// 底部标签（图标在上、文字在下）
UiFactory.CreateTabButton(parent, "Tab_x", IconNames.TAB_LIST, "账单", onClick);

// 左图标 + 右文字（「+ 添加账户」）
UiFactory.CreateIconTextButton(parent, "Add", IconNames.ADD, "添加账户", onClick,
    Theme.PRIMARY, Theme.WHITE, Theme.FONT_TITLE, "+ 添加账户");   // 末参是兜底文字

// 把已有按钮的文字标签换成图标
UiFactory.ReplaceButtonLabelWithIcon(oButton, IconNames.CHEVRON_LEFT, 40f, Theme.PRIMARY);

// 列表行首的图标位：有图放图，没图留一个等宽空位（不画）
Image oSlot = UiFactory.CreateIconSlot(oRow, "Icon",
    IconNames.ForCategory(oStatementRow.CategoryIconName),
    Theme.CATEGORY_ICON_SIZE, Theme.TEXT_WEAK);
UiFactory.SetIconSlot(oSlot, IconNames.ForCategory(oCategory?.IconName), Theme.TEXT_WEAK);
```

有了图标之后要改按钮配色，用 `UiFactory.GetButtonIcon(oButton)` 拿到 Image。

**`CreateIconSlot` / `SetIconSlot` 与 `CreateIconOrText` 的分工**：

| | 没图标时 | 返回 | 用来 |
|---|---|---|---|
| `CreateIconOrText` | 建一个 Text 显示兜底文字 | `RectTransform` | 翻页箭头、行尾 chevron——光秃秃一个箭头没了就没法点 |
| `CreateIconSlot` | 整格透明，**位置留着** | `Image` | 列表行首——宁可空一格，也不能让各行文字左边缘参差不齐 |

`CreateIconSlot` 返回 `Image` 而不是 `RectTransform`，是为了能**就地换图**：
记账页选中分类后要往同一格里填图，若改成重建节点，`Destroy` 延迟到帧末，
新旧两个节点会在同一帧的布局里各占一格。要换图一律走 `SetIconSlot`，不要自己
`Destroy` + 重建。

分类图标名是**数据**（存在 `category.icon_name`），解析一律用
`IconNames.ForCategory(...)`——它会检查资源真实存在，找不到返回 `null`，
`SetIconSlot` 拿到 `null` 就把整格调透明。

---

## 卡片

列表行、账户行、报表行这些「圆角卡片底」统一用 `UiFactory.PaintCard`：

```csharp
UiFactory.PaintCard(oRow);   // 圆角 + 卡片色 + 一层投影
```

**别自己写 `AddComponent<Image>` + `SpriteFactory.Card()` + `Sliced`**——这段样板
曾经在三个页面里各抄一遍，加投影时就得改三处，漏一处那块卡片就平贴在底色上，
而且从代码里完全看不出来。

投影走 `UiFactory.AddCardShadow`（UGUI 的 `Shadow` 组件），画在**节点之外**，
不动任何布局尺寸。把投影烘进底图是不行的：九宫格的 border 必须连它一起包住，
图被拉伸到节点尺寸时卡片本体就比节点小一圈，行高、内边距、行间距全得跟着重量一遍。
代价是 `Shadow` 只偏移不模糊、边缘偏硬——想要柔和投影就给美术一张 `card.png`，
`AssetProvider` 会优先用图。

⚠️ **卡片色和页面底色只差十几个色阶**（暖色纸感就是这么设计的），两者的边界
全靠这层投影交代。调整用 `Theme.SHADOW` / `Theme.SHADOW_OFFSET`，
色值来自 `Resources/theme.json` 的 `shadow` 键。

**并不是所有白底都该有投影**：账单页的月份条、汇总条是直角纯色条，标签栏和标题栏
也是——它们是「栏」不是「卡片」，`PaintCard` 只给真正的卡片用。

---

## 选择弹窗

「弹一个列表让用户挑」的交互统一走 `PickerDialog`，不要各写一套：

```csharp
PickerDialog.Show(Root, "选择分类", lLabels, iIndex => { _pick(lCategories[iIndex]); },
    "还没有分类，请先去「账户」页添加");
```

- 第四个参数是选中回调，拿到的是**下标**，不是对象——调用方自己索引回原列表
- 第五个参数是列表为空时的文案，省略则显示「暂无可选项」
- 弹窗挂在传入的父节点下（通常就是页面 `Root`），选完或取消后自行销毁

要带图标的重载，把图标名列表插在选项列表后面：

```csharp
PickerDialog.Show(Root, "选择分类", lLabels, lIcons,
    iIndex => _pickCategory(lCategories[iIndex]), "还没有分类，请先去「账户」页检查");
```

- 图标名列表可以是 `null`（走原重载，不建图标位），元素也可以是 `null`（该项留空位）
- **解析放调用方，不放 PickerDialog**：它是分类/账户/日期/类型共用的通用弹窗，
  让它认识「分类图标」这个概念是错的耦合。页面解析好名字传进来
- 一旦传了列表，**所有行都建图标位**，跟当前这项有没有图无关——否则同一弹窗里
  有图的项和没图的项文字左边缘会错开

## 注意事项

- **`.meta` 文件必须一起提交**
- 改完代码在 Unity Console 确认无编译报错（命令行编译常因编辑器占用跑不了）
- 新增资源目录时记得放 `.gitkeep`，否则 git 不跟踪空目录
