using EasyMoney.Core;
using NUnit.Framework;

namespace EasyMoney.Tests
{
    /// <summary>
    /// 安全区换算测试。
    ///
    /// 这一块要盯的是一件很容易搞反的事：**底部到底该让出多少**。
    /// 同一个 Screen.safeArea，在 Android 15 上铺满全屏（导航栏是浮层，没被排除），
    /// 在 Android 13/14 上却已经把导航栏排除掉了（safeArea.y ≈ 导航栏高度）。
    /// 规则写成「无条件减掉导航栏高度」的话，Android 13/14 上会凭空多出一条与导航栏
    /// 等高的空白；写成「不管它」的话，Android 15 上内容被导航栏盖住。
    /// **两种都不报错、不崩溃，界面上看上去都「挺正常」。**
    ///
    /// 所以断言围绕**幂等**写：把算出来的结果再喂回去，不该再缩一次。
    ///
    /// 另一类是「算歪」——宽度为负、屏幕尺寸为 0、安全区跑到屏幕外面。锚点越界
    /// 不会报错，只会把节点画到看不见的地方去。
    /// </summary>
    public class SafeAreaLayoutTests
    {
        private const float TOLERANCE = 0.0001f;

        /// <summary>一台 1080×2280 的手机，三键导航栏占 132px。</summary>
        private const float SCREEN_WIDTH = 1080f;
        private const float SCREEN_HEIGHT = 2280f;
        private const float NAV_BAR = 132f;

        // ── 常规换算 ────────────────────────────────

        [Test]
        public void Compute_TypicalSafeArea_MapsProportionally()
        {
            // 最普通的一条：没有导航栏要补，纯粹是像素 → 归一化。
            // 顶边留出状态栏，所以 MaxY 小于 1
            SafeAreaAnchors oAnchors = SafeAreaLayout.Compute(
                new SafeAreaRect(0f, 60f, SCREEN_WIDTH, 2200f), SCREEN_WIDTH, SCREEN_HEIGHT, 0f);

            Assert.AreEqual(0f, oAnchors.MinX);
            Assert.AreEqual(60f / SCREEN_HEIGHT, oAnchors.MinY, TOLERANCE);
            Assert.AreEqual(1f, oAnchors.MaxX);
            Assert.AreEqual(2260f / SCREEN_HEIGHT, oAnchors.MaxY, TOLERANCE,
                "顶边的内缩要留着，刘海那条不能被抹平");
        }

        // ── 导航栏：让多少 ──────────────────────────

        [Test]
        public void Compute_EdgeToEdgeSafeArea_StepsBackByNavBarHeight()
        {
            // Android 15 强制 edge-to-edge：safeArea 铺满全屏，导航栏浮在内容上。
            // 这正是真机上底部被盖住的那台
            SafeAreaAnchors oAnchors = SafeAreaLayout.Compute(
                new SafeAreaRect(0f, 0f, SCREEN_WIDTH, SCREEN_HEIGHT),
                SCREEN_WIDTH, SCREEN_HEIGHT, NAV_BAR);

            Assert.AreEqual(NAV_BAR / SCREEN_HEIGHT, oAnchors.MinY, TOLERANCE,
                "要把导航栏那条让出来");
            Assert.AreEqual(1f, oAnchors.MaxY, TOLERANCE, "顶边不受影响");
        }

        [Test]
        public void Compute_SafeAreaAlreadyExcludesNavBar_AddsNothing()
        {
            // Android 13/14：safeArea.y 本来就等于导航栏高度，再让一次就是白边。
            // 这条与上一条的期望值**恰好相同**，正是幂等的意思
            SafeAreaAnchors oAnchors = SafeAreaLayout.Compute(
                new SafeAreaRect(0f, NAV_BAR, SCREEN_WIDTH, SCREEN_HEIGHT - NAV_BAR),
                SCREEN_WIDTH, SCREEN_HEIGHT, NAV_BAR);

            Assert.AreEqual(NAV_BAR / SCREEN_HEIGHT, oAnchors.MinY, TOLERANCE,
                "safeArea 已经排除导航栏了，不该再让一条");
        }

        [Test]
        public void Compute_ApplyingResultTwice_DoesNotShrinkAgain()
        {
            // 幂等，换个说法再钉一遍：把第一次算出来的底边当作新的 safeArea 喂回去，
            // 结果必须原地不动。写成「无条件减 fNavigationBarPx」这条会红
            SafeAreaAnchors oFirst = SafeAreaLayout.Compute(
                new SafeAreaRect(0f, 0f, SCREEN_WIDTH, SCREEN_HEIGHT),
                SCREEN_WIDTH, SCREEN_HEIGHT, NAV_BAR);

            float fBottomPx = oFirst.MinY * SCREEN_HEIGHT;

            SafeAreaAnchors oSecond = SafeAreaLayout.Compute(
                new SafeAreaRect(0f, fBottomPx, SCREEN_WIDTH, SCREEN_HEIGHT - fBottomPx),
                SCREEN_WIDTH, SCREEN_HEIGHT, NAV_BAR);

            Assert.AreEqual(oFirst.MinY, oSecond.MinY, TOLERANCE,
                "已经让过一次了，第二次不该再缩");
        }

