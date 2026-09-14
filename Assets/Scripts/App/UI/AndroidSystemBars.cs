using EasyMoney.Core;
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
        /// **底部要预留多少**（屏幕像素）。下面这些情况都返回 0，调用方一律按「不用让」处理：
        /// 非 Android、读不到、这台设备本来就没有导航栏、**手势导航**（导航栏不占版面）、
        /// 以及**窗口自己已经让开了底部**（不再请求全屏之后就是这样）。
        ///
        /// ⚠️ 名字是「预留多少」不是「导航栏多高」——这两个值在三键导航 + 窗口铺满时
        /// 才相等。分辨规则全在 <see cref="SafeAreaLayout.ResolveBottomInset"/> 里
        /// （可测的纯函数），方法本身只负责把几个数读回来。
        /// </summary>
        public static int BottomInsetPx()
        {
#if UNITY_ANDROID
            // 编辑器里安全区等于全屏、也没有导航栏，一律当作不用让
            if (Application.isEditor)
            {
                return 0;
            }

            return _readBottomInsetPx();
#else
            return 0;
#endif
        }

        /// <summary>
        /// 把**两条系统栏**调成与界面一致的外观，并要求系统别把导航栏收走。
        /// 每次重算安全区都调一遍（调用点在 <c>SafeAreaFitter._apply()</c>）。
        ///
        /// 四件事，顺序不能换：
        ///
        /// ① **先取得「系统栏底色由这个窗口负责画」的资格**——见下面那段 ⚠️。
        /// ② 两条栏的底色都设成页面底色。系统默认给的是**纯黑**，而本项目的底是燕麦米白，
        ///    两条黑边夹着界面很扎眼（微信那边是底色连着界面）。
        /// ③ 底色浅，图标就得是深的。图标颜色由系统按 <c>windowLightNavigationBar</c> 定，
        ///    而 Unity 生成的 <c>BaseUnityTheme</c> 在 API 31+ 继承的是
        ///    <c>android:Theme.Holo.Light.NoActionBar.Fullscreen</c>——Holo 是 API 27 之前的
        ///    东西，**压根没有这个属性**，取默认值 <c>false</c> = 画**白色**图标。
        ///    真机上第三轮的现象正是这个：底部空出 124px 什么都没有，而读数 <c>nav 124</c>
        ///    明明说导航栏占着地方——**它不是被藏起来了，是看不见**。
        /// ④ 明确要求系统**显示**导航栏、且**不要自动隐藏**——底部那 124px 是留出来了的，
        ///    收走就成了空白。
        ///
        /// ⚠️ <b>少了第 ① 步，第 ② 步会被静默忽略。</b>
        /// <c>Window.setStatusBarColor</c> / <c>setNavigationBarColor</c> 的文档写得很明白：
        /// **只有窗口带 <c>FLAG_DRAWS_SYSTEM_BAR_BACKGROUNDS</c> 时这两个调用才生效**。
        /// 而 Unity 那套 Holo 主题没有开这一位——第四轮真机上两条栏都是纯黑，
        /// 就是这么来的：代码跑了，颜色被系统丢掉了，**不报错、不崩溃**。
        ///
        /// ⚠️ <b>只在 API 30+ 动手。</b> Android 11 以下不强制 edge-to-edge，两条栏都是
        /// **不透明的黑条**，白图标配黑底本来就清楚——在那里设「浅色系统栏」会把图标
        /// 变成黑图标画黑底，比不设更糟。
        /// </summary>
        public static void EnsureSystemBarsUsable()
        {
#if UNITY_ANDROID
            if (Application.isEditor)
            {
                return;
            }

            _ensureSystemBarsUsable();
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
#if UNITY_ANDROID
            if (Application.isEditor)
            {
                return -1;
            }

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
#if UNITY_ANDROID
            if (Application.isEditor)
            {
                return -1;
            }

            return _probeInsets(true);
#else
            return -1;
#endif
        }

        /// <summary>
        /// ⚠️ 临时，诊断用：系统现在**认为**导航栏可见吗。1 = 可见，0 = 不可见，
        /// -1 = 非 Android / 读失败。
        ///
        /// 为什么要单独报这个：`nav 124` 只说明「导航栏占着 124px」，说不出它**有没有
        /// 被画出来**。「系统把它藏了」和「画了但图标看不见」在截图里一模一样，
        /// 只能靠这个数区分——而这两种情况要改的代码完全不同。
        /// </summary>
        public static int NavigationBarVisibleForDiagnostics()
        {
#if UNITY_ANDROID
            if (Application.isEditor)
            {
                return -1;
            }

            return _probeNavigationBarVisible();
#else
            return -1;
#endif
        }

        /// <summary>
        /// ⚠️ 临时，诊断用：系统栏的**窗口标志**与两条栏**读回来的底色**，一行文本。
        /// 非 Android 返回「非 Android」。
        ///
        /// 为什么要单独报这个：第五轮真机上两条栏都是纯黑，而代码里明明调了
        /// `setStatusBarColor` / `setNavigationBarColor`。「调用跑了但被系统丢掉」和
        /// 「压根没跑到」在截图上一模一样，只有读回来才知道——颜色读回来是页面底色
        /// 而屏幕上仍是黑的，就坐实了是系统丢的，下一步只能去改主题（`windowDrawsSystemBarBackgrounds`），
        /// 而不是继续在这几个 API 上打转。
        ///
        /// `flags` 打十六进制是有用的：`80000000` = `FLAG_DRAWS_SYSTEM_BAR_BACKGROUNDS`
        /// 有没有设上，`04000000` / `08000000` = 半透明状态栏/导航栏有没有被谁设上
        /// （设了就说明系统会自算一层蒙版，颜色同样不生效）。
        /// </summary>
        public static string SystemBarAppearanceForDiagnostics()
        {
#if UNITY_ANDROID
            if (Application.isEditor)
            {
                return "非 Android";
            }

            return _probeSystemBarAppearance();
#else
            return "非 Android";
#endif
        }

        /// <summary>
        /// ⚠️ 临时，诊断用：系统栏里可以点的那部分有多高。0 = 手势导航，非 0 = 三键。
        /// 非 Android / 读失败返回 -1。
        ///
        /// 为什么要单独报这个：底部该不该让出导航栏那 124px 全看它，
        /// 而它读不到时程序会按「有导航键」兜底——界面上「兜底生效了」和
        /// 「真的是三键导航」长得一模一样，只能靠这个数分开。
        /// </summary>
        public static int TappableElementBottomPxForDiagnostics()
        {
#if UNITY_ANDROID
            if (Application.isEditor)
            {
                return -1;
            }

            return _probeTappableElementBottom();
#else
            return -1;
#endif
        }

