using UnityEngine;
using UnityEngine.UI;

namespace EasyMoney.App.UI
{
    /// <summary>
    /// 「选中 / 未选中」这套配色的唯一出处：报表页的支出/收入切换、记账页的
    /// 支出/收入/转账切换都用它。
    ///
    /// 收成一处是有来历的——这段样板原先在报表页与记账页各抄了一遍，抄第二遍时
    /// 把收入那个按钮的三元表达式写反了分支：未选中时底是 SURFACE（暖白）、字却是
    /// WHITE，白底白字。它不报错、不崩溃，只是那个按钮看上去「没有文字」。
    /// 现在调用方传的是「选没选中」而不是两组颜色，这类抄错结构性地不可能再犯。
    ///
    /// ⚠️ 选择月份弹窗（<see cref="MonthPickerDialog"/>）**不走这里**：它的未选中底
    /// 用的是页面底色而不是卡片色（弹窗本身就是卡片，格子再用卡片色就分不出来了），
    /// 是另一种场景，不是这条规则漏了一个调用方。
    /// </summary>
    public static class TogglePalette
    {
        public static Color Background(bool bSelected)
        {
            return bSelected ? Theme.PRIMARY : Theme.SURFACE;
        }

        public static Color Label(bool bSelected)
        {
            return bSelected ? Theme.WHITE : Theme.TEXT;
        }

        /// <summary>把选中态画到按钮上。底与字一起给，免得只画一半。</summary>
        public static void Apply(Button oButton, bool bSelected)
        {
            UiFactory.PaintButton(oButton, Background(bSelected), Label(bSelected));
        }
    }
}
