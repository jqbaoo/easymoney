using EasyMoney.App.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace EasyMoney.Tests
{
    /// <summary>
    /// 「选中 / 未选中」这套配色。
    ///
    /// 原先这段样板在报表页与记账页各抄了一遍，抄第二遍时把收入那个按钮的三元表达式
    /// 写反了——未选中时是暖白底配白字，肉眼看着就是「这个按钮没有文字」，而且不报错、
    /// 跑测试也不会红（那时候它压根没有测试）。收成 TogglePalette 之后，调用方传的是
    /// 「选没选中」而不是两组颜色，这类抄错结构性地不可能再犯。
    /// </summary>
    public class TogglePaletteTests
    {
        private const float TOLERANCE = 0.001f;

        /// <summary>
        /// WCAG AA 对 UI 组件（非正文）的最低对比度要求。选它有出处，
        /// 比拍一个「亮度差要大于多少」稳——后者调一次配色就得重拍一遍。
        /// </summary>
        private const float MIN_CONTRAST = 3f;

        private GameObject m_Root;

        [SetUp]
        public void SetUp()
        {
            Theme.Apply(ThemePalette.Light());
            m_Root = new GameObject("TestRoot", typeof(RectTransform));
        }

        [TearDown]
        public void TearDown()
        {
            // EditMode 下 GameObject 不会随场景销毁，必须手动清
            Object.DestroyImmediate(m_Root);
        }

        // ── 取色 ────────────────────────────────────

        [Test]
        public void Selected_UsesPrimaryBackgroundWithWhiteLabel()
        {
            _assertColor(Theme.PRIMARY, TogglePalette.Background(true), "选中态的底该是主色");
            _assertColor(Theme.WHITE, TogglePalette.Label(true), "主色底上要用白字才读得出来");
        }

        [Test]
        public void Unselected_UsesSurfaceBackgroundWithBodyTextLabel()
        {
            _assertColor(Theme.SURFACE, TogglePalette.Background(false), "未选中态是普通卡片底");
            _assertColor(Theme.TEXT, TogglePalette.Label(false), "卡片底上要用正文色");
        }

        [Test]
        public void Unselected_LabelIsReadableOnBackground()
        {
            // 这条才是原 bug 的形状：底色和字色**各自**都合法，配在一起却看不见。
            // 只断言「等于某个常量」的话，两个常量一起写错就漏过去了
            _assertReadable(false);
        }

        [Test]
        public void Selected_LabelIsReadableOnBackground()
        {
            // 反向的一条：选中态是主色底，字要是也跟着用正文色就成了深底深字
            _assertReadable(true);
        }

        // ── 落到控件上 ──────────────────────────────

        [Test]
        public void Apply_Selected_PaintsBackgroundAndLabel()
        {
            Button oButton = _button();

            TogglePalette.Apply(oButton, true);

            _assertColor(Theme.PRIMARY, _background(oButton).color, "按钮底没画成主色");
            _assertColor(Theme.WHITE, UiFactory.GetButtonLabel(oButton).color, "按钮文字没画成白色");
        }

        [Test]
        public void Apply_Unselected_PaintsBackgroundAndLabel()
        {
            // 上一条走选中态，这条走未选中态——原 bug 正好**只在未选中态露脸**，
            // 只测选中态的话它是绿的
            Button oButton = _button();

            TogglePalette.Apply(oButton, false);

            _assertColor(Theme.SURFACE, _background(oButton).color, "按钮底没画成卡片色");
            _assertColor(Theme.TEXT, UiFactory.GetButtonLabel(oButton).color,
                "未选中态的文字没画成正文色——原来这里画的是白色，在暖白底上看不见");
        }

        [Test]
        public void Apply_SwitchesBackAndForth()
        {
            // 报表页每次重刷都把两个按钮各画一遍（一个选中、一个未选中），
            // 所以同一条规则必须能来回切，不能只认第一次画上去的那个
            Button oButton = _button();

            TogglePalette.Apply(oButton, true);
            TogglePalette.Apply(oButton, false);

            _assertColor(Theme.SURFACE, _background(oButton).color, "切回未选中态时底色没跟着回去");
            _assertColor(Theme.TEXT, UiFactory.GetButtonLabel(oButton).color, "切回未选中态时字色没跟着回去");
        }

        // ── 辅助 ────────────────────────────────────

        private void _assertReadable(bool bSelected)
        {
            float fContrast = _contrast(TogglePalette.Background(bSelected), TogglePalette.Label(bSelected));

            Assert.GreaterOrEqual(fContrast, MIN_CONTRAST,
                $"{(bSelected ? "选中" : "未选中")}态的底与字对比度只有 {fContrast:F2}，字会看不清");
        }

        /// <summary>
        /// WCAG 的对比度：1 = 完全看不见，21 = 黑白。要先做 gamma 校正——
        /// 直接平均 RGB 的话纯蓝和纯黄算出来一样亮，而人眼看它们差得远。
        /// </summary>
        private static float _contrast(Color oFirst, Color oSecond)
        {
            float fA = _relativeLuminance(oFirst);
            float fB = _relativeLuminance(oSecond);

            return (Mathf.Max(fA, fB) + 0.05f) / (Mathf.Min(fA, fB) + 0.05f);
        }

        private static float _relativeLuminance(Color oColor)
        {
            return 0.2126f * _linearize(oColor.r)
                + 0.7152f * _linearize(oColor.g)
                + 0.0722f * _linearize(oColor.b);
        }

        private static float _linearize(float fChannel)
        {
            return fChannel <= 0.03928f
                ? fChannel / 12.92f
                : Mathf.Pow((fChannel + 0.055f) / 1.055f, 2.4f);
        }

        private Button _button()
        {
            return UiFactory.CreateButton(m_Root.transform, "Tab", "支出", null,
                Theme.SURFACE, Theme.FONT_BODY);
        }

        private static Image _background(Button oButton)
        {
            return oButton.targetGraphic as Image;
        }

        private static void _assertColor(Color oExpected, Color oActual, string sMessage)
        {
            Assert.AreEqual(oExpected.r, oActual.r, TOLERANCE, sMessage);
            Assert.AreEqual(oExpected.g, oActual.g, TOLERANCE, sMessage);
            Assert.AreEqual(oExpected.b, oActual.b, TOLERANCE, sMessage);
        }
    }
}
