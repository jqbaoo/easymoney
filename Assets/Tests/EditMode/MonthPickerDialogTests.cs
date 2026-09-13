using System.Collections.Generic;
using System.Text.RegularExpressions;
using EasyMoney.App.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace EasyMoney.Tests
{
    /// <summary>
    /// 选择月份弹窗的结构与交互测试。
    ///
    /// 弹窗是纯代码搭的，节点名就是它对外可见的形状（Row0/Month1、YearRow/Year…），
    /// 所以这里按名字取节点断言——改了命名或网格分组，这些用例就该红。
    ///
    /// ⚠️ 测不了「选完 / 取消后弹窗消失」：关闭走的是 Object.Destroy，而它在编辑模式下
    /// 是**非法**的——直接打一条 Error 并且什么事都不做（不是延迟到帧末）。所以点完
    /// 关闭按钮，overlay 其实还在原地。销毁只能靠 Play 验，这里只断言回调。
    /// </summary>
    public class MonthPickerDialogTests
    {
        private GameObject m_Root;
        private RectTransform m_Parent;

        [SetUp]
        public void SetUp()
        {
            // 要断言按钮配色，先给一份确定的配色，免得依赖 theme.json 的载入时机
            Theme.Apply(ThemePalette.Light());

            m_Root = new GameObject("TestRoot", typeof(RectTransform));
            m_Parent = m_Root.GetComponent<RectTransform>();
            m_Parent.sizeDelta = new Vector2(Theme.REF_WIDTH, Theme.REF_HEIGHT);
        }

        [TearDown]
        public void TearDown()
        {
            // EditMode 下 GameObject 不会随场景销毁，必须手动清
            Object.DestroyImmediate(m_Root);
        }

        // ── 结构 ────────────────────────────────────

        [Test]
        public void Show_BuildsTwelveMonthButtons()
        {
            MonthPickerDialog.Show(m_Parent, 2026, 9, null);

            for (int iMonth = 1; iMonth <= 12; iMonth++)
            {
                Assert.AreEqual($"{iMonth}月", _monthButton(iMonth).GetComponentInChildren<Text>().text,
                    $"{iMonth} 月的格子文案不对——月份不补零，要与顶部月份条同一种写法");
            }

            Assert.IsNull(m_Parent.Find("MonthPickerOverlay/Panel/Row4"),
                "月份网格只该有 4 行（3 列 × 4 行 = 12 个月），多出来的行说明行列算错了");
        }

        [Test]
        public void Show_StartsAtGivenYear()
        {
            MonthPickerDialog.Show(m_Parent, 2026, 9, null);

            Assert.AreEqual("2026年", _yearLabel().text,
                "弹窗该停在打开它的那一刻正在查看的年份");
        }

        [Test]
        public void Show_HighlightsGivenMonthOnly()
        {
            MonthPickerDialog.Show(m_Parent, 2026, 9, null);

            for (int iMonth = 1; iMonth <= 12; iMonth++)
            {
                Assert.AreEqual(iMonth == 9, _isHighlighted(iMonth),
                    $"{iMonth} 月的高亮状态不对——弹窗打开时只该高亮正在查看的 9 月");
            }
        }

        // ── 选月份 ──────────────────────────────────

        [Test]
        public void PickMonth_InvokesCallbackWithDisplayedYear()
        {
            List<(int Year, int Month)> lPicked = _showAndRecord(2026, 9);

            _expectEditModeDestroy();
            _monthButton(3).onClick.Invoke();

            Assert.AreEqual(1, lPicked.Count, "点一个月份只该回调一次");
            Assert.AreEqual((2026, 3), lPicked[0]);
        }

        [Test]
        public void PickMonth_AfterSwitchingYear_UsesTheDisplayedYear()
        {
            List<(int Year, int Month)> lPicked = _showAndRecord(2026, 9);

            _yearButton("PrevYear").onClick.Invoke();

            _expectEditModeDestroy();
            _monthButton(3).onClick.Invoke();

            Assert.AreEqual((2025, 3), lPicked[0],
                "切到 2025 年之后再选月份，回传的该是 2025 年——不是打开弹窗时的 2026");
        }

        // ── 切年份 ──────────────────────────────────

        [Test]
        public void SwitchYear_UpdatesYearLabel()
        {
            MonthPickerDialog.Show(m_Parent, 2026, 9, null);

            _yearButton("PrevYear").onClick.Invoke();
            Assert.AreEqual("2025年", _yearLabel().text);

            _yearButton("NextYear").onClick.Invoke();
            _yearButton("NextYear").onClick.Invoke();
            Assert.AreEqual("2027年", _yearLabel().text, "从 2025 年连点两次下一年该到 2027");
        }

        [Test]
        public void SwitchYear_ClearsHighlight()
        {
            MonthPickerDialog.Show(m_Parent, 2026, 9, null);

            _yearButton("PrevYear").onClick.Invoke();

            for (int iMonth = 1; iMonth <= 12; iMonth++)
            {
                Assert.IsFalse(_isHighlighted(iMonth),
                    $"切到 2025 年后没有任何月份该高亮（选中的是 2026 年 9 月），但 {iMonth} 月亮着。" +
                    "高亮判定要同时看年份和月份，只看月份的话每一年的 9 月都会亮");
            }
        }

        [Test]
        public void SwitchYear_Back_KeepsHighlight()
        {
            MonthPickerDialog.Show(m_Parent, 2026, 9, null);

            _yearButton("PrevYear").onClick.Invoke();
            _yearButton("NextYear").onClick.Invoke();

            Assert.IsTrue(_isHighlighted(9), "切回 2026 年，9 月该重新亮起来");
        }

        // ── 取消 ────────────────────────────────────

        [Test]
        public void Cancel_DoesNotInvokeCallback()
        {
            List<(int Year, int Month)> lPicked = _showAndRecord(2026, 9);

            _expectEditModeDestroy();
            m_Parent.Find("MonthPickerOverlay/Panel/Cancel")
                .GetComponent<Button>().onClick.Invoke();

            Assert.AreEqual(0, lPicked.Count, "点取消不该回传任何年月");
        }

        // ── 辅助 ────────────────────────────────────

        /// <summary>
        /// 声明「接下来会打一条 Destroy 报错」。
        ///
        /// 选完或取消时弹窗走 Object.Destroy，而编辑模式下 Destroy 是**非法**的：
        /// 它会直接打一条 Error 并且什么都不做（不是延迟到帧末）。测试框架把没有
        /// 声明过的 Error 日志判为失败，所以「会关弹窗」的那几下要显式声明一下。
        /// 真机跑在 Play 模式，不会产生这条日志。
        /// </summary>
        private static void _expectEditModeDestroy()
        {
            LogAssert.Expect(LogType.Error, new Regex("Destroy may not be called from edit mode"));
        }

        private List<(int Year, int Month)> _showAndRecord(int iYear, int iMonth)
        {
            List<(int Year, int Month)> lPicked = new List<(int, int)>();
            MonthPickerDialog.Show(m_Parent, iYear, iMonth, (y, m) => lPicked.Add((y, m)));
            return lPicked;
        }

        private Button _monthButton(int iMonth)
        {
            // 3 列一行：1~3 月在 Row0，4~6 月在 Row1，以此类推
            Transform oNode = m_Parent.Find(
                $"MonthPickerOverlay/Panel/Row{(iMonth - 1) / 3}/Month{iMonth}");

            Assert.IsNotNull(oNode, $"找不到 {iMonth} 月的格子——网格的行列分组变了？");
            return oNode.GetComponent<Button>();
        }

        private Button _yearButton(string sName)
        {
            Transform oNode = m_Parent.Find($"MonthPickerOverlay/Panel/YearRow/{sName}");
            Assert.IsNotNull(oNode, $"年份行里找不到 {sName}");
            return oNode.GetComponent<Button>();
        }

        private Text _yearLabel()
        {
            Transform oNode = m_Parent.Find("MonthPickerOverlay/Panel/YearRow/Year");
            Assert.IsNotNull(oNode, "找不到年份文字");
            return oNode.GetComponent<Text>();
        }

        private bool _isHighlighted(int iMonth)
        {
            return ((Image)_monthButton(iMonth).targetGraphic).color == Theme.PRIMARY;
        }
    }
}
