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
        public static SafeAreaAnchors Compute(
            SafeAreaRect oArea, float fScreenWidth, float fScreenHeight, float fNavigationBarPx)
        {
            // 屏幕尺寸还没准备好时除零会得到 NaN，锚点跟着全废。
            // 退回全屏至少是个能看的界面
            if (fScreenWidth <= 0f || fScreenHeight <= 0f)
            {
                return new SafeAreaAnchors(0f, 0f, 1f, 1f);
            }

            float fBottom = _extraBottom(oArea, fNavigationBarPx);

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
        /// **幂等是这里的关键。** 不能无条件减掉导航栏高度：同一个 Screen.safeArea，
        /// Android 15 上铺满全屏（y = 0，导航栏是浮层），Android 13/14 上却已经把导航栏
        /// 排除掉了（y ≈ 导航栏高度）。无条件减在后者会凭空多出一条与导航栏等高的空白，
        /// 无条件不减在前者内容就被盖住——两种错都不报错。
        ///
        /// 取差额就同时照顾了两种情况，将来 Unity 把 safeArea 修好（UUM-121413 在 6.1 修复）
        /// 也不用改代码：那时 y ≈ 导航栏高度，差额自动变成 0。
        /// </summary>
        private static float _extraBottom(SafeAreaRect oArea, float fNavigationBarPx)
        {
            if (fNavigationBarPx <= 0f)
            {
                return 0f;
            }

            float fExtra = fNavigationBarPx - oArea.Y;
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
