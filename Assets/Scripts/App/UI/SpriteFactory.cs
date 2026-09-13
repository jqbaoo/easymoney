using UnityEngine;

namespace EasyMoney.App.UI
{
    /// <summary>
    /// 卡片与按钮的底图。资源优先、代码兜底：
    ///   1. Assets/Resources/Sprites/card.png —— 美术给了图就用图（九宫格，带 border）
    ///   2. 程序化生成圆角矩形 —— 没给图时自己画，界面照样能看
    ///
    /// 两条路产出的都是九宫格 Sprite，Image.type 设成 Sliced 后任意拉伸不变形，
    /// 所以美术随时把图丢进来都能无缝替换，调用方完全无感。
    /// </summary>
    public static class SpriteFactory
    {
        /// <summary>圆点贴图的边长。够大才经得起拉伸，反正只是一张纯白圆。</summary>
        private const int CIRCLE_TEXTURE_SIZE = 64;

        private static Sprite s_Card;
        private static Sprite s_Button;
        private static Sprite s_Circle;

        /// <summary>卡片底图。</summary>
        public static Sprite Card()
        {
            if (s_Card == null)
            {
                s_Card = AssetProvider.Sprite(AssetPaths.CARD_SPRITE);
                if (s_Card == null)
                {
                    s_Card = _createRounded(64, (int)Theme.CARD_RADIUS);
                }
            }

            return s_Card;
        }

        /// <summary>按钮底图。没单独给 button.png 时复用卡片底图。</summary>
        public static Sprite Button()
        {
            if (s_Button == null)
            {
                s_Button = AssetProvider.Sprite(AssetPaths.BUTTON_SPRITE);
                if (s_Button == null)
                {
                    s_Button = Card();
                }
            }

            return s_Button;
        }

        /// <summary>
        /// 正圆，环形图的图例色点用——圆角半径取半边长，同一个 SDF 出来的就是圆。
        ///
        /// 与 Card / Button 的区别在用法不在画法：那两张是九宫格（`Image.type = Sliced`，
        /// 任意拉伸不变形），这张是整张（`Simple`）——色点是固定大小的正方形，不需要九宫格。
        /// 没有对应的资源图，纯代码生成：一个 20 像素的圆点不值得占一张美术图。
        /// </summary>
        public static Sprite Circle()
        {
            if (s_Circle == null)
            {
                s_Circle = _createRounded(CIRCLE_TEXTURE_SIZE, CIRCLE_TEXTURE_SIZE / 2);
            }

            return s_Circle;
        }

        private static Sprite _createRounded(int iSize, int iRadius)
        {
            Texture2D oTexture = new Texture2D(iSize, iSize, TextureFormat.RGBA32, false);
            oTexture.filterMode = FilterMode.Bilinear;
            oTexture.wrapMode = TextureWrapMode.Clamp;

            Color32[] lPixels = new Color32[iSize * iSize];

            for (int iY = 0; iY < iSize; iY++)
            {
                for (int iX = 0; iX < iSize; iX++)
                {
                    // 到圆角矩形的有符号距离：>0 在外部，<0 在内部
                    float fDistance = _roundedRectSdf(
                        iX + 0.5f, iY + 0.5f, iSize, iSize, iRadius);

                    // 用 1 像素宽度做抗锯齿过渡
                    float fAlpha = Mathf.Clamp01(0.5f - fDistance);

                    lPixels[iY * iSize + iX] = new Color32(255, 255, 255, (byte)(fAlpha * 255f));
                }
            }

            oTexture.SetPixels32(lPixels);
            oTexture.Apply();

            return Sprite.Create(
                oTexture,
                new Rect(0f, 0f, iSize, iSize),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(iRadius, iRadius, iRadius, iRadius));
        }

        private static float _roundedRectSdf(float fX, float fY, float fWidth, float fHeight, float fRadius)
        {
            float fHalfW = fWidth * 0.5f;
            float fHalfH = fHeight * 0.5f;

            float fDx = Mathf.Abs(fX - fHalfW) - (fHalfW - fRadius);
            float fDy = Mathf.Abs(fY - fHalfH) - (fHalfH - fRadius);

            float fOutsideX = Mathf.Max(fDx, 0f);
            float fOutsideY = Mathf.Max(fDy, 0f);
            float fOutside = Mathf.Sqrt(fOutsideX * fOutsideX + fOutsideY * fOutsideY);

            float fInside = Mathf.Min(Mathf.Max(fDx, fDy), 0f);

            return fOutside + fInside - fRadius;
        }
    }
}
