using EasyMoney.Core;
using NUnit.Framework;

namespace EasyMoney.Tests
{
    /// <summary>
    /// 字重 → 资源槽位的映射，以及字体缺失时的回退链。
    ///
    /// 这两条规则页面测不到：槽位名写错（Medium 指到 bold 的文件）不会报错，
    /// 只会让两档字重静默渲染成一样，真机上肉眼才看得出来。所以在这里逐个钉死。
    /// </summary>
    public class FontSlotsTests
    {
        [Test]
        public void SlotOf_Regular_KeepsTheBaseSlotName()
        {
            Assert.AreEqual("main", FontSlots.SlotOf(FontWeight.Regular),
                "Regular 必须沿用既有的 main 槽位名，改名会让已经放进 Fonts/ 的 main.otf 失效");
        }

        [Test]
        public void SlotOf_Medium_IsMainMedium()
        {
            Assert.AreEqual("main_medium", FontSlots.SlotOf(FontWeight.Medium),
                "槽位名要和 Assets/Resources/Fonts/ 下的文件名一致，对不上 Resources.Load 会返回 null");
        }

        [Test]
        public void SlotOf_Bold_IsMainBold()
        {
            Assert.AreEqual("main_bold", FontSlots.SlotOf(FontWeight.Bold),
                "槽位名要和 Assets/Resources/Fonts/ 下的文件名一致，对不上 Resources.Load 会返回 null");
        }

        [Test]
        public void SlotOf_EveryWeight_MapsToADistinctSlot()
        {
            string sRegular = FontSlots.SlotOf(FontWeight.Regular);
            string sMedium = FontSlots.SlotOf(FontWeight.Medium);
            string sBold = FontSlots.SlotOf(FontWeight.Bold);

            Assert.AreNotEqual(sRegular, sMedium,
                "Regular 与 Medium 指向同一槽位，两档字重会渲染成一模一样");
            Assert.AreNotEqual(sMedium, sBold,
                "Medium 与 Bold 指向同一槽位，两档字重会渲染成一模一样");
            Assert.AreNotEqual(sRegular, sBold,
                "Regular 与 Bold 指向同一槽位，两档字重会渲染成一模一样");
        }

        [Test]
        public void FallbackChain_Regular_ContainsOnlyItself()
        {
            FontWeight[] lChain = FontSlots.FallbackChain(FontWeight.Regular);

            Assert.AreEqual(1, lChain.Length,
                "Regular 就是兜底档位，它后面不该再挂别的字重");
            Assert.AreEqual(FontWeight.Regular, lChain[0]);
        }

        [Test]
        public void FallbackChain_Medium_PrefersMediumAndEndsWithRegular()
        {
            FontWeight[] lChain = FontSlots.FallbackChain(FontWeight.Medium);

            Assert.AreEqual(FontWeight.Medium, lChain[0],
                "回退链要按优先级排，首选在前，否则永远轮不到 Medium");
            Assert.AreEqual(FontWeight.Regular, lChain[lChain.Length - 1],
                "链尾必须是 Regular，否则字体文件缺失时会一路掉到没有字体可用");
        }

        [Test]
        public void FallbackChain_Bold_PrefersBoldAndEndsWithRegular()
        {
            FontWeight[] lChain = FontSlots.FallbackChain(FontWeight.Bold);

            Assert.AreEqual(FontWeight.Bold, lChain[0],
                "回退链要按优先级排，首选在前，否则永远轮不到 Bold");
            Assert.AreEqual(FontWeight.Regular, lChain[lChain.Length - 1],
                "链尾必须是 Regular，否则字体文件缺失时会一路掉到没有字体可用");
        }

        [Test]
        public void FallbackChain_EveryWeight_HasNoDuplicateEntries()
        {
            foreach (FontWeight eWeight in new[] { FontWeight.Regular, FontWeight.Medium, FontWeight.Bold })
            {
                CollectionAssert.AllItemsAreUnique(FontSlots.FallbackChain(eWeight),
                    $"字重 {eWeight} 的回退链里有重复项，解析时同一档会白试两次");
            }
        }
    }
}
