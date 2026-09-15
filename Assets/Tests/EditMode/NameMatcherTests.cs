using System.Collections.Generic;
using EasyMoney.Core;
using NUnit.Framework;

namespace EasyMoney.Tests
{
    /// <summary>
    /// 名字相似度算法测试。
    ///
    /// 这套规则是**真金白银**的：猜中一个名字就直接填进表单，用户可能看都不看就保存。
    /// 所以下面盯得最紧的不是「能不能猜中」，而是**猜不中的时候绝不能乱猜**——
    /// 唯一候选、长度差、单字名这三组边界用例才是本文件的主体，能猜中的反倒只有两条。
    ///
    /// 之所以要这么防：`DraftResolver` 原本是严格全等，匹配不上就留空、用户自己选，
    /// 那是安全的。加了模糊匹配之后，原本会留空的地方现在会自动填一个「最像的」，
    /// **一旦填错，界面上看不出任何异常**。
    /// </summary>
    public class NameMatcherTests
    {
        // ── 编辑距离 ────────────────────────────────

        [Test]
        public void EditDistance_Identical_IsZero()
        {
            Assert.AreEqual(0, NameMatcher.EditDistance("现金", "现金"));
            Assert.AreEqual(0, NameMatcher.EditDistance(string.Empty, string.Empty));
        }

        /// <summary>替换一个字。ASR 的同音错字主要就是这个形态（「现金」→「现佥」）。</summary>
        [Test]
        public void EditDistance_OneSubstitution_IsOne()
        {
            Assert.AreEqual(1, NameMatcher.EditDistance("现金", "现佥"));
        }

        /// <summary>增删各算一次，两个方向对称——ASR 漏字和多字都会发生。</summary>
        [Test]
        public void EditDistance_InsertionAndDeletion()
        {
            Assert.AreEqual(1, NameMatcher.EditDistance("支付宝", "支付"));
            Assert.AreEqual(1, NameMatcher.EditDistance("支付", "支付宝"));
        }

        [Test]
        public void EditDistance_EmptyString_IsOtherLength()
        {
            Assert.AreEqual(2, NameMatcher.EditDistance("现金", string.Empty));
            Assert.AreEqual(2, NameMatcher.EditDistance(string.Empty, "现金"));
        }

        /// <summary>
        /// 相邻两个字调个个儿，算**两步**而不是一步。
        ///
        /// 标准 Levenshtein 不做换位补偿（Damerau 才做）。这是有意的取舍：
        /// ASR 听错字是「同音替换」，「两个字换位」不是它常见的错法；
        /// 而开了换位补偿等于把可匹配的范围放宽一圈，代价全落在误配上。
        /// 宁可漏配，不可错配。
        /// </summary>
        [Test]
        public void EditDistance_AdjacentTransposition_CountsAsTwo()
        {
            Assert.AreEqual(2, NameMatcher.EditDistance("张三", "三张"));
        }

        // ── 阈值 ────────────────────────────────────

        [Test]
        public void MaxDistanceFor_GrowsWithLength()
        {
            // 一个字的名字不给容错空间：改一个字就是换了个名字
            Assert.AreEqual(0, NameMatcher.MaxDistanceFor(1));

            // 2-4 字允许改一个字（「张三」→「张散」）。这是本次要救的主要场景
            Assert.AreEqual(1, NameMatcher.MaxDistanceFor(2));
            Assert.AreEqual(1, NameMatcher.MaxDistanceFor(3));
            Assert.AreEqual(1, NameMatcher.MaxDistanceFor(4));

            // 5 字起才允许改两个：长名字里改一处仍是同一个名字的可能性更高
            Assert.AreEqual(2, NameMatcher.MaxDistanceFor(5));
        }

        /// <summary>空名字与非正长度都落在「不给容错」那一档，不该有负数或异常。</summary>
        [Test]
        public void MaxDistanceFor_NonPositive_IsZero()
        {
            Assert.AreEqual(0, NameMatcher.MaxDistanceFor(0));
            Assert.AreEqual(0, NameMatcher.MaxDistanceFor(-1));
        }

