using UnityEngine;

namespace EasyMoney.App.UI
{
    /// <summary>
    /// 读 Android 系统栏的高度（屏幕像素）。
    ///
    /// 为什么需要它：**Unity 2022.3 的 <c>Screen.safeArea</c> 不包含导航栏。**
    /// targetSdk 35 的应用在 Android 15 上会被系统强制 edge-to-edge（窗口铺满整屏、
    /// 导航栏变成浮层盖在内容上），而 safeArea 仍然报告全屏，底部内容就被挡住了。
    /// 这是 Unity 的已知缺陷 UUM-121413，只在 6.1 修复，因为是 breaking change
    /// **没有回移 2022**，所以只能自己从 WindowInsets 里读。
    ///
    /// 注意 <see cref="SafeAreaLayout"/> 那边是**按差额**补的，不是无条件减掉这里
    /// 返回的值——Android 13/14 的 safeArea 本来就排除了导航栏，无条件减会多出一条白边。
    ///
    /// ⚠️ <b>返回的是像素不是 dp</b>，与 <c>Screen.safeArea</c> 同一个坐标系，
    /// 不需要拿 <c>DisplayMetrics.density</c> 换算。px 与 dp 混用是这块最经典的错。
    ///
    /// ⚠️ <b>必须在主线程调用。</b> Unity 的 Awake/Update 就在 Android UI 线程上，
    /// 正常写不会出问题——这条是防着以后有人把它挪进 Task.Run。
    ///
    /// ⚠️ <b>不要改成每帧调用。</b> 每读一次会新建若干个 AndroidJavaObject，每个占一个
    /// JNI local ref，而本地引用表只有 512 项、主线程的 JNI frame 贯穿整个进程不会自动
    /// 回收。偶尔读几次无所谓，每帧读就是几秒后 local reference table overflow 直接崩，
    /// 而且崩得毫无线索。调用方（SafeAreaFitter）只在尺寸变化和启动头两秒里读。
    ///
    /// ⚠️ 别为了「解耦」把它改成靠字符串反射调用——那才会真被 IL2CPP 的代码剥离干掉。
    /// 下面这些泛型实例化都字面写在代码里，编译期可见，剥不掉；JNI 那边的
    /// FindClass/GetMethodID 又发生在 Java VM 里，剥离器根本不参与。
    /// </summary>
    public static class AndroidSystemBars
    {
        /// <summary>
        /// 导航栏高度（屏幕像素）。非 Android、读不到、或者这台设备本来就没有导航栏时
        /// 返回 0——调用方按「不用让」处理。
        /// </summary>
        public static int NavigationBarHeightPx()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return _readNavigationBarHeightPx();
#else
            // 编辑器里安全区等于全屏、也没有导航栏，一律当作不用让
            return 0;
#endif
        }

        /// <summary>
        /// ⚠️ <b>临时，仅供本次真机验收的诊断读数用，验完连同 AppRoot 里那段读数一起删。</b>
        ///
        /// 兜底取法（老 API）读到的值，与
        /// <see cref="NewApiNavigationBarHeightPxForDiagnostics"/> 交叉对照用。
        /// 真机上两个值要是都对不上、或者一个有一个没有，就能一眼看出走的是哪条路——
        /// 只显示最终值的话，「正路返回 0 所以回退了」和「这台设备本来就没导航栏」
        /// 长得一模一样。非 Android 返回 -1，与「读到了 0」区分开。
        ///
        /// 外层不带 <c>#if</c> 是为了让调用方（SafeAreaDiagnostics）不必也包一层——
        /// 那个组件的 Update 在编辑器里照样要编译。
        /// </summary>
        public static int LegacyNavigationBarHeightPxForDiagnostics()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return _probeInsets(false);
#else
            return -1;
#endif
        }

        /// <summary>
        /// ⚠️ 临时，同上一段的用途：正路（API 30+ 的 <c>getInsets</c>）能读到多少。
        /// 非 Android 返回 -1。
        ///
        /// ⚠️ <b>这两个诊断方法都必须留在这个 <c>#if</c> 块外面</b>——调用方
        /// SafeAreaDiagnostics 的 Update 在编辑器里照样要编译，方法在块内的话
        /// 编辑器下直接 CS0117。这个错已经犯过一次了。
        /// </summary>
        public static int NewApiNavigationBarHeightPxForDiagnostics()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return _probeInsets(true);
