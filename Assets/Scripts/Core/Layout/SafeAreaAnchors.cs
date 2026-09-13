namespace EasyMoney.Core
{
    /// <summary>
    /// 归一化锚点（左下、右上），直接喂给 RectTransform 的 anchorMin / anchorMax。
    ///
    /// 单独定义而不是返回四个 float 或元组：四个 float 排成一串时
    /// 「MinX, MaxX, MinY, MaxY」和「MinX, MinY, MaxX, MaxY」一样能编译，
    /// 而这个类型存在的全部意义就是不让它们弄混。
    /// </summary>
    public readonly struct SafeAreaAnchors
    {
        private readonly float m_MinX;
        private readonly float m_MinY;
        private readonly float m_MaxX;
        private readonly float m_MaxY;

        public SafeAreaAnchors(float fMinX, float fMinY, float fMaxX, float fMaxY)
        {
            m_MinX = fMinX;
            m_MinY = fMinY;
            m_MaxX = fMaxX;
            m_MaxY = fMaxY;
        }

        public float MinX => m_MinX;

        public float MinY => m_MinY;

        public float MaxX => m_MaxX;

        public float MaxY => m_MaxY;
    }
}