        // ── 挑候选：猜中的那些 ──────────────────────

        /// <summary>
        /// 库里有精确命中的名字时，取它。
        ///
        /// ⚠️ 这条真正守的是**重名**：库里有两个同名账户时，现在的行为是取第一个，
        /// 加了模糊匹配后若把「唯一候选」规则套到精确命中上，就会变成「并列 → 拒绝」，
        /// 那是**行为回归**——用户明明说了个库里有的名字，却被当成没匹配上。
        /// </summary>
        [Test]
        public void PickUniqueIndex_ExactWins()
        {
            List<string> lNames = new List<string> { "现金", "现金卡" };

            Assert.AreEqual(0, NameMatcher.PickUniqueIndex("现金", lNames));

            // 换个位置也认得出，不受候选顺序影响
            Assert.AreEqual(1, NameMatcher.PickUniqueIndex("现金", new List<string> { "现金卡", "现金" }));
        }

        [Test]
        public void PickUniqueIndex_DuplicateExact_ReturnsFirst()
        {
            // 库里有两条同名记录（数据脏了），取第一个而不是当成「并列所以不猜」
            List<string> lNames = new List<string> { "现金", "微信", "现金" };

            Assert.AreEqual(0, NameMatcher.PickUniqueIndex("现金", lNames));
        }

        /// <summary>改一个字能猜中——这是加这层容错的全部意义。</summary>
        [Test]
        public void PickUniqueIndex_OneCharOff_ReturnsThatIndex()
        {
            Assert.AreEqual(0, NameMatcher.PickUniqueIndex("现佥", new List<string> { "现金", "微信" }));
            Assert.AreEqual(1, NameMatcher.PickUniqueIndex("支忖宝", new List<string> { "现金", "支付宝" }));
        }

        // ── 挑候选：不能猜的那些（本文件的主体）─

        /// <summary>
        /// **两个候选一样近时，一个都不猜。**
        ///
        /// 「现佥」离「现金」和「现钞」都只差一个字——没有任何依据说明用户说的是哪个。
        /// 随便挑一个的话，用户会在账户页看到一个自己没提过的账户名，而界面上
        /// 完全看不出这是猜的。留空他一定看得见。
        /// </summary>
        [Test]
        public void PickUniqueIndex_TwoEquallyClose_ReturnsMinusOne()
        {
            List<string> lNames = new List<string> { "现金", "现钞" };

            Assert.AreEqual(-1, NameMatcher.PickUniqueIndex("现佥", lNames));

            // 同音错字也是一样：微信/微博 离「微搏」都是一步
            Assert.AreEqual(-1, NameMatcher.PickUniqueIndex("微搏", new List<string> { "微信", "微博" }));
        }

        /// <summary>
        /// 长度差超过一个字就不猜。
        ///
        /// 本例里「招商银行支行」离「招商银行」正好差两个字、**没有超出 6 字名的阈值 2**，
        /// 所以只有这条长度差规则能拦住它——漏了这条，用户说个带后缀的全名
        /// 就会被静默截成库里那个短名字。
        /// </summary>
        [Test]
        public void PickUniqueIndex_LengthGapTooBig_Skips()
        {
            List<string> lNames = new List<string> { "招商银行" };

            Assert.AreEqual(-1, NameMatcher.PickUniqueIndex("招商银行支行", lNames));
        }

        /// <summary>差得超过阈值不猜。4 字名只许改一个字，改两个已经是另一个名字了。</summary>
        [Test]
        public void PickUniqueIndex_OverThreshold_ReturnsMinusOne()
        {
            // ⚠️ 别拿「工商银行」对「招商银行」当反例——那两家**只差第一个字**，
            // 距离是 1、在额度内，会被认成同一个。那是本算法已知的代价，
            // 单独有一条 PickUniqueIndex_DifferentEntitiesOneCharApart 钉着它
            Assert.AreEqual(-1, NameMatcher.PickUniqueIndex("招尚很行", new List<string> { "招商银行" }));
            Assert.AreEqual(-1, NameMatcher.PickUniqueIndex("李四", new List<string> { "张三" }));
        }

