namespace EasyMoney.Core
{
    /// <summary>
    /// 安全区换算：把屏幕像素的安全区矩形换算成归一化锚点，顺便让开 Android 的导航栏。
    ///
    /// 放 Core 的理由与 FontSlots / ReportForm 一样——这段规则留在 SafeAreaFitter 里
    /// 就只能靠真机肉眼看，而它错起来是「底部被导航栏盖住」或者「凭空多出一条白边」，
    /// 两种都不报错、不崩溃，界面上看上去都「挺正常」。
    ///
    /// ⚠️ <b>这里用 float，破的是「Core 一律 decimal」那条惯例。</b>
    /// decimal 在这个项目里防的是**四舍五入边界**（见 ReportForm 里那段注释：
    /// float 的 0.1 是 0.100000001，乘 100 以后落在边界上的值会飘）。而安全区换算
    /// 中间没有一次舍入、比较或累加，输入（Screen.safeArea 是 Rect）和输出
    /// （anchorMin 是 Vector2）两端本来就是 float，用 decimal 只是
    /// float → decimal → float 的仪式，还得在两端各写四次强转。破例只在这三个文件里。
    /// </summary>
    public static class SafeAreaLayout
    {
        /// <summary>
        /// <paramref name="fBottomInsetPx"/> 是**底部要预留的高度**，不是「导航栏有多高」——
        /// 手势导航下导航栏不占版面，那个值由
        /// <see cref="ResolveBottomInset"/> 归零后再传进来。
        /// </summary>
        public static SafeAreaAnchors Compute(
            SafeAreaRect oArea, float fScreenWidth, float fScreenHeight, float fBottomInsetPx)
        {
            // 屏幕尺寸还没准备好时除零会得到 NaN，锚点跟着全废。
            // 退回全屏至少是个能看的界面
            if (fScreenWidth <= 0f || fScreenHeight <= 0f)
            {
                return new SafeAreaAnchors(0f, 0f, 1f, 1f);
            }

            float fBottom = _extraBottom(oArea, fBottomInsetPx);

            // 顶边不动，只把底边往上抬 fBottom
            float fMinX = _clamp01(oArea.X / fScreenWidth);
            float fMinY = _clamp01((oArea.Y + fBottom) / fScreenHeight);
            float fMaxX = _clamp01((oArea.X + oArea.Width) / fScreenWidth);
            float fMaxY = _clamp01((oArea.Y + oArea.Height) / fScreenHeight);

            // 钳位之后仍然可能倒过来（宽度为负这种坏输入），那样节点会整个镜像。
            // 理由同 ReportForm.BarWidthRatio：锚点越界不会报错，只会画歪
            if (fMaxX < fMinX)
            {
                fMaxX = fMinX;
            }

            if (fMaxY < fMinY)
            {
                fMaxY = fMinY;
            }

            return new SafeAreaAnchors(fMinX, fMinY, fMaxX, fMaxY);
        }

        /// <summary>
        /// 底部还要额外让出多少像素。
        ///
        /// **幂等是这里的关键。** 不能无条件减掉要预留的高度：同一个 Screen.safeArea，
        /// Android 15 上铺满全屏（y = 0，导航栏是浮层），Android 13/14 上却已经把导航栏
        /// 排除掉了（y ≈ 导航栏高度）。无条件减在后者会凭空多出一条与导航栏等高的空白，
        /// 无条件不减在前者内容就被盖住——两种错都不报错。
        ///
        /// 取差额就同时照顾了两种情况，将来 Unity 把 safeArea 修好（UUM-121413 在 6.1 修复）
        /// 也不用改代码：那时 y ≈ 导航栏高度，差额自动变成 0。
        /// </summary>
        private static float _extraBottom(SafeAreaRect oArea, float fBottomInsetPx)
        {
            if (fBottomInsetPx <= 0f)
            {
                return 0f;
            }

            float fExtra = fBottomInsetPx - oArea.Y;
            if (fExtra <= 0f)
            {
                return 0f;
            }

            // 让出去之后高度就没了的话，宁可露一点也别让。安全区高度成 0 会让
            // 标题栏、标签栏、内容区（它按上下两个高度算，还会算出负数）全塌下来，
            // 整屏一片空白，而且不报错不崩——比露一条导航栏难查得多
            if (oArea.Height - fExtra <= 0f)
            {
                return 0f;
            }

            return fExtra;
        }

