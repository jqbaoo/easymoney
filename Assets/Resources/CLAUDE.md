# Assets/Resources — 资源目录

**换美术资源只碰这个目录，不改任何代码。**

完整指南（每个图标的文件名、显示尺寸、分类 slug 表、九宫格 border 怎么切）见
`Claude/资源替换指南.md`。本文件是速查。

---

## 目录约定

```
Assets/Resources/
├── Icons/          图标（PNG，透明底）
├── Sprites/        卡片 / 按钮九宫格底图
├── Fonts/          字体（main / main_medium / main_bold 三档字重）
└── theme.json      配色
```

文件名不带扩展名引用。`Resources.Load` 的路径是相对 `Assets/Resources/` 的，
所以 `Icons/chevron_right.png` 在代码里叫 `chevron_right`。

---

## 具名资源

| 路径 | 用途 | 缺失时的兜底 |
|---|---|---|
| `Icons/<名字>.png` | 图标 | 文字符号（`<` `>` `+`）或纯文字标签 |
| `Icons/<名字>_on.png` | 标签栏选中态（可选） | 把普通图标染成主色 |
| `Sprites/card.png` | 卡片九宫格底图 | 程序化生成圆角矩形 |
| `Sprites/button.png` | 按钮九宫格底图 | 复用 `card.png` |
| `Fonts/main.otf` | 主字体 Regular（正文、次要说明） | 系统字体 → Unity 内置字体 |
| `Fonts/main_medium.otf` | 主字体 Medium（金额、标题） | 退回 Regular |
| `Fonts/main_bold.otf` | 主字体 Bold（标签栏选中、主按钮） | 退回 Regular |
| `Fonts/LICENSE-NotoSansSC.txt` | 字体许可证 | —— |
| `theme.json` | 配色 | 内置浅色主题 |

资源名常量在 `Assets/Scripts/App/UI/AssetPaths.cs` 和 `IconNames.cs`。

---

## 字体

当前是 **Noto Sans SC**（思源黑体同源），SIL OFL 1.1，可商用、可随 APK 分发——
所以这三份许可证文件**必须跟着走**，删了就是许可证违约。

文件名后缀 `.otf` 不影响引用：`Resources.Load` 不看扩展名，代码里仍然叫
`main` / `main_medium` / `main_bold`。

**导入后 Inspector 里 `Character` 保持默认的 `Dynamic`。** 改成 `Unicode`
会尝试烘焙全字符集——卡死编辑器，包体也会爆炸。

三档字重合计约 5.7 MB，是 APK 里最大的一块资源。**砍掉 Bold 能省 1.9 MB**，
但标签栏选中态和两个主按钮会退回 Regular，层次靠字号硬撑。

换字体只改这个目录，不动代码：槽位名由 `Core/Typography/FontSlots.cs` 定义，
字重分配由 `Theme.cs` 的四个语义常量（`WEIGHT_AMOUNT` / `WEIGHT_TITLE` /
`WEIGHT_BODY` / `WEIGHT_STRONG`）决定。字体文件缺失时逐级回退，不会崩也不会变方块
（回退到系统字体那一级为止）。

子集化脚本在 `D:\font-tmp\subset.py`（fonttools），裁的是「GB2312 汉字 +
ASCII + 中文标点 + 货币符号」共 7594 字。**超出这个范围的生僻字会渲染成方块**，
要补就改脚本重跑。

---

## 图标清单

| 文件名 | 用在哪 | 显示尺寸 | 兜底 |
|---|---|---|---|
| `tab_record.png` | 底部标签「记一笔」 | 48 | 纯文字标签 |
| `tab_list.png` | 底部标签「账单」 | 48 | 纯文字标签 |
| `tab_account.png` | 底部标签「账户」 | 48 | 纯文字标签 |
| `tab_report.png` | 底部标签「报表」 | 48 | 纯文字标签 |
| `chevron_left.png` | 上一月 | 40 | 文字 `<` |
| `chevron_right.png` | 下一月 / 行尾箭头 | 40 / 44 | 文字 `>` |
| `icon_add.png` | 「添加账户」 | 38 | 文字「+ 添加账户」 |
| `icon_edit.png` | 账户行的「改」 | 36 | 文字「改」 |
| `icon_delete.png` | 账单行的「删」 | 36 | 文字「删」 |
| `cat_*.png` | 分类图标 | 40 | 留一个等宽空位，不画 |

**建议规格**：统一 `128×128` PNG，透明背景，**单色**（白或黑）。

分类图标用 `cat_<slug>`，slug 表和数据库 `category.icon_name` 字段一致
（`cat_food` / `cat_shopping` / `cat_salary` …），完整表见资源替换指南。

---

## 九宫格底图怎么切

选中图片 → Inspector → `Sprite Editor` → 拖拽 Border 的四条边 → Apply。

- 左右 border 至少要盖住圆角半径（当前 16pt，即 2 倍图上的 32px）
- 上下同理
- 中间留一段可拉伸区域

**没设 border 的话，图片会被整体拉伸，圆角会变形。**

---

## 为什么建议图标做成单色

标签栏的选中态靠**代码染色**实现：选中染主色、未选中染弱化色。
图标本身是彩色的，染色会把它压成一片纯色。

想保留彩色图标就再提供一张 `_on` 选中态图，代码会优先用它。

---

## theme.json

```json
{
  "background":  "#F2ECE2",
  "surface":     "#FFFCF6",
  "primary":     "#A9714B",
  "expense":     "#C0523C",
  "income":      "#4F8A5B",
  "textPrimary": "#2B2620",
  "textWeak":    "#8A8177",
  "divider":     "#E7DFD2",
  "barTrack":    "#E7DFD2",
  "scrim":       "#00000073",
  "shadow":      "#46311C1A"
}
```

当前是**暖色纸感**：燕麦米白的底、暖白的卡片、焦糖棕的主色。底色和卡片色只差
十几个色阶，两者之间的边界靠 `shadow` 那层投影交代——**别把 shadow 调没了**，
不然整页会糊成一片白。

格式 `#RRGGBB` 或 `#RRGGBBAA`。**可以只写想改的项**，没写的自动用默认值。
格式不合法也只是该项退回默认，不会崩。

⚠️ **这个文件和 `ThemePalette.Light()` 是同一份配色的两个副本。** 运行时由
`theme.json` 覆盖代码里的值，所以改了这里不改代码，平时看不出问题——只有当
`theme.json` 缺失或解析失败、`Light()` 那套露脸时才会突然变样。
`ThemePaletteTests.ShippedThemeJson_MatchesLightPalette` 逐字段钉着两者一致，
**改配色时两处都要改**（深色配色只在 `ThemePalette.Dark()` 里，不受这个文件影响）。

深色模式：`ThemePalette.Dark()` 已就绪，调 `Theme.Apply(ThemePalette.Dark())`
界面会自动重建。目前还没接切换入口（等设置页）。

---

## 注意事项

- **`.meta` 文件必须一起提交**，漏了会导致引用全断
- 图片 `Texture Type` 保持默认的 `Sprite (2D and UI)`
- Play 模式下新丢的图片不会自动生效（静态缓存），重新 Play 一次
- 空目录用 `.gitkeep` 占位（Unity 忽略 `.` 开头的文件，不会为它生成 meta）
