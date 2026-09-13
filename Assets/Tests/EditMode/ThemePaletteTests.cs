using System.IO;
using EasyMoney.App.UI;
using NUnit.Framework;
using UnityEngine;

namespace EasyMoney.Tests
{
    /// <summary>
    /// 配色方案。App 层能测的东西不多，但 ThemePalette 是纯数据变换，而且
    /// 浅色配色在代码（ThemePalette.Light）和资源（Resources/theme.json）
    /// 各存了一份——运行时由 theme.json 覆盖 Light。
    ///
    /// 两处副本漂移了编译器不管，只会表现为「theme.json 删掉后界面突然变了个样」，
    /// 而且改配色的人多半只改一处。跟分类图标名是同一类坑，同样用测试兜住。
    /// </summary>
    public class ThemePaletteTests
    {
        /// <summary>颜色分量的容差。1/255 ≈ 0.0039，取略大一点。</summary>
        private const float TOLERANCE = 0.004f;

        [SetUp]
        public void SetUp()
        {
            // 分类色板是从 Theme.Palette 取的，先把当前配色钉成浅色，
            // 免得受别的测试类留下的状态影响
            Theme.Apply(ThemePalette.Light());
        }

        [Test]
        public void FromJson_ShadowOverride_Applies()
        {
            ThemePalette oPalette = ThemePalette.FromJson("{ \"shadow\": \"#FF000080\" }");

            Assert.AreEqual(1f, oPalette.Shadow.r, TOLERANCE, "红色分量应来自 theme.json");
            Assert.AreEqual(0f, oPalette.Shadow.g, TOLERANCE, "绿色分量应来自 theme.json");
            Assert.AreEqual(128f / 255f, oPalette.Shadow.a, TOLERANCE,
                "透明度应来自 theme.json——后两位十六进制是 alpha");
        }

        [Test]
        public void FromJson_InvalidShadow_FallsBackToDefault()
        {
            ThemePalette oDefault = ThemePalette.Light();
            ThemePalette oPalette = ThemePalette.FromJson("{ \"shadow\": \"不是颜色\" }");

            Assert.AreEqual(oDefault.Shadow.r, oPalette.Shadow.r, TOLERANCE,
                "格式不合法应退回默认，而不是变成黑色或抛异常");
            Assert.AreEqual(oDefault.Shadow.a, oPalette.Shadow.a, TOLERANCE,
                "透明度也要一起退回默认");
        }

        [Test]
        public void FromJson_MissingShadow_KeepsDefault()
        {
            ThemePalette oDefault = ThemePalette.Light();
            ThemePalette oPalette = ThemePalette.FromJson("{ \"primary\": \"#123456\" }");

            Assert.AreEqual(oDefault.Shadow.a, oPalette.Shadow.a, TOLERANCE,
                "旧版的 theme.json 里没有 shadow 键，不能因此把投影弄丢");
        }

        /// <summary>
        /// 随包的 theme.json 必须和 Light() 逐字段一致。
        ///
        /// theme.json 是给设计改色用的，不是「另一套主题」——它只覆盖 Light()，
        /// 所以两者不一致时，界面看到的是 theme.json，而 Light() 那套只在
        /// 「theme.json 缺失或解析失败」时才露脸，平时完全没机会被发现。
        /// </summary>
        [Test]
        public void ShippedThemeJson_MatchesLightPalette()
        {
            string sPath = Path.Combine(Application.dataPath, "Resources", "theme.json");
            Assert.IsTrue(File.Exists(sPath), $"随包的 theme.json 不见了：{sPath}");

            ThemePalette oFromFile = ThemePalette.FromJson(File.ReadAllText(sPath));
            ThemePalette oDefault = ThemePalette.Light();

            _assertSame("background", oFromFile.Background, oDefault.Background);
            _assertSame("surface", oFromFile.Surface, oDefault.Surface);
            _assertSame("primary", oFromFile.Primary, oDefault.Primary);
            _assertSame("expense", oFromFile.Expense, oDefault.Expense);
            _assertSame("income", oFromFile.Income, oDefault.Income);
            _assertSame("textPrimary", oFromFile.TextPrimary, oDefault.TextPrimary);
            _assertSame("textWeak", oFromFile.TextWeak, oDefault.TextWeak);
            _assertSame("divider", oFromFile.Divider, oDefault.Divider);
            _assertSame("barTrack", oFromFile.BarTrack, oDefault.BarTrack);
            _assertSame("scrim", oFromFile.Scrim, oDefault.Scrim);
            _assertSame("shadow", oFromFile.Shadow, oDefault.Shadow);
        }