        /// <summary>
        /// 从两个来源里挑出导航栏高度（屏幕像素）。
        ///
        /// **新 API 优先，它为 0 才回退老的；两个都拿不到就是 0。**
        ///
        /// 为什么必须有回退：真机实测（Redmi K60 / Android 15 / 三键导航）
        /// API 30+ 的 <c>getInsets(Type.navigationBars())</c> 返回 **0**，而 deprecated 的
        /// <c>getSystemWindowInsetBottom()</c> 给出了正确的 **124px**。少了这一步，
        /// 现象就是「修了跟没修一样」——`extra` 算出来恒为 0，而界面上看不出原因。
        ///
        /// ⚠️ **不要改成取两者的较大值。** 两个来源都可能给出与当前导航模式不符的偏大值
        /// （手势导航时导航栏只有一条细缝），取大就会多让出一截——那是白边，同样不报错。
        /// 新 API 优先是因为它语义最准（按 Type 取，不受 deprecated 语义影响）。
        /// </summary>
        public static int ResolveNavigationBarHeight(int iNewApiPx, int iLegacyPx)
        {
            if (iNewApiPx > 0)
            {
                return iNewApiPx;
            }

            if (iLegacyPx > 0)
            {
                return iLegacyPx;
            }

            return 0;
        }

        /// <summary>
        /// 底部到底要预留多少像素——**导航栏不占版面时就是 0**。
        ///
        /// 三键导航和手势导航要区别对待：
        ///
        /// * **三键**：屏幕底部一条 48dp 的实心按钮条，内容钻到它下面就永远看不见，
        ///   必须整条让出来（微信也是这么做的，标签栏正好落在导航键上方）。
        /// * **手势**：只有一条贴底的细提示条，**不占版面**，标签栏应该一直落到屏幕最底，
        ///   让出来的话底部就凭空多一条空白——真机上验收时看到的就是这个。
        ///
        /// 怎么分辨这两者：<c>WindowInsets.Type.tappableElement()</c> 的 bottom
        /// **为 0 就是手势导航**（没有可点的系统栏），非 0 就是三键。
        /// 这是 Android 官方 edge-to-edge 指引给的分辨办法，不是拍的——
        /// 它衡量的是系统栏**实际占了多少地方**，与 inset 本身报多大无关，
        /// 而某些机型在手势导航下照样报出三键的高度。
        ///
        /// ⚠️ <b>读不到（API 30 以下）时按「有导航键」处理，不要按手势算。</b>
        /// 两个方向都可能错，但错法不一样：多留一截只是难看，
        /// 少留一截是内容被系统栏压住、再也点不到——宁可难看。
        /// 而且 Android 13/14 及更早的 <c>Screen.safeArea</c> 本来就排除了导航栏，
        /// 多留的那截会被 <see cref="Compute"/> 里的差额规则抵消掉，这层兜底几乎不会生效。
        /// </summary>
        public static int ResolveBottomInset(int iNavigationBarPx, int iTappableElementPx)
        {
            if (iNavigationBarPx <= 0)
            {
                return 0;
            }

            if (iTappableElementPx < 0)
            {
                return iNavigationBarPx;
            }

            if (iTappableElementPx == 0)
            {
                return 0;
            }

            return iNavigationBarPx;
        }

        private static float _clamp01(float fValue)
        {
            if (fValue < 0f)
            {
                return 0f;
            }

            if (fValue > 1f)
            {
                return 1f;
            }

            return fValue;
        }
    }
}
