using System.Collections.Generic;
using EasyMoney.Core;
using UnityEngine;

namespace EasyMoney.App.UI.Reports
{
    /// <summary>
    /// 环形图的贴图。UGUI 里没有现成的扇形控件，也不值得为一个环引入图表库，
    /// 所以自己画一张：逐像素判断它落在哪一片的角度范围内，写成那个分类的颜色。
    ///
    /// 一次刷新画一张。320 见方约 10 万像素，其中落在环上的六成左右，
    /// 只在切月 / 切收支 / 切视图时发生，不在每帧里。
    /// </summary>
    public static class DonutSprite
    {
        /// <summary>
        /// 贴图边长。环按 440 的设计像素铺开，贴图给到 320——
        /// 在 2x 屏上略糊一点点，但内外缘都做了覆盖率过渡，实际看不出来，
        /// 而再往上翻一倍就要明显拖慢刷新了。
        /// </summary>
        public const int TEXTURE_SIZE = 320;

        private static Sprite s_Sprite;
        private static Texture2D s_Texture;

        /// <param name="lSlices">扇区，角度已经闭合</param>
        /// <param name="fInnerRatio">内径占外径的比例</param>
        public static Sprite Render(IList<DonutSlice> lSlices, float fInnerRatio)
        {
            _release();

            int iSize = TEXTURE_SIZE;
            float fHalf = iSize * 0.5f;
            float fInner = Mathf.Clamp01(fInnerRatio) * fHalf;

            // 环外留透明。Color32 的 default 就是全 0，写出来是为了让「没画到的地方」一眼可见
            Color32 oClear = default;

            Texture2D oTexture = new Texture2D(iSize, iSize, TextureFormat.RGBA32, false);
            oTexture.filterMode = FilterMode.Bilinear;
            oTexture.wrapMode = TextureWrapMode.Clamp;

            Color32[] lPixels = new Color32[iSize * iSize];

            for (int iY = 0; iY < iSize; iY++)
            {
                float fDy = iY + 0.5f - fHalf;

                for (int iX = 0; iX < iSize; iX++)
                {
                    float fDx = iX + 0.5f - fHalf;
                    float fRadius = Mathf.Sqrt(fDx * fDx + fDy * fDy);

                    // 环内环外先剔掉，剩下的才去算角度——atan2 是这一圈里最贵的一步
                    if (fRadius > fHalf || fRadius < fInner)
                    {
                        lPixels[iY * iSize + iX] = oClear;
                        continue;
                    }

                    // 12 点方向为 0°、顺时针增大：atan2(x, y) 正好就是这个方向
                    float fAngle = Mathf.Atan2(fDx, fDy) * Mathf.Rad2Deg;
                    if (fAngle < 0f)
                    {
                        fAngle += 360f;
                    }

                    Color oColor = _colorAt(lSlices, fAngle);
                    if (oColor.a <= 0f)
                    {
                        lPixels[iY * iSize + iX] = oClear;
                        continue;
                    }

                    // 内外缘各做 1 像素的覆盖率过渡。环的边界是斜的，
                    // 直接按「在不在环上」二值化，圆边会有明显的台阶
                    oColor.a *= Mathf.Min(
                        Mathf.Clamp01(fHalf - fRadius + 0.5f),
                        Mathf.Clamp01(fRadius - fInner + 0.5f));

                    lPixels[iY * iSize + iX] = oColor;
                }
            }

            oTexture.SetPixels32(lPixels);
            oTexture.Apply();

            s_Texture = oTexture;
            s_Sprite = Sprite.Create(oTexture, new Rect(0f, 0f, iSize, iSize),
                new Vector2(0.5f, 0.5f), 100f);

            return s_Sprite;
        }

        /// <summary>
        /// 这个角度归哪一片。片与片之间不留缝也不做过渡——交界处两边本来就是
        /// 硬碰硬画在一起的，各自再淡出一次反而会露出一道底色。
        /// </summary>
        private static Color _colorAt(IList<DonutSlice> lSlices, float fAngle)
        {
            if (lSlices == null)
            {
                return Theme.TRANSPARENT;
            }

            foreach (DonutSlice oSlice in lSlices)
            {
                // 右端用严格小于：角度由 atan2 得来，永远落在 [0, 360)，
                // 360 那一头取不到；而末片正好收口在 360，用 <= 只是白判一次
                if (fAngle >= (float)oSlice.StartDegrees && fAngle < (float)oSlice.EndDegrees)
                {
                    return Theme.ChartColor(oSlice.SeriesIndex);
                }
            }

            // 占比合不拢时留下的空档就留白，不硬塞一个颜色进去
            return Theme.TRANSPARENT;
        }

        /// <summary>
        /// 放掉上一张。贴图是原生对象，不 Destroy 就会一直挂着——切月、切收支、
        /// 切视图每样都画一张新的，不释放的话内存一路涨，而且不报任何错。
        ///
        /// 先放 Sprite 再放贴图：Sprite 引用着贴图。
        /// </summary>
        private static void _release()
        {
            UiFactory.DestroyObject(s_Sprite);
            s_Sprite = null;

            UiFactory.DestroyObject(s_Texture);
            s_Texture = null;
        }
    }
}
