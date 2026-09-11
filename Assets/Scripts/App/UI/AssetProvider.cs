using System.Collections.Generic;
using UnityEngine;

namespace EasyMoney.App.UI
{
    /// <summary>
    /// 统一资源加载入口。全项目只有这里碰 Resources.Load。
    ///
    /// 关键约定：**找不到就返回 null，绝不抛异常、绝不返回占位图**。
    /// 调用方拿到 null 后走代码兜底（文字符号 / 程序化生成），
    /// 于是「美术还没给图」和「美术已经给了图」是同一份代码，
    /// 把文件丢进 Resources 目录重启即生效，零代码改动。
    /// </summary>
    public static class AssetProvider
    {
        private static readonly Dictionary<string, Sprite> s_Icons = new Dictionary<string, Sprite>();
        private static readonly Dictionary<string, Sprite> s_Sprites = new Dictionary<string, Sprite>();
        private static readonly Dictionary<string, Font> s_Fonts = new Dictionary<string, Font>();

        /// <summary>按名字取图标（Assets/Resources/Icons/&lt;名字&gt;）。</summary>
        public static Sprite Icon(string sName)
        {
            return _loadSprite(s_Icons, AssetPaths.ICON_DIR, sName);
        }

        /// <summary>按名字取图片（Assets/Resources/Sprites/&lt;名字&gt;）。</summary>
        public static Sprite Sprite(string sName)
        {
            return _loadSprite(s_Sprites, AssetPaths.SPRITE_DIR, sName);
        }

        /// <summary>按名字取字体（Assets/Resources/Fonts/&lt;名字&gt;）。</summary>
        public static Font Font(string sName)
        {
            if (string.IsNullOrEmpty(sName))
            {
                return null;
            }

            if (s_Fonts.TryGetValue(sName, out Font oCached))
            {
                return oCached;
            }

            Font oLoaded = Resources.Load<Font>($"{AssetPaths.FONT_DIR}/{sName}");
            s_Fonts[sName] = oLoaded;
            return oLoaded;
        }

        /// <summary>读取一个文本资源（用于 theme.json）。</summary>
        public static string Text(string sName)
        {
            TextAsset oAsset = Resources.Load<TextAsset>(sName);
            return oAsset != null ? oAsset.text : null;
        }

        /// <summary>
        /// 清空缓存。开发期在 Play 模式下新拖入图片后调用它可以立即生效；
        /// 正常运行时不需要调用。
        /// </summary>
        public static void ClearCache()
        {
            s_Icons.Clear();
            s_Sprites.Clear();
            s_Fonts.Clear();
        }

        private static Sprite _loadSprite(Dictionary<string, Sprite> dCache, string sDir, string sName)
        {
            if (string.IsNullOrEmpty(sName))
            {
                return null;
            }

            if (dCache.TryGetValue(sName, out Sprite oCached))
            {
                return oCached;
            }

            // null 也缓存起来：资源缺失是常态（美术还没给图），
            // 不缓存的话每次刷新界面都要走一遍 Resources.Load。
            Sprite oLoaded = Resources.Load<Sprite>($"{sDir}/{sName}");
            dCache[sName] = oLoaded;
            return oLoaded;
        }
    }
}
