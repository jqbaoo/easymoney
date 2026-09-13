using EasyMoney.App.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace EasyMoney.Tests
{
    /// <summary>
    /// 下拉列表测试。
    ///
    /// 这一条是「能测的界面逻辑就往下沉成组件」的又一例——它本来会写在报表页里
    /// （按钮 + 面板 + 遮罩 + 选中回调），而页面跑不到 EditMode。
    ///
    /// ⚠️ 只验「弹没弹、选没选中、关没关掉」。面板浮在按钮下方**哪个位置**是靠
    /// RectTransformUtility 换算出来的，依赖布局跑过一轮，EditMode 里量不了，
    /// 那部分只能 Play 里看。
    /// </summary>
    public class DropdownButtonTests
    {
        private const float TOLERANCE = 0.001f;

        private GameObject m_Root;
        private RectTransform m_Parent;
        private int m_PickedIndex;
        private int m_PickCount;

        [SetUp]
        public void SetUp()
        {
            Theme.Apply(ThemePalette.Light());

            m_Root = new GameObject("TestRoot", typeof(RectTransform));
            m_Parent = m_Root.GetComponent<RectTransform>();
            m_Parent.sizeDelta = new Vector2(Theme.REF_WIDTH, Theme.REF_HEIGHT);

            m_PickedIndex = -1;
            m_PickCount = 0;
        }

        [TearDown]
        public void TearDown()
        {
            // EditMode 下 GameObject 不会随场景销毁，必须手动清
            Object.DestroyImmediate(m_Root);
        }

        // ── 初始状态 ────────────────────────────────

        [Test]
        public void Constructor_ShowsCurrentOption()
        {
            _create(1, "条形图", "环形图");

            Assert.AreEqual("环形图", _label().text, "按钮上显示的是当前选中项，不是第一项");
        }

        [Test]
        public void Constructor_ShowsFallbackChevron()
        {
            _create(0, "条形图", "环形图");

            Transform oChevron = m_Parent.Find("Dropdown/Body/Chevron");
            Assert.IsNotNull(oChevron, "按钮上该有个小箭头，提示这里能点开");

            // chevron_down.png 还没有，应当退化成兜底文字。
            // 兜底只能用「↓」——「▾」「▼」不在 GB2312 字体子集里，真机上渲染成空白
            Assert.AreEqual("↓", oChevron.GetComponent<Text>().text);
        }

        [Test]
        public void Constructor_DoesNotNotifyAndStaysClosed()
        {
            DropdownButton oDropdown = _create(0, "条形图", "环形图");

            Assert.AreEqual(0, m_PickCount, "只是把下拉建出来不该惊动页面");
            Assert.IsFalse(oDropdown.IsOpen);
        }

        [Test]
        public void Constructor_ClampsOutOfRangeIndex()
        {
            _create(99, "条形图", "环形图");
            Assert.AreEqual("环形图", _label().text, "下标越界往后钳到最后一项");

            Object.DestroyImmediate(m_Parent.Find("Dropdown").gameObject);
            _create(-5, "条形图", "环形图");
            Assert.AreEqual("条形图", _label().text, "下标越界往前钳到第一项");
        }

        // ── 展开 ────────────────────────────────────

        [Test]
        public void Open_ShowsOneRowPerOption()
        {
            DropdownButton oDropdown = _create(0, "条形图", "环形图");

            _button().onClick.Invoke();

            Assert.IsTrue(oDropdown.IsOpen);
            Assert.IsNotNull(m_Parent.Find("DropdownOverlay/Panel"), "面板该弹在遮罩下面");
            Assert.AreEqual("条形图", m_Parent.Find("DropdownOverlay/Panel/Option_0/Label")
                .GetComponent<Text>().text);
            Assert.AreEqual("环形图", m_Parent.Find("DropdownOverlay/Panel/Option_1/Label")
                .GetComponent<Text>().text);
            Assert.IsNull(m_Parent.Find("DropdownOverlay/Panel/Option_2"),
                "选项只有两项，不该多出一行");
        }

        [Test]
        public void Open_MarksCurrentOptionWithPrimaryColor()
        {
            _create(1, "条形图", "环形图");

            _button().onClick.Invoke();

            _assertColor(Theme.TEXT, _optionLabel(0).color, "没选中的项用普通文字色");
            _assertColor(Theme.PRIMARY, _optionLabel(1).color, "当前项要用主色标出来");
        }

        [Test]
        public void Open_UsesTransparentBlocker()
        {
            _create(0, "条形图", "环形图");

            _button().onClick.Invoke();

            // 挡板是接住「点空白就收起」的那一层：透明但必须能挡射线，
            // 不透明或者 raycastTarget 关掉都会让点击穿透到下面的页面上去
            Image oBlocker = m_Parent.Find("DropdownOverlay").GetComponent<Image>();
            Assert.IsNotNull(oBlocker);
            Assert.AreEqual(0f, oBlocker.color.a, TOLERANCE);
            Assert.IsTrue(oBlocker.raycastTarget);
        }

        [Test]
        public void Open_WhenAlreadyOpen_DoesNotStackSecondPanel()
        {
            _create(0, "条形图", "环形图");

            _button().onClick.Invoke();
            _button().onClick.Invoke();

            int iOverlays = 0;
            foreach (Transform oChild in m_Parent)
            {
                if (oChild.name == "DropdownOverlay")
                {
                    iOverlays++;
                }
            }

            Assert.AreEqual(1, iOverlays, "开着的时候再点一次不该叠出第二块面板");
        }

        // ── 选中 ────────────────────────────────────

        [Test]
        public void Pick_NotifiesWithIndexUpdatesLabelAndCloses()
        {
            DropdownButton oDropdown = _create(0, "条形图", "环形图");
            _button().onClick.Invoke();

            _optionButton(1).onClick.Invoke();

            Assert.AreEqual(1, m_PickedIndex, "回调拿到的是下标，调用方自己索引回原列表");
            Assert.AreEqual(1, m_PickCount, "只通知一次");
            Assert.AreEqual(1, oDropdown.CurrentIndex);
            Assert.AreEqual("环形图", _label().text, "选完按钮上的字要跟着换");
            Assert.IsFalse(oDropdown.IsOpen);
            Assert.IsNull(m_Parent.Find("DropdownOverlay"), "选完面板要收掉");
        }

        [Test]
        public void Pick_CurrentOption_ClosesWithoutNotifying()
        {
            DropdownButton oDropdown = _create(0, "条形图", "环形图");
            _button().onClick.Invoke();

            _optionButton(0).onClick.Invoke();

            Assert.AreEqual(0, m_PickCount,
                "选中的还是当前那项，没必要让页面白刷一次（报表页那个回调会重新查库）");
            Assert.IsFalse(oDropdown.IsOpen, "但面板还是要收起来");
        }

        [Test]
        public void OpenAgainAfterPick_ShowsNewCurrentOption()
        {
            _create(0, "条形图", "环形图");
            _button().onClick.Invoke();
            _optionButton(1).onClick.Invoke();

            _button().onClick.Invoke();

            _assertColor(Theme.PRIMARY, _optionLabel(1).color, "换过之后高亮要跟着挪");
        }

        // ── 点空白收起 ──────────────────────────────

        [Test]
        public void ClickOutside_ClosesWithoutNotifying()
        {
            DropdownButton oDropdown = _create(0, "条形图", "环形图");
            _button().onClick.Invoke();

            m_Parent.Find("DropdownOverlay").GetComponent<Button>().onClick.Invoke();

            Assert.IsFalse(oDropdown.IsOpen);
            Assert.IsNull(m_Parent.Find("DropdownOverlay"));
            Assert.AreEqual(0, m_PickCount, "点空白收起不该当成选中了什么");
        }

        // ── 辅助 ────────────────────────────────────

        private DropdownButton _create(int iCurrentIndex, params string[] lOptions)
        {
            return new DropdownButton(m_Parent, m_Parent, lOptions, iCurrentIndex, i =>
            {
                m_PickedIndex = i;
                m_PickCount++;
            });
        }

        private Button _button()
        {
            Transform oNode = m_Parent.Find("Dropdown");
            Assert.IsNotNull(oNode, "找不到下拉按钮本身");
            return oNode.GetComponent<Button>();
        }

        private Text _label()
        {
            Transform oNode = m_Parent.Find("Dropdown/Body/Label");
            Assert.IsNotNull(oNode, "找不到下拉按钮上的文字");
            return oNode.GetComponent<Text>();
        }

        private Text _optionLabel(int iIndex)
        {
            Transform oNode = m_Parent.Find($"DropdownOverlay/Panel/Option_{iIndex}/Label");
            Assert.IsNotNull(oNode, $"面板里找不到第 {iIndex} 项");
            return oNode.GetComponent<Text>();
        }

        private Button _optionButton(int iIndex)
        {
            Transform oNode = m_Parent.Find($"DropdownOverlay/Panel/Option_{iIndex}");
            Assert.IsNotNull(oNode, $"面板里找不到第 {iIndex} 项");
            return oNode.GetComponent<Button>();
        }

        private static void _assertColor(Color oExpected, Color oActual, string sMessage)
        {
            Assert.AreEqual(oExpected.r, oActual.r, TOLERANCE, sMessage);
            Assert.AreEqual(oExpected.g, oActual.g, TOLERANCE, sMessage);
            Assert.AreEqual(oExpected.b, oActual.b, TOLERANCE, sMessage);
        }
    }
}