        /// <summary>
        /// **已知代价，不是期望行为，但确实收下了。**
        ///
        /// 「工商银行」和「招商银行」是两家不同的银行，可它们只差一个字，
        /// 在编辑距离上与 ASR 的典型错法（「张三」→「张散」）**完全同形**——
        /// 阈值这一层分不出「听错了一个字」和「本来就说的是另一家」。
        /// 要分得出只能靠拼音（工 gong / 招 zhao 不同音），而那需要一张覆盖
        /// 常用汉字的拼音表加多音字处理，塞不进 Core，收益也撑不起这个代价。
        ///
        /// 收下它的理由：认错的结果**写在表单上是看得见的**，而最后那一下保存
        /// 必须用户自己按（「一句话记账是填表不是记账」那条规则的余荫）。
        ///
        /// 这条用例放在这里不是为了断言它「对」，而是**让后人调阈值时能看见这个代价**
        /// ——否则下次有人发现「说工商银行填出招商银行」，会当成一个新 bug 去修，
        /// 而把阈值收紧到 0 就等于把这次改动的主要收益全部还回去。
        /// </summary>
        [Test]
        public void PickUniqueIndex_DifferentEntitiesOneCharApart_AreStillMatched()
        {
            Assert.AreEqual(0, NameMatcher.PickUniqueIndex("工商银行", new List<string> { "招商银行" }));
        }

        /// <summary>
        /// 一个字的名字只认全等。
        ///
        /// 「卡」和「下」只差一步，但一个字的名字没有「大部分相同」可言——
        /// 改一个字就是换了个名字。阈值表里长度 1 给的是 0，这条钉住它。
        /// </summary>
        [Test]
        public void PickUniqueIndex_SingleCharName_RequiresExact()
        {
            Assert.AreEqual(-1, NameMatcher.PickUniqueIndex("下", new List<string> { "卡" }));
            Assert.AreEqual(0, NameMatcher.PickUniqueIndex("卡", new List<string> { "卡" }));
        }

        /// <summary>
        /// 没有候选可挑时返回 -1，**不抛异常**。
        ///
        /// null 表是真会发生的：库还没建好时调用方可能传 null，
        /// `TransactionPhraseParserTests.Parse_NullCategoryList_DoesNotThrow` 已经钉过
        /// 「一个语音输入把 App 崩掉，比它不工作严重得多」。
        /// </summary>
        [Test]
        public void PickUniqueIndex_NoCandidate_ReturnsMinusOne()
        {
            Assert.AreEqual(-1, NameMatcher.PickUniqueIndex("现金", null));
            Assert.AreEqual(-1, NameMatcher.PickUniqueIndex("现金", new List<string>()));
            Assert.AreEqual(-1, NameMatcher.PickUniqueIndex("现金", new List<string> { string.Empty }));
            Assert.AreEqual(-1, NameMatcher.PickUniqueIndex("不相干的东西", new List<string> { "现金" }));
        }

        /// <summary>空名字不该去匹配任何东西——否则「备注没写」会被猜成某个账户。</summary>
        [Test]
        public void PickUniqueIndex_EmptyWanted_ReturnsMinusOne()
        {
            List<string> lNames = new List<string> { "现金", "微信" };

            Assert.AreEqual(-1, NameMatcher.PickUniqueIndex(string.Empty, lNames));
            Assert.AreEqual(-1, NameMatcher.PickUniqueIndex(null, lNames));
            Assert.AreEqual(-1, NameMatcher.PickUniqueIndex("   ", lNames));
        }
    }
}
