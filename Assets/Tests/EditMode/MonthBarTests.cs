using System.Text.RegularExpressions;
using EasyMoney.App.UI;
using EasyMoney.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace EasyMoney.Tests
{
    /// <summary>
    /// 月份条的翻页、文案与「点开弹窗选月」的联动测试。
    ///
    /// 这一条原先在账单页和报表页各写了一份，而页面跑不到 EditMode 测试——
    /// 收成组件之后，这些行为才第一次被测试盯上。
    /// </summary>
    public class MonthBarTests
    {
        private GameObject m_Root;
        private RectTransform m_Parent;
        private int m_ChangeCount;

        [SetUp]
        public void SetUp()
        {
            Theme.Apply(ThemePalette.Light());

            m_Root = new GameObject("TestRoot", typeof(RectTransform));
            m_Parent = m_Root.GetComponent<RectTransform>();
            m_Parent.sizeDelta = new Vector2(Theme.REF_WIDTH, Theme.REF_HEIGHT);
            m_ChangeCount = 0;
        }

        [TearDown]
        public void TearDown()
        {
            // EditMode 下 GameObject 不会随场景销毁，必须手动清
            Object.DestroyImmediate(m_Root);
        }

        // ── 初始状态 ────────────────────────────────

        [Test]
        public void Constructor_StartsAtCurrentMonth()
        {
            MonthBar oBar = _createBar();
            (int iYear, int iMonth) = TimeUtil.CurrentYearMonth();

            Assert.AreEqual(iYear, oBar.Year);
            Assert.AreEqual(iMonth, oBar.Month, "月份条建出来就该停在当前月");
        }

        [Test]
        public void Constructor_ShowsYearMonthLabel()
        {
            MonthBar oBar = _createBar();

            Assert.AreEqual(TimeUtil.FormatYearMonth(oBar.Year, oBar.Month), _label().text,
                "月份条的文字要与页面里的年月格式一致（月份不补零）");
        }

        [Test]
        public void Constructor_DoesNotNotify()
        {
            _createBar();

            Assert.AreEqual(0, m_ChangeCount, "只是把条建出来不该让页面白刷一次");
        }

        // ── 翻月 ────────────────────────────────────

        [Test]
        public void Next_AdvancesOneMonthAndNotifies()
        {
            MonthBar oBar = _createBar();
            (int iYear, int iMonth) = (oBar.Year, oBar.Month);

            _navButton("Next").onClick.Invoke();

            Assert.AreEqual(TimeUtil.AddMonths(iYear, iMonth, 1), (oBar.Year, oBar.Month));
            Assert.AreEqual(TimeUtil.FormatYearMonth(oBar.Year, oBar.Month), _label().text,
                "翻月之后条上的文字要跟着变");
            Assert.AreEqual(1, m_ChangeCount, "翻月要通知页面重新取数，且只通知一次");
        }

        [Test]
        public void Prev_GoesBackOneMonthAndNotifies()
        {
            MonthBar oBar = _createBar();
            (int iYear, int iMonth) = (oBar.Year, oBar.Month);

            _navButton("Prev").onClick.Invoke();

            Assert.AreEqual(TimeUtil.AddMonths(iYear, iMonth, -1), (oBar.Year, oBar.Month));
            Assert.AreEqual(1, m_ChangeCount);
        }

        [Test]
        public void Next_AcrossDecember_RollsIntoNextYear()
        {
            MonthBar oBar = _createBar();

            // 当前是几月不能假定，先翻到 12 月再验证跨年那一步
            while (oBar.Month != 12)
            {
                _navButton("Next").onClick.Invoke();
            }

            int iYearAtDecember = oBar.Year;
            _navButton("Next").onClick.Invoke();

            Assert.AreEqual(1, oBar.Month);
            Assert.AreEqual(iYearAtDecember + 1, oBar.Year, "12 月的下一个月是次年 1 月");
        }

        [Test]
        public void Prev_AcrossJanuary_BorrowsFromPreviousYear()
        {
            MonthBar oBar = _createBar();

            while (oBar.Month != 1)
            {
                _navButton("Prev").onClick.Invoke();
            }

            int iYearAtJanuary = oBar.Year;
            _navButton("Prev").onClick.Invoke();

            Assert.AreEqual(12, oBar.Month);
            Assert.AreEqual(iYearAtJanuary - 1, oBar.Year, "1 月的上一个月是上年 12 月");
        }

        // ── 点开弹窗选月 ────────────────────────────

        [Test]
        public void MonthButton_ShowsFallbackChevron()
        {
            _createBar();

            Transform oChevron = m_Parent.Find("MonthBar/Month/Body/Chevron");
            Assert.IsNotNull(oChevron, "年月文字右边该有个小箭头，提示这里能点开");

            // chevron_down.png 还没有，应当退化成兜底文字。
            // 兜底只能用「↓」——「▾」「▼」不在 GB2312 字体子集里，真机上渲染成空白
            Assert.AreEqual("↓", oChevron.GetComponent<Text>().text);
        }

        [Test]
        public void MonthButton_OpensPickerDialog()
        {
            _createBar();

            _monthButton().onClick.Invoke();

            Assert.IsNotNull(m_Parent.Find("MonthPickerOverlay"),
                "点年月文字应当弹出选择月份");
        }

        [Test]
        public void PickMonthFromDialog_UpdatesLabelAndNotifies()
        {
            MonthBar oBar = _createBar();
            int iYear = oBar.Year;

            _monthButton().onClick.Invoke();

            // 选完月份会关弹窗，而编辑模式下 Object.Destroy 会打一条 Error——
            // 详见 MonthPickerDialogTests._expectEditModeDestroy 的说明
            LogAssert.Expect(LogType.Error, new Regex("Destroy may not be called from edit mode"));
            m_Parent.Find("MonthPickerOverlay/Panel/Row0/Month3")
                .GetComponent<Button>().onClick.Invoke();

            Assert.AreEqual(3, oBar.Month, "弹窗里选了 3 月，月份条要跟着跳过去");
            Assert.AreEqual(iYear, oBar.Year, "没切年份，年不该变");
            Assert.AreEqual(TimeUtil.FormatYearMonth(iYear, 3), _label().text);
            Assert.AreEqual(1, m_ChangeCount, "选完月份要通知页面重新取数，且只通知一次");
        }

        // ── 辅助 ────────────────────────────────────

        private MonthBar _createBar()
        {
            return new MonthBar(m_Parent, m_Parent, () => m_ChangeCount++);
        }

        private Button _navButton(string sName)
        {
            Transform oNode = m_Parent.Find($"MonthBar/{sName}");
            Assert.IsNotNull(oNode, $"月份条里找不到 {sName} 箭头");
            return oNode.GetComponent<Button>();
        }

        private Button _monthButton()
        {
            Transform oNode = m_Parent.Find("MonthBar/Month");
            Assert.IsNotNull(oNode, "月份条里找不到年月文字那块");
            return oNode.GetComponent<Button>();
        }

        private Text _label()
        {
            Transform oNode = m_Parent.Find("MonthBar/Month/Body/Label");
            Assert.IsNotNull(oNode, "找不到月份条上的年月文字");
            return oNode.GetComponent<Text>();
        }
    }
}