        [Test]
        public void Compute_GestureBarShorterThanAreaInset_AddsNothing()
        {
            // 手势导航的细条比 safeArea 已经让出的还矮，差额是负的。
            // 不钳的话底边会被推回屏幕外
            SafeAreaAnchors oAnchors = SafeAreaLayout.Compute(
                new SafeAreaRect(0f, NAV_BAR, SCREEN_WIDTH, SCREEN_HEIGHT - NAV_BAR),
                SCREEN_WIDTH, SCREEN_HEIGHT, 48f);

            Assert.AreEqual(NAV_BAR / SCREEN_HEIGHT, oAnchors.MinY, TOLERANCE,
                "差额为负时不能倒着缩");
        }

        [Test]
        public void Compute_NegativeNavBarHeight_TreatedAsZero()
        {
            SafeAreaAnchors oAnchors = SafeAreaLayout.Compute(
                new SafeAreaRect(0f, 0f, SCREEN_WIDTH, SCREEN_HEIGHT),
                SCREEN_WIDTH, SCREEN_HEIGHT, -50f);

            Assert.AreEqual(0f, oAnchors.MinY, TOLERANCE);
        }

        // ── 让出去就没高度了 ────────────────────────

        [Test]
        public void Compute_NavBarWouldEatWholeArea_FallsBackToNoInset()
        {
            // 让出去高度就没了的话，宁可露一点也别让：安全区高度成 0 会让标题栏、
            // 标签栏、内容区（它按上下两个高度算，还会算出负数）全塌下来，
            // 整屏一片空白，而且不报错不崩
            const float TINY_HEIGHT = 100f;

            // 前提钉住：这点高度确实不够让
            Assert.Less(TINY_HEIGHT, NAV_BAR, "前提：这块安全区比导航栏还矮");

            SafeAreaAnchors oAnchors = SafeAreaLayout.Compute(
                new SafeAreaRect(0f, 0f, SCREEN_WIDTH, TINY_HEIGHT),
                SCREEN_WIDTH, SCREEN_HEIGHT, NAV_BAR);

            Assert.AreEqual(0f, oAnchors.MinY, TOLERANCE, "高度不够让，退回不内缩");
            Assert.AreEqual(TINY_HEIGHT / SCREEN_HEIGHT, oAnchors.MaxY, TOLERANCE,
                "高度也不能被压没");
        }

        [Test]
        public void Compute_NavBarExactlyEatsArea_FallsBackToNoInset()
        {
            // 边界：正好吃光也算吃光，压成 0 高度就是整屏空白
            SafeAreaAnchors oAnchors = SafeAreaLayout.Compute(
                new SafeAreaRect(0f, 0f, SCREEN_WIDTH, NAV_BAR),
                SCREEN_WIDTH, SCREEN_HEIGHT, NAV_BAR);

            Assert.AreEqual(0f, oAnchors.MinY, TOLERANCE);
        }

        // ── 坏数据 ──────────────────────────────────

        [Test]
        public void Compute_ZeroScreenSize_FillsWholeScreen()
        {
            // 屏幕尺寸还没准备好时除零会得到 NaN，锚点跟着全废。
            // 退回全屏至少是个能看的界面
            SafeAreaAnchors oAnchors = SafeAreaLayout.Compute(
                new SafeAreaRect(0f, 0f, 0f, 0f), 0f, 0f, NAV_BAR);

            Assert.AreEqual(0f, oAnchors.MinX);
            Assert.AreEqual(0f, oAnchors.MinY);
            Assert.AreEqual(1f, oAnchors.MaxX);
            Assert.AreEqual(1f, oAnchors.MaxY);
        }

        [Test]
        public void Compute_AreaOutsideScreen_ClampedToUnitRange()
        {
            // 锚点越界不会报错，只会画到看不见的地方去
            SafeAreaAnchors oAnchors = SafeAreaLayout.Compute(
                new SafeAreaRect(-100f, -100f, 2000f, 4000f), SCREEN_WIDTH, SCREEN_HEIGHT, 0f);

            Assert.AreEqual(0f, oAnchors.MinX);
            Assert.AreEqual(0f, oAnchors.MinY);
            Assert.AreEqual(1f, oAnchors.MaxX);
            Assert.AreEqual(1f, oAnchors.MaxY);
        }

        [Test]
        public void Compute_NegativeWidth_NeverInvertsAnchors()
        {
            // 宽度为负会把两个锚点倒过来，节点整个镜像——比算歪更难认
            SafeAreaAnchors oAnchors = SafeAreaLayout.Compute(
                new SafeAreaRect(500f, 500f, -300f, -300f), SCREEN_WIDTH, SCREEN_HEIGHT, 0f);

            Assert.LessOrEqual(oAnchors.MinX, oAnchors.MaxX, "锚点倒置会让节点镜像");
            Assert.LessOrEqual(oAnchors.MinY, oAnchors.MaxY);
        }
    }
}
