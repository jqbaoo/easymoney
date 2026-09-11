# EasyMoney.App — 应用层

**能碰 UnityEngine 和 UI 的唯一一层。** 上层没有别的东西了，下层是 Data / Core。

命名空间 `EasyMoney.App`，程序集 `EasyMoney.App`，引用 `UnityEngine.UI`。

---

## 目录

```
App/
├── EasyMoney.App.asmdef
├── AppRoot.cs          应用入口 + 界面骨架 + 标签栏装配
├── DemoData.cs         ⚠️ 假数据。接数据层后整个文件删掉
└── UI/
    ├── Theme.cs            尺寸 / 字号 / 颜色（颜色转发给 ThemePalette）
    ├── ThemePalette.cs     配色对象，含 Light() / Dark() / FromJson()
    ├── UiFactory.cs        ★ 所有 UI 构件的工厂
    ├── SpriteFactory.cs    卡片 / 按钮底图（资源优先，程序化兜底）
    ├── FontProvider.cs     字体（Resources → 系统 → 内置）
    ├── AssetPaths.cs       资源路径常量
    ├── AssetProvider.cs    唯一的 Resources.Load 入口
    ├── IconNames.cs        图标名常量
    ├── SafeAreaFitter.cs   安全区适配
    ├── PageBase.cs         页面抽象基类
    ├── PageRouter.cs       页面注册与切换
    ├── TabBar.cs           底部标签栏
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

## 接数据层时改哪里

**唯一的改动点是各页面的 `_refresh()`。**

把 `DemoData.BuildXxx()` 换成 `AppContext.Instance` 的真实取数：

```csharp
private void _refresh()
{
    // 现在：
    m_IncomeValue.text = DemoData.SUMMARY_INCOME;

    // 接数据层后：
    PeriodSummary oSummary = AppContext.Instance.ReportCalculator.Summarize(...);
    m_IncomeValue.text = Money.FromCents(oSummary.IncomeCents).ToString();
}
```

然后：

1. 删掉 `DemoData.cs`
2. 新建 `AppContext.cs`（计划 Task 12 有完整代码）
3. 页面订阅 `AppContext.Instance.DataChanged`，数据变了自动刷新

页面里**只做展示和取数调用**，不要写业务逻辑。校验、聚合都在 Core / Data 层。

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
```

有了图标之后要改按钮配色，用 `UiFactory.GetButtonIcon(oButton)` 拿到 Image。

---

## 注意事项

- **`.meta` 文件必须一起提交**
- 改完代码在 Unity Console 确认无编译报错（命令行编译常因编辑器占用跑不了）
- 新增资源目录时记得放 `.gitkeep`，否则 git 不跟踪空目录
