using EasyMoney.Core;
using UnityEngine;
using UnityEngine.UI;

namespace EasyMoney.App.UI
{
    /// <summary>
    /// ⚠️ <b>临时：真机上「底部被导航栏遮挡」这一轮的诊断读数，验完连同调用点一起删掉。</b>
    ///
    /// 为什么值得占一次真机往返：如果 <c>getRootWindowInsets()</c> 在启动那个时机读不到值，
    /// 现象就是「**没修好**」，而无从区分是代码没生效还是这台设备本来就不需要补——
    /// 两者在界面上长得一模一样。这行字把 safeArea、屏幕尺寸、两个 JNI 取法各自的值
    /// 摆在一起，一次就能定位。
    ///
    /// 只在 Android 真机上显示：编辑器里这些值恒为 0，显示出来只会挡住标题栏，
    /// 干扰「Play 模式下界面没有回归」那条验收。
    /// </summary>
    public sealed class SafeAreaDiagnostics : MonoBehaviour
    {
        private const float PANEL_HEIGHT = 240f;

        /// <summary>
        /// 面板顶边离 Canvas 顶多远。
        ///
        /// ⚠️ **不能贴顶（0）**：状态栏是系统窗口，永远画在应用之上，贴顶的话读数第一行
        /// 正好被状态栏盖住——上一次真机验收就是这么丢掉 `safeArea` 那行的，
        /// 而它恰恰是判断「该不该补底部」缺不了的那个数。140 设计像素 × 缩放后
        /// 比 48dp 的状态栏高，挡不着。
        /// </summary>
        private const float PANEL_TOP_OFFSET = 140f;

        /// <summary>
        /// 读数多久刷一次。
        ///
        /// ⚠️ <b>不是为了省 CPU，是为了不把 JNI local ref 表撑爆。</b>
        /// 每读一次要新建五六个 AndroidJavaObject，各占一个 local ref，而本地引用表
        /// 只有 512 项。按帧读，几秒后就是 local reference table overflow 直接崩，
        /// 且崩得毫无线索。生产路径（SafeAreaFitter）本来就只在尺寸变化时才读，
        /// 别让这个临时组件把那条纪律破坏掉。
        /// </summary>
        private const float REFRESH_SECONDS = 0.5f;

        private Text m_Label;
        private float m_NextRefresh;

        public static void Attach(RectTransform oCanvas)
        {
#if UNITY_ANDROID
            // 编辑器里这些值恒为 0，面板显示出来只会挡住标题栏。
            // 用运行时判断而不是 #if UNITY_EDITOR，理由同 AndroidSystemBars 顶部那段注释
            if (Application.isEditor)
            {
                return;
            }

            RectTransform oRoot = UiFactory.CreateNode(oCanvas, "SafeAreaDiagnostics");

            // 挂在屏幕上方盖住标题栏那一带——反正是临时的，看得清比好看要紧。
            // 挂 Canvas 下而不是 SafeArea 下，免得跟着安全区一起被挤动
            oRoot.anchorMin = new Vector2(0f, 1f);
            oRoot.anchorMax = new Vector2(1f, 1f);
            oRoot.pivot = new Vector2(0.5f, 1f);
            oRoot.anchoredPosition = new Vector2(0f, -PANEL_TOP_OFFSET);
            oRoot.sizeDelta = new Vector2(0f, PANEL_HEIGHT);

            Image oBackground = UiFactory.CreatePanel(oRoot, "Bg", Theme.SCRIM);
            UiFactory.Stretch(oBackground.rectTransform);

            SafeAreaDiagnostics oSelf = oRoot.gameObject.AddComponent<SafeAreaDiagnostics>();
            oSelf.m_Label = UiFactory.CreateText(oRoot, "Text", "",
                Theme.FONT_TINY, TextAnchor.MiddleLeft, Theme.WHITE);
            UiFactory.Stretch(oSelf.m_Label.rectTransform);

            // 压在所有页面之上
            oRoot.SetAsLastSibling();
#endif
        }

        private void Update()
        {
            if (m_Label == null || Time.realtimeSinceStartup < m_NextRefresh)
            {
                return;
            }

            m_NextRefresh = Time.realtimeSinceStartup + REFRESH_SECONDS;

            Rect oSafe = Screen.safeArea;
            int iNewApi = AndroidSystemBars.NewApiNavigationBarHeightPxForDiagnostics();
            int iLegacy = AndroidSystemBars.LegacyNavigationBarHeightPxForDiagnostics();
            int iVisible = AndroidSystemBars.NavigationBarVisibleForDiagnostics();
            int iTappable = AndroidSystemBars.TappableElementBottomPxForDiagnostics();
            int iInset = AndroidSystemBars.BottomInsetPx();

            // nav 与 inset 是**两个数**，别混着看：nav 是「导航栏报多高」，
            // inset 是「最终决定让多少」。三键 + 窗口铺满时两者相等，
            // 而只要窗口自己让开了底部（gap 够一条），nav 还报 124、inset 已经归 0
            int iNavBar = SafeAreaLayout.ResolveNavigationBarHeight(iNewApi, iLegacy);

            // gap：渲染面底边到屏幕底边还剩多少。**这个数才是「要不要补底部」的关键**——
            // 它够一个导航栏高就说明窗口自己已经让开了，再补就是白边（真机上就是
            // 「内容整体偏高、标签栏下面空一块」）。两个来源都打出来，
            // 万一某个机型上有一个不灵，一眼就能看出是哪个
            int iSystemHeight = Display.main.systemHeight;
            int iResolutionHeight = Screen.currentResolution.height;
            int iGap = iSystemHeight > Screen.height ? iSystemHeight - Screen.height : 0;

            // 重算一遍「该让多少」，跟界面上实际的表现对照。
            // 故意调的是同一个纯函数，这样读数反映的是算出来的结果而不是拍的数
            SafeAreaAnchors oAnchors = SafeAreaLayout.Compute(
                new SafeAreaRect(oSafe.x, oSafe.y, oSafe.width, oSafe.height),
                Screen.width, Screen.height, iInset);

            float fExtra = oAnchors.MinY * Screen.height - oSafe.y;

            // vis：系统认为导航栏可不可见（1 可见 / 0 不可见 / -1 读不到）。
            // 光看 nav 分不出「系统藏了」和「画了但看不见」，这两个数要一起看
            //
            // tap：系统栏里可以点的那部分有多高，0 = 手势导航、非 0 = 三键、-1 = 读不到。
            // nav 是「导航栏报多高」，tap 是「系统栏实际占多高」，两者都可能撒谎——
            // 某些机型手势导航下 nav 照样报三键的 124，靠 tap 才分得出来
            m_Label.text =
                $"safeArea y={oSafe.y:F0} h={oSafe.height:F0} w={oSafe.width:F0}\n" +
                $"screen {Screen.width}x{Screen.height}  " +
                $"sys {iSystemHeight}  res {iResolutionHeight}  gap {iGap}\n" +
                $"nav {iNavBar} (new {iNewApi}/old {iLegacy})  vis {iVisible}  tap {iTappable}\n" +
                $"inset {iInset}  extra {fExtra:F0}";
        }
    }
}