        /// <summary>
        /// 深色配色也必须有一套投影色。深色下投影几乎看不见，但影子漏配会让
        /// AddCardShadow 拿到全透明的颜色——那时卡片在深色底上就真的一点边界都没有。
        /// </summary>
        [Test]
        public void Dark_ShadowIsVisible()
        {
            ThemePalette oDark = ThemePalette.Dark();

            Assert.Greater(oDark.Shadow.a, 0f, "深色主题的投影不能是全透明的");
        }

        // ── 分类色板（环形图）────────────────────────

        [Test]
        public void ChartColors_LightAreOpaqueAndDistinct()
        {
            Color[] lColors = ThemePalette.Light().ChartColors;

            Assert.GreaterOrEqual(lColors.Length, 6, "分类色太少的话，几个分类就会开始撞色");

            for (int i = 0; i < lColors.Length; i++)
            {
                Assert.AreEqual(1f, lColors[i].a, TOLERANCE, "色板里不该有半透明的颜色");

                // 相邻两块在环上挨着，颜色一样就分不出边界了
                Color oNext = lColors[(i + 1) % lColors.Length];
                float fDistance = Mathf.Abs(lColors[i].r - oNext.r)
                    + Mathf.Abs(lColors[i].g - oNext.g)
                    + Mathf.Abs(lColors[i].b - oNext.b);
                Assert.Greater(fDistance, 0.05f, $"第 {i} 号和下一号颜色几乎一样");
            }
        }

        [Test]
        public void ChartColors_Dark_IsNotEmpty()
        {
            Color[] lColors = ThemePalette.Dark().ChartColors;

            Assert.IsNotNull(lColors);
            Assert.Greater(lColors.Length, 0, "深色配色也得有一套分类色");
        }

        [Test]
        public void ChartColors_AreNotTakenFromThemeJson()
        {
            // 色板刻意不开放 theme.json 覆盖（见 ThemePalette.ChartColors 的说明），
            // 所以只改那 11 个键的 theme.json 不该把色板冲掉
            ThemePalette oPalette = ThemePalette.FromJson("{ \"primary\": \"#123456\" }");

            Assert.AreEqual(ThemePalette.Light().ChartColors.Length, oPalette.ChartColors.Length,
                "theme.json 里没有 chartColors 键，色板要原样跟着 Light()");
        }

        [Test]
        public void ChartColor_WrapsAroundAtPaletteLength()
        {
            Color[] lColors = ThemePalette.Light().ChartColors;

            // 色板长度固定，分类数由用户定——十几个分类很常见，只能回绕
            Assert.AreEqual(lColors[0], Theme.ChartColor(lColors.Length));
            Assert.AreEqual(lColors[1], Theme.ChartColor(lColors.Length + 1));
        }

        [Test]
        public void ChartColor_NegativeIndex_WrapsToTheEnd()
        {
            Color[] lColors = ThemePalette.Light().ChartColors;

            // C# 的取余会保留负号，不补这一下就会拿 -1 去索引，直接抛异常
            Assert.AreEqual(lColors[lColors.Length - 1], Theme.ChartColor(-1));
        }

        private static void _assertSame(string sField, Color oFromFile, Color oDefault)
        {
            Assert.AreEqual(oDefault.r, oFromFile.r, TOLERANCE,
                $"theme.json 的 {sField} 与 ThemePalette.Light() 对不上，改配色时两处都要改");
            Assert.AreEqual(oDefault.g, oFromFile.g, TOLERANCE, $"{sField} 的绿色分量对不上");
            Assert.AreEqual(oDefault.b, oFromFile.b, TOLERANCE, $"{sField} 的蓝色分量对不上");
            Assert.AreEqual(oDefault.a, oFromFile.a, TOLERANCE, $"{sField} 的透明度对不上");
        }
    }
}