#else
            return -1;
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        /// <summary>SDK_INT 是常量，读一次记下来，省掉每次调用新建一个 AndroidJavaClass。</summary>
        private static int s_SdkInt = -1;

        private static int _readNavigationBarHeightPx()
        {
            try
            {
                using AndroidJavaClass oUnityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using AndroidJavaObject oActivity = oUnityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

                if (oActivity == null)
                {
                    return 0;
                }

                using AndroidJavaObject oWindow = oActivity.Call<AndroidJavaObject>("getWindow");
                using AndroidJavaObject oDecorView = oWindow.Call<AndroidJavaObject>("getDecorView");
                using AndroidJavaObject oInsets = oDecorView.Call<AndroidJavaObject>("getRootWindowInsets");

                if (oInsets == null)
                {
                    // 视图还没 attach 到窗口，或者 inset 还没分发下来。
                    // 返回 0 会让「不用让」和「还没读到」长得一样，所以调用方会重试
                    return 0;
                }

                return _navigationBarBottom(oInsets);
            }
            catch (System.Exception oError)
            {
                // 不能静默返回 0：那样日志里「抛了异常」和「这台设备没导航栏」长得一模一样，
                // 真机上「修了跟没修一样」就无从排查了
                Debug.LogWarning($"[AndroidSystemBars] 读导航栏高度失败，按 0 处理：{oError}");
                return 0;
            }
        }

        /// <summary>
        /// 两个来源各读一遍，挑哪一个的规则在
        /// <see cref="SafeAreaLayout.ResolveNavigationBarHeight"/> 里（可测的纯函数）。
        /// </summary>
        private static int _navigationBarBottom(AndroidJavaObject oInsets)
        {
            int iNewApi = _sdkInt() >= 30 ? _insetsBottom(oInsets) : 0;
            int iLegacy = _legacyInsetBottom(oInsets);

            return SafeAreaLayout.ResolveNavigationBarHeight(iNewApi, iLegacy);
        }

        /// <summary>
        /// API 30+ 的正路：按 Type 取导航栏的 inset。
        ///
        /// ⚠️ **自带 try/catch，失败只吞掉自己。** 这条路径在真机上实测会返回 0
        /// （见 <see cref="SafeAreaLayout.ResolveNavigationBarHeight"/> 的注释），
        /// 要是让异常冒到外层那个 catch，后面的 legacy 兜底会被一起带走，
        /// 于是又变成「修了跟没修一样」。
        /// </summary>
        private static int _insetsBottom(AndroidJavaObject oInsets)
        {
            try
            {
                // ⚠️ WindowInsets$Type 在 API 30 以下不存在，FindClass 会直接抛，
                // 所以这个类只能在分支内部构造，不能提到 if 外面
                using AndroidJavaClass oType = new AndroidJavaClass("android.view.WindowInsets$Type");
                int iNavigationBars = oType.CallStatic<int>("navigationBars");

                using AndroidJavaObject oBarInsets =
                    oInsets.Call<AndroidJavaObject>("getInsets", iNavigationBars);

                if (oBarInsets == null)
                {
                    return 0;
                }

                // android.graphics.Insets.bottom 是 public final int **字段**，
                // 没有 getBottom() 方法，用 Get 不是 Call
                return oBarInsets.Get<int>("bottom");
            }
            catch (System.Exception oError)
            {
                Debug.LogWarning($"[AndroidSystemBars] getInsets 取导航栏高度失败，改用兜底取法：{oError}");
                return 0;
            }
        }

        /// <summary>
        /// 兜底：API 20 就有、一直被标记废弃但到 API 36 都没移除的零参数方法。
        /// 真机上它反而比正路可靠，理由见
        /// <see cref="SafeAreaLayout.ResolveNavigationBarHeight"/> 的注释。
        /// </summary>
        private static int _legacyInsetBottom(AndroidJavaObject oInsets)
        {
            return oInsets.Call<int>("getSystemWindowInsetBottom");
        }

        private static int _sdkInt()
        {
            if (s_SdkInt < 0)
            {
                using AndroidJavaClass oVersion = new AndroidJavaClass("android.os.Build$VERSION");
                s_SdkInt = oVersion.GetStatic<int>("SDK_INT");
            }

            return s_SdkInt;
        }

        private static int _probeInsets(bool bNewApi)
        {
            try
            {
                using AndroidJavaClass oUnityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using AndroidJavaObject oActivity = oUnityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

                if (oActivity == null)
                {
                    return -1;
                }

                using AndroidJavaObject oWindow = oActivity.Call<AndroidJavaObject>("getWindow");
                using AndroidJavaObject oDecorView = oWindow.Call<AndroidJavaObject>("getDecorView");
                using AndroidJavaObject oInsets = oDecorView.Call<AndroidJavaObject>("getRootWindowInsets");

                if (oInsets == null)
                {
                    return -1;
                }

                // 调用方（诊断读数）按刷新间隔限流，不是每帧都走这里
                return bNewApi && _sdkInt() >= 30 ? _insetsBottom(oInsets) : _legacyInsetBottom(oInsets);
            }
            catch (System.Exception oError)
            {
                Debug.LogWarning($"[AndroidSystemBars] 诊断读数失败：{oError}");
                return -1;
            }
        }
#endif
    }
}
