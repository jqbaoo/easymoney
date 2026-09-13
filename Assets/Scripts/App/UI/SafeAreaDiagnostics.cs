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
        private const float PANEL_HEIGHT = 130f;

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
#if UNITY_ANDROID && !UNITY_EDITOR
            RectTransform oRoot = UiFactory.CreateNode(oCanvas, "SafeAreaDiagnostics");

            // 贴在屏幕最顶端盖住标题栏——反正是临时的，看得清比好看要紧。
            // 挂 Canvas 下而不是 SafeArea 下，免得跟着安全区一起被挤动
            oRoot.anchorMin = new Vector2(0f, 1f);
            oRoot.anchorMax = new Vector2(1f, 1f);
            oRoot.pivot = new Vector2(0.5f, 1f);
            oRoot.anchoredPosition = Vector2.zero;
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
            int iNavBar = AndroidSystemBars.NavigationBarHeightPx();
            int iNewApi = AndroidSystemBars.NewApiNavigationBarHeightPxForDiagnostics();
            int iLegacy = AndroidSystemBars.LegacyNavigationBarHeightPxForDiagnostics();

            // 重算一遍「该让多少」，跟界面上实际的表现对照。
            // 故意调的是同一个纯函数，这样读数反映的是算出来的结果而不是拍的数
            SafeAreaAnchors oAnchors = SafeAreaLayout.Compute(
                new SafeAreaRect(oSafe.x, oSafe.y, oSafe.width, oSafe.height),
                Screen.width, Screen.height, iNavBar);

            float fExtra = oAnchors.MinY * Screen.height - oSafe.y;

            m_Label.text =
                $"safeArea y={oSafe.y:F0} h={oSafe.height:F0} w={oSafe.width:F0}\n" +
                $"screen {Screen.width}x{Screen.height}  " +
                $"nav {iNavBar} (new {iNewApi}/old {iLegacy})  extra {fExtra:F0}";
        }
    }
}