#if UNITY_ANDROID
        // ⚠️ 全文件一律用 #if UNITY_ANDROID + 运行时 Application.isEditor 判断，
        // **不要写成 #if UNITY_ANDROID && !UNITY_EDITOR**。
        //
        // 后者会让整段 JNI 代码在编辑器里不参与编译，于是里面任何编译错误
        // （漏 using、方法名写错、类型不存在）都只有真机打包时才会暴露——
        // 代价是白跑一轮十几分钟的构建。**这个坑已经踩过一次**：
        // _bottomInset 里用了 Core 的 SafeAreaLayout 却漏了 using EasyMoney.Core，
        // 364 个 EditMode 用例全绿，打包时才报 CS0103。
        //
        // 改成 #if UNITY_ANDROID 之后，只要当前 Build Target 是 Android（本项目一直是），
        // 编辑器就会把这段代码也编译一遍，写错当场就能发现；执行路径由 isEditor 早返回挡住。
        // 残留的边界：Build Target 切到非 Android 时这段仍不编译——别长期切走。

        /// <summary>SDK_INT 是常量，读一次记下来，省掉每次调用新建一个 AndroidJavaClass。</summary>
        private static int s_SdkInt = -1;

        /// <summary>
        /// activity → window → decorView → rootWindowInsets。
        ///
        /// ⚠️ <b>调用方负责释放返回值</b>——它是个 <c>AndroidJavaObject</c>，占一个
        /// JNI local ref，不能就这么丢掉。拿到的也可能是 <c>null</c>（视图还没 attach
        /// 到窗口，或者 inset 还没分发下来），调用方各自决定那算「读不到」还是「不用让」。
        /// </summary>
        private static AndroidJavaObject _rootWindowInsets()
        {
            using AndroidJavaClass oUnityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            using AndroidJavaObject oActivity = oUnityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

            if (oActivity == null)
            {
                return null;
            }

            using AndroidJavaObject oWindow = oActivity.Call<AndroidJavaObject>("getWindow");
            using AndroidJavaObject oDecorView = oWindow.Call<AndroidJavaObject>("getDecorView");

            return oDecorView.Call<AndroidJavaObject>("getRootWindowInsets");
        }

        private static int _readBottomInsetPx()
        {
            try
            {
                using AndroidJavaObject oInsets = _rootWindowInsets();

                if (oInsets == null)
                {
                    // 视图还没 attach 到窗口，或者 inset 还没分发下来。
                    // 返回 0 会让「不用让」和「还没读到」长得一样，所以调用方会重试
                    return 0;
                }

                return _bottomInset(oInsets);
            }
            catch (System.Exception oError)
            {
                // 不能静默返回 0：那样日志里「抛了异常」和「这台设备没导航栏」长得一模一样，
                // 真机上「修了跟没修一样」就无从排查了
                Debug.LogWarning($"[AndroidSystemBars] 读底部预留高度失败，按 0 处理：{oError}");
                return 0;
            }
        }

        /// <summary>
        /// 设两条系统栏的底色与图标颜色，并要求系统显示导航栏、别自动隐藏。
        /// 为什么只在 API 30+ 做、每一步为什么是这个顺序，见
        /// <see cref="EnsureSystemBarsUsable"/> 的注释。
        /// </summary>
        private static void _ensureSystemBarsUsable()
        {
            if (_sdkInt() < 30)
            {
                return;
            }

            try
            {
                using AndroidJavaClass oUnityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using AndroidJavaObject oActivity = oUnityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

                if (oActivity == null)
                {
                    return;
                }

                using AndroidJavaObject oWindow = oActivity.Call<AndroidJavaObject>("getWindow");
                using AndroidJavaObject oController = oWindow.Call<AndroidJavaObject>("getInsetsController");

                if (oController == null)
                {
                    return;
                }

                // 常量一律从类上读，不写死字面量——理由同 navigationBars()：
                // 含义改了不会有任何报错，只会悄悄失效
                using AndroidJavaClass oControllerClass =
                    new AndroidJavaClass("android.view.WindowInsetsController");
                using AndroidJavaClass oLayoutParamsClass =
                    new AndroidJavaClass("android.view.WindowManager$LayoutParams");

                int iLightStatusBars = oControllerClass.GetStatic<int>("APPEARANCE_LIGHT_STATUS_BARS");
                int iLightNavBars = oControllerClass.GetStatic<int>("APPEARANCE_LIGHT_NAVIGATION_BARS");
                int iBehaviorDefault = oControllerClass.GetStatic<int>("BEHAVIOR_DEFAULT");

                using AndroidJavaClass oType = new AndroidJavaClass("android.view.WindowInsets$Type");
                int iNavBars = oType.CallStatic<int>("navigationBars");

                // ① 先要「系统栏底色由这个窗口负责画」的资格。
                // **Unity 那套 Holo 主题没开这一位**，而 Window.setStatusBarColor /
                // setNavigationBarColor 只在窗口带这个标志时才生效——少了这一句，
                // 下面那两句颜色调用会被静默丢掉，真机上就是两条纯黑，跟没写代码一样
                oWindow.Call("addFlags",
                    oLayoutParamsClass.GetStatic<int>("FLAG_DRAWS_SYSTEM_BAR_BACKGROUNDS"));

                // ② 两条栏的底色都跟页面走
                int iArgb = _colorToArgb(Theme.BACKGROUND);
                oWindow.Call("setStatusBarColor", iArgb);
                oWindow.Call("setNavigationBarColor", iArgb);

                // ③ 底色浅 → 图标要深，两条栏都要。默认那套白图标画在浅底上等于没有
                int iLightBars = iLightStatusBars | iLightNavBars;
                oController.Call("setSystemBarsAppearance", iLightBars, iLightBars);

                // ④ 别自动隐藏。底部那 124px 是按「导航栏在」留出来的，
                // 系统把导航栏收走，留出来的就成了一块纯空白
                oController.Call("setSystemBarsBehavior", iBehaviorDefault);
                oController.Call("show", iNavBars);
            }
            catch (System.Exception oError)
            {
                // 失败就退回系统默认外观——不影响记账，但要在日志里留痕：
                // 否则真机上「还是黑的」和「压根没走到这里」分不出来
                Debug.LogWarning($"[AndroidSystemBars] 设置系统栏外观失败，按系统默认处理：{oError}");
            }
        }

        private static string _probeSystemBarAppearance()
        {
            try
            {
                using AndroidJavaClass oUnityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using AndroidJavaObject oActivity = oUnityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

                if (oActivity == null)
                {
                    return "读不到（无 Activity）";
                }

                using AndroidJavaObject oWindow = oActivity.Call<AndroidJavaObject>("getWindow");
                using AndroidJavaObject oAttributes = oWindow.Call<AndroidJavaObject>("getAttributes");

                // 三条都是「我们设了什么」，不是「系统画了什么」——两者不一致时
                // 就是系统把调用丢了，那才是这一行要回答的问题
                int iFlags = oAttributes.Get<int>("flags");
                int iStatusBar = oWindow.Call<int>("getStatusBarColor");
                int iNavigationBar = oWindow.Call<int>("getNavigationBarColor");

                return $"flags {iFlags:X8}  sb {iStatusBar:X8}  nb {iNavigationBar:X8}";
            }
            catch (System.Exception oError)
            {
                Debug.LogWarning($"[AndroidSystemBars] 读系统栏外观失败：{oError}");
                return "读失败";
            }
        }

        private static int _probeTappableElementBottom()
        {
            try
            {
                using AndroidJavaObject oInsets = _rootWindowInsets();

                if (oInsets == null)
                {
                    return -1;
                }

                return _tappableElementBottom(oInsets);
            }
            catch (System.Exception oError)
            {
                Debug.LogWarning($"[AndroidSystemBars] 诊断读数失败：{oError}");
                return -1;
            }
        }

        private static int _probeNavigationBarVisible()
        {
            if (_sdkInt() < 30)
            {
                return -1;
            }

            try
            {
                using AndroidJavaObject oInsets = _rootWindowInsets();

                if (oInsets == null)
                {
                    return -1;
                }

                using AndroidJavaClass oType = new AndroidJavaClass("android.view.WindowInsets$Type");
                int iNavBars = oType.CallStatic<int>("navigationBars");

                return oInsets.Call<bool>("isVisible", iNavBars) ? 1 : 0;
            }
            catch (System.Exception oError)
            {
                Debug.LogWarning($"[AndroidSystemBars] 读导航栏可见性失败：{oError}");
                return -1;
            }
        }

        /// <summary>
        /// 底部最终要预留的高度。两步，规则都在
        /// <see cref="SafeAreaLayout"/> 里（可测的纯函数），这里只负责把数取回来：
        /// 先从两个来源里挑出导航栏高度（<see cref="SafeAreaLayout.ResolveNavigationBarHeight"/>），
        /// 再按当前是手势还是三键决定留不留（<see cref="SafeAreaLayout.ResolveBottomInset"/>）。
        /// </summary>
        private static int _bottomInset(AndroidJavaObject oInsets)
        {
            int iNewApi = _sdkInt() >= 30 ? _insetsBottom(oInsets) : 0;
            int iLegacy = _legacyInsetBottom(oInsets);
            int iNavBar = SafeAreaLayout.ResolveNavigationBarHeight(iNewApi, iLegacy);

            return SafeAreaLayout.ResolveBottomInset(
                iNavBar, _tappableElementBottom(oInsets), _windowBottomGapPx());
        }

        /// <summary>
        /// 渲染面底边到屏幕底边还剩多少像素。
        ///
        /// 为什么需要它：**关掉「Start in Fullscreen Mode」之后，窗口自己就不铺到导航栏下面了**
        /// （真机读数：`Screen.height` 从 2400 变成 2276，正好少一个导航栏）。但
        /// <c>getInsets(navigationBars())</c> **照样报 124**——所以光看 inset 分不出
        /// 「窗口铺在导航栏下面」和「窗口已经停在导航栏上沿」，前者要留、后者留了就是白边，
        /// 真机现象是「内容整体偏高、标签栏下面空一块」。
        ///
        /// 两个来源取较大值：两个都是「屏幕原始高度」，个别机型上会有一个不灵
        /// （所以真机读数里两个都打出来对照）。**任一个报对了就能判对**，
        /// 而它们报大了也不会误判——铺满时 <c>Screen.height</c> 同样大，差额仍是 0。
        /// </summary>
        private static int _windowBottomGapPx()
        {
            if (Screen.height <= 0)
            {
                return 0;
            }

            int iSystemHeight = Mathf.Max(Display.main.systemHeight, Screen.currentResolution.height);

            int iGap = iSystemHeight - Screen.height;

            return iGap > 0 ? iGap : 0;
        }

        /// <summary>
        /// 系统栏里**可以点的**那部分有多高。三键导航下等于导航栏高度，手势导航下是 0。
        ///
        /// 为什么要专门读它：导航栏 inset 本身分辨不出导航模式。某些机型在手势导航下
        /// 照样报出三键的高度，于是底部白留一条——真机上验收时看到的就是这个。
        /// 而 tappableElement 衡量的是系统栏**实际占了多少地方**，与 inset 报多大无关，
        /// 是 Android 官方 edge-to-edge 指引给的分辨办法。
        ///
        /// ⚠️ <b>读不到一律返回 -1（「不知道」），绝不能返回 0。</b>
        /// 0 在这个语义里是「确定是手势导航」，会把本该留出来的导航栏让没了；
        /// 调用方按「不知道就当有导航键」处理。
        /// </summary>
        private static int _tappableElementBottom(AndroidJavaObject oInsets)
        {
            // WindowInsets$Type 在 API 30 以下不存在，FindClass 会直接抛
            if (_sdkInt() < 30)
            {
                return -1;
            }

            try
            {
                using AndroidJavaClass oType = new AndroidJavaClass("android.view.WindowInsets$Type");
                int iTappableElement = oType.CallStatic<int>("tappableElement");

                using AndroidJavaObject oBarInsets =
                    oInsets.Call<AndroidJavaObject>("getInsets", iTappableElement);

                if (oBarInsets == null)
                {
                    return -1;
                }

                // 与导航栏那边同一个坑：android.graphics.Insets.bottom 是字段不是方法
                return oBarInsets.Get<int>("bottom");
            }
            catch (System.Exception oError)
            {
                Debug.LogWarning($"[AndroidSystemBars] 读系统栏可点区域失败，按有导航键处理：{oError}");
                return -1;
            }
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

        /// <summary>
        /// Unity 的 <c>Color</c>（0..1 的 float）转 Android 的 ARGB 整数。
        /// Java 那边收的是有符号 int，不需要额外处理——位模式一样。
        /// </summary>
        private static int _colorToArgb(Color oColor)
        {
            Color32 oBytes = oColor;
            return (oBytes.a << 24) | (oBytes.r << 16) | (oBytes.g << 8) | oBytes.b;
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
                using AndroidJavaObject oInsets = _rootWindowInsets();

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
