namespace EasyMoney.Core
{
    /// <summary>
    /// 屏幕像素坐标下的一个矩形（左下角为原点）。
    ///
    /// 为什么不直接传四个 float：这四个数的顺序写错了照样编译通过，
    /// 而 Width 和 Height 传反只会让安全区算歪，不报错也不崩。
    /// 给它一个名字，调用点就是自解释的。
    ///
    /// 与 <see cref="SafeAreaAnchors"/> 分开而不是共用一个类型：一个是像素、
    /// 一个是归一化比例，共用了就会把「拿锚点当矩形喂回去」这种错变得可编译。
    /// </summary>
    public readonly struct SafeAreaRect
    {
        private readonly float m_X;
        private readonly float m_Y;
        private readonly float m_Width;
        private readonly float m_Height;

        public SafeAreaRect(float fX, float fY, float fWidth, float fHeight)
        {
            m_X = fX;
            m_Y = fY;
            m_Width = fWidth;
            m_Height = fHeight;
        }

        public float X => m_X;

        public float Y => m_Y;

        public float Width => m_Width;

        public float Height => m_Height;
    }
}
