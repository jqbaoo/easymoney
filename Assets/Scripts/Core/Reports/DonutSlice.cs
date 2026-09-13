namespace EasyMoney.Core
{
    /// <summary>
    /// 环形图上的一片。角度以 12 点方向为 0°、顺时针增大。
    /// </summary>
    public class DonutSlice
    {
        public decimal StartDegrees { get; set; }

        public decimal EndDegrees { get; set; }

        /// <summary>
        /// 色板槽位，与分类在明细列表里的下标一致（第 0 项就是明细列表的第一行）。
        ///
        /// 这里只给「第几号色」而不是色值：具体取哪个颜色是 App 层的事，
        /// 而 Core 不许 using UnityEngine，压根拿不到 Color。
        /// </summary>
        public int SeriesIndex { get; set; }

        /// <summary>张角（度）。</summary>
        public decimal SpanDegrees => EndDegrees - StartDegrees;
    }
}
