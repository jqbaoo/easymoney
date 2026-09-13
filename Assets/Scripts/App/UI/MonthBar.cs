using System;
using EasyMoney.Core;
using UnityEngine;
using UnityEngine.UI;

namespace EasyMoney.App.UI
{
    /// <summary>
    /// 月份条：左右箭头翻月，中间的年月文字点开「选择月份」弹窗。
    ///
    /// 账单页与报表页都要这一条，原先两页各写一遍（连三个常量都各定义一份），
    /// 再加「点选月份」就是抄第三遍——收在这里，样式和交互只维护一份。
    ///
    /// 年月由本类持有，页面通过 <see cref="Year"/> / <see cref="Month"/> 取，
    /// 变化后经 <c>oOnChanged</c> 通知页面重新取数。
    /// </summary>
    public sealed class MonthBar
    {
        /// <summary>月份条的高度。页面的顶部区域要用它算总高，所以是公开的。</summary>
        public const float HEIGHT = 88f;

        private const float NAV_BUTTON_WIDTH = 88f;
        private const float NAV_ICON_SIZE = 40f;
        private const float CHEVRON_SIZE = 28f;

        /// <summary>
        /// 下拉提示找不到图时的兜底字符。
        ///
        /// 只能用字体子集里真有的字：「▾」「▼」都不在（字体按 GB2312 子集化，
        /// 脚本见 D:\font-tmp\subset.py），真机上会渲染成空白——备注里的 emoji
        /// 就是这么栽的。「↓」在子集里，是安全的。
        /// </summary>
        private const string CHEVRON_FALLBACK = "↓";

        private readonly RectTransform m_DialogParent;
        private readonly Action m_OnChanged;
        private readonly Text m_Label;

        public int Year { get; private set; }
        public int Month { get; private set; }

        /// <param name="oParent">月份条挂在哪（页面顶部的 Top 列）</param>
        /// <param name="oDialogParent">弹窗挂在哪（页面 Root，遮罩要盖住整个内容区）</param>
        /// <param name="oOnChanged">年月变化后的回调，页面在那里刷新数据</param>
        public MonthBar(RectTransform oParent, RectTransform oDialogParent, Action oOnChanged)
        {
            // 构造即定位到当前月。页面实例只 new 一次（PageBase 的 _build 只跑一次），
            // 所以这等价于原先两页那句「首次进来才取当前年月」——
            // 而且不用再靠 m_Year == 0 去分辨「是不是第一次」
            (Year, Month) = TimeUtil.CurrentYearMonth();

            m_DialogParent = oDialogParent;
            m_OnChanged = oOnChanged;

            RectTransform oRow = UiFactory.CreateRowContainer(oParent, "MonthBar");
            UiFactory.SetHeight(oRow, HEIGHT);

            // 月份条是「栏」不是卡片，用直角纯色，不加投影
            oRow.gameObject.AddComponent<Image>().color = Theme.SURFACE;

            UiFactory.CreateNavButton(oRow, "Prev", IconNames.CHEVRON_LEFT, "<",
                NAV_BUTTON_WIDTH, NAV_ICON_SIZE, () => _go(-1));

            // 接返回值而不是在 _buildMonthButton 里直接给 m_Label 赋值：
            // readonly 字段只允许在构造函数体内赋值，在被调用的方法里赋是编译错误
            m_Label = _buildMonthButton(oRow);

            UiFactory.CreateNavButton(oRow, "Next", IconNames.CHEVRON_RIGHT, ">",
                NAV_BUTTON_WIDTH, NAV_ICON_SIZE, () => _go(1));

            _refreshLabel();
        }

        private void _go(int iDelta)
        {
            (Year, Month) = TimeUtil.AddMonths(Year, Month, iDelta);
            _refreshLabel();
            m_OnChanged?.Invoke();
        }

        private void _showPicker()
        {
            MonthPickerDialog.Show(m_DialogParent, Year, Month, (iYear, iMonth) =>
            {
                Year = iYear;
                Month = iMonth;
                _refreshLabel();
                m_OnChanged?.Invoke();
            });
        }

        private void _refreshLabel()
        {
            m_Label.text = TimeUtil.FormatYearMonth(Year, Month);
        }

        /// <summary>
        /// 中间那块：年月文字 + 下拉小箭头，整块可点。
        ///
        /// 不用 CreateButton：它内部的 Label 是 Stretch 铺满居中的，旁边塞不下箭头，
        /// 而给那个 Label 再加布局组会跟 Stretch 的锚点打架。
        /// 这里自己搭「透明射线靶 + 内部横排」，与 UiFactory.CreateIconTextButton 同一种做法。
        /// </summary>
        private Text _buildMonthButton(RectTransform oRow)
        {
            // CreateRowContainer 只有布局组、没有 Graphic，接不住点击，补一层透明射线靶
            Image oHit = UiFactory.CreatePanel(oRow, "Month", Theme.TRANSPARENT);
            UiFactory.SetFlexible(oHit.rectTransform);

            Button oButton = oHit.gameObject.AddComponent<Button>();
            oButton.targetGraphic = oHit;
            oButton.onClick.AddListener(_showPicker);

            RectTransform oBody = UiFactory.CreateRowContainer(oHit.transform, "Body", 8f);
            UiFactory.Stretch(oBody);

            // 整组居中：文字左边、箭头紧跟其后，两者加起来才是这一块的中心
            HorizontalLayoutGroup oBodyLayout = oBody.GetComponent<HorizontalLayoutGroup>();
            oBodyLayout.childAlignment = TextAnchor.MiddleCenter;
            oBodyLayout.childForceExpandWidth = false;

            Text oLabel = UiFactory.CreateText(oBody, "Label", string.Empty,
                Theme.FONT_TITLE, TextAnchor.MiddleLeft, Theme.TEXT, Theme.WEIGHT_TITLE);

            // 有 chevron_down.png 就用图，没有就显示「↓」——美术补图后自动生效，代码不用改
            UiFactory.CreateIconOrText(oBody, "Chevron", IconNames.CHEVRON_DOWN, CHEVRON_FALLBACK,
                CHEVRON_SIZE, Theme.FONT_CAPTION, Theme.TEXT_WEAK);

            return oLabel;
        }
    }
}
