using EasyMoney.Core;
using UnityEngine;

namespace EasyMoney.App.UI
{
    /// <summary>
    /// 安全区适配。把自身 RectTransform 内缩到 Screen.safeArea，
    /// 避开刘海、状态栏与底部导航栏。编辑器里 safeArea 等于全屏、导航栏高度恒 0，
    /// 所以不会产生偏移。
    ///
    /// 「底部要不要再让开导航栏」那条规则在 <see cref="SafeAreaLayout"/> 里——
    /// Unity 2022.3 的 Screen.safeArea 不包含导航栏（UUM-121413），得另外去
    /// WindowInsets 里读，见 <see cref="AndroidSystemBars"/>。
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        /// <summary>
        /// 启动后多久之内反复重读导航栏高度。
        ///
        /// 启动初期 getRootWindowInsets() 的失败形态是「返回非 null 但各边都是 0」
        /// （视图 attach 了、inset 还没分发下来），而不是返回 null。这时候读到 0 会让
        /// 底部不让，**表现就是「修了跟没修一样」**，而且极难排查——所以才要重试。
        ///
        /// 按时间不按帧数：帧率是浮动的，10 帧在低端机上也许只有几十毫秒。
        /// </summary>
        private const float INSET_PROBE_SECONDS = 2f;

        /// <summary>两次重读之间隔多少帧。每读一次要走一串 JNI，别每帧都来。</summary>
        private const int INSET_PROBE_FRAME_GAP = 10;

        private RectTransform m_Rect;
        private Rect m_LastSafeArea = Rect.zero;
        private Vector2Int m_LastScreenSize = Vector2Int.zero;
        private int m_NavigationBarPx;
        private int m_FramesSinceProbe;
        private float m_StartTime;

        private void Awake()
        {
            m_Rect = GetComponent<RectTransform>();
            m_StartTime = Time.realtimeSinceStartup;
            _apply();
        }

        private void Update()
        {
            // 转屏或分屏会改变安全区，尺寸变了才重算，避免每帧无谓开销
            if (Screen.safeArea != m_LastSafeArea ||
                Screen.width != m_LastScreenSize.x ||
                Screen.height != m_LastScreenSize.y)
            {
                _apply();
                return;
            }

            // 导航栏高度还没读到，就在启动头两秒里再试几次。设备本来就没导航栏时
            // 会一直返回 0，试满时间窗就自然停下——不用另外记「失败过几次」
            m_FramesSinceProbe++;
            if (m_NavigationBarPx <= 0 &&
                m_FramesSinceProbe >= INSET_PROBE_FRAME_GAP &&
                Time.realtimeSinceStartup - m_StartTime < INSET_PROBE_SECONDS)
            {
                _apply();
            }
        }

        /// <summary>
        /// 切回前台时重读一次。
        ///
        /// 手势导航 ↔ 三键导航切换时导航栏会从细条变成 48dp，而在 Android 15 上
        /// Screen.safeArea 一直报全屏、Screen.width/height 也不变——Update 里
        /// 「尺寸变了」那个条件**永远不触发**，缓存的一直是切换前的旧高度。
        /// </summary>
        private void OnApplicationFocus(bool bHasFocus)
        {
            if (!bHasFocus)
            {
                return;
            }

            m_StartTime = Time.realtimeSinceStartup;
            _apply();
        }

        private void _apply()
        {
            if (m_Rect == null)
            {
                return;
            }

            Rect oSafeArea = Screen.safeArea;
            m_LastSafeArea = oSafeArea;
            m_LastScreenSize = new Vector2Int(Screen.width, Screen.height);
            m_FramesSinceProbe = 0;

            // 每次重算安全区都重申一遍「导航栏要看得见、别自动收走」。
            //
            // 为什么不能只调一次：Unity 的播放器会自己往窗口上设全屏沉浸标志
            // （SYSTEM_UI_FLAG_HIDE_NAVIGATION | IMMERSIVE_STICKY，libunity.so 里
            // 明摆着有这几个字符串），它设的时机在我们后面——启动时调一次会被它盖掉，
            // 现象是导航键**先露一下再慢慢消失、划一下才浮出来、一两秒又缩回去**。
            // 而 _apply 恰好在启动头两秒里反复跑（下面那段重试），
            // 又会在切回前台时跑一次，正好是播放器可能重新设标志的两个时间点。
            //
            // 代价是把那串 JNI 多走十几遍。_apply 本身就不是每帧调用的，撑得住
            AndroidSystemBars.EnsureNavigationBarUsable();

            // 读一次要走一串 JNI，所以只在真要重算的时候读，不放进 Update 每帧跑
            m_NavigationBarPx = AndroidSystemBars.NavigationBarHeightPx();

            SafeAreaAnchors oAnchors = SafeAreaLayout.Compute(
                new SafeAreaRect(oSafeArea.x, oSafeArea.y, oSafeArea.width, oSafeArea.height),
                Screen.width, Screen.height, m_NavigationBarPx);

            m_Rect.anchorMin = new Vector2(oAnchors.MinX, oAnchors.MinY);
            m_Rect.anchorMax = new Vector2(oAnchors.MaxX, oAnchors.MaxY);
            m_Rect.offsetMin = Vector2.zero;
            m_Rect.offsetMax = Vector2.zero;
        }
    }
}
