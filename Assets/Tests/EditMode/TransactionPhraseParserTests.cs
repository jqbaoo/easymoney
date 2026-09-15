using System.Collections.Generic;
using EasyMoney.Core;
using NUnit.Framework;

namespace EasyMoney.Tests
{
    /// <summary>
    /// 一句话记账的解析测试。
    ///
    /// 盯住四件事：**语序无关**（字段顺序随便变、ASR 漏段也只丢那一个字段）、
    /// **「类型」标签的值域消歧**（「类型收入」是账单类型、「类型餐饮」是分类）、
    /// **金额的单位与边界**（元进分出，0 与负数拒绝）、
    /// **兜底**（解析不出来返回 false，而不是拿一份空草稿去冲掉表单）。
    ///
    /// 金额断言一律写成 Money.FromYuan(N).Cents 而不是裸数字：读的人不用回表里
    /// 查「3000 是啥」，同时这个写法还把 FromYuan 一起验了。
    /// </summary>
    public class TransactionPhraseParserTests
    {
        /// <summary>
        /// 一份够用的分类名表。真实场景里它来自数据库，这里给固定的几个。
        /// 「类型X」的消歧要靠它，所以涉及消歧的用例都把这个表传进去。
        /// </summary>
        private static readonly List<string> CATEGORIES =
            new List<string> { "餐饮", "交通", "购物", "工资" };

        // ── A. 句式鲁棒性 ────────────────────────────

        [Test]
        public void Parse_StandardOrder_FillsEveryLabelledField()
        {
            Assert.IsTrue(_parse("记一笔，账户张三，金额30，类型餐饮，备注哈哈哈",
                out TransactionDraft oDraft));

            Assert.AreEqual(Money.FromYuan(30).Cents, oDraft.AmountCents);
            Assert.AreEqual("餐饮", oDraft.CategoryName);
            Assert.AreEqual("张三", oDraft.AccountName);
            Assert.AreEqual("哈哈哈", oDraft.Note);

            // 触发词被剔除了，不该混进「没认出来的字」里报给用户
            Assert.IsEmpty(oDraft.Unrecognized, "「记一笔」是触发词，不是未识别文本");
        }

        [Test]
        public void Parse_ShuffledWithEmptySegment_MatchesStandardOrder()
        {
            // 「记一笔」跑到中间、连着两个逗号（ASR 的停顿常被识别成分隔符）。
            // 标签锚定的句式下，这两件事都不该影响结果
            Assert.IsTrue(_parse("金额30，账户张三，记一笔，，类型餐饮，备注哈哈哈",
                out TransactionDraft oDraft));

            Assert.AreEqual(Money.FromYuan(30).Cents, oDraft.AmountCents);
            Assert.AreEqual("餐饮", oDraft.CategoryName);
            Assert.AreEqual("张三", oDraft.AccountName);
            Assert.AreEqual("哈哈哈", oDraft.Note);
        }

        [Test]
        public void Parse_WithoutTriggerWord_StillParses()
        {
            // 触发词不是门槛：UI 上按的是「语音记账」，意图已经由那一按确定了
            Assert.IsTrue(_parse("金额30，类型餐饮", out TransactionDraft oDraft));

            Assert.AreEqual(Money.FromYuan(30).Cents, oDraft.AmountCents);
            Assert.AreEqual("餐饮", oDraft.CategoryName);
        }

        [Test]
        public void Parse_TriggerWordAtEnd_IsIgnored()
        {
            Assert.IsTrue(_parse("金额30，备注哈哈哈，记一笔", out TransactionDraft oDraft));

            Assert.AreEqual("哈哈哈", oDraft.Note);
            Assert.AreEqual(Money.FromYuan(30).Cents, oDraft.AmountCents);
        }

        [Test]
        public void Parse_EmptyOrBlankInput_ReturnsFalse()
        {
            Assert.IsFalse(_parse(string.Empty, out _));
            Assert.IsFalse(_parse(null, out _));
            Assert.IsFalse(_parse("   ", out _));
            Assert.IsFalse(_parse("，，，", out _), "全是分隔符等于什么都没说");
        }

        [Test]
        public void Parse_LooseTextBeforeFirstLabel_IsReportedNotDropped()
        {
            // 静默吞掉的话，用户看到金额填上了，不会发现前面那几个字没被理
            Assert.IsTrue(_parse("随便说说，金额30", out TransactionDraft oDraft));

            Assert.AreEqual(new List<string> { "随便说说" }, oDraft.Unrecognized);
            Assert.AreEqual(Money.FromYuan(30).Cents, oDraft.AmountCents);
        }

        [Test]
        public void Parse_TransferFields_AreFilled()
        {
            // 转账有「转入」而没有分类，账户与转入这两个标签都得认
            Assert.IsTrue(_parse("记一笔，类型转账，金额100，转入微信，账户现金",
                out TransactionDraft oDraft));

            Assert.AreEqual(TxType.Transfer, oDraft.Type);
            Assert.AreEqual(Money.FromYuan(100).Cents, oDraft.AmountCents);
            Assert.AreEqual("现金", oDraft.AccountName);
            Assert.AreEqual("微信", oDraft.ToAccountName);
        }

        // ── B. 「类型」标签的值域消歧 ──────────────────

        [Test]
        public void Parse_TypeLabelWithIncomeValue_SetsTxType()
        {
            Assert.IsTrue(_parse("记一笔，类型收入，金额500", out TransactionDraft oDraft));

            Assert.AreEqual(TxType.Income, oDraft.Type);
            Assert.AreEqual(Money.FromYuan(500).Cents, oDraft.AmountCents);
            Assert.AreEqual(string.Empty, oDraft.CategoryName);
        }

        [Test]
        public void Parse_TypeLabelWithCategoryName_SetsCategory()
        {
            // 同一个标签、同一个位置，只有值不同。用户不必为了迁就程序
            // 去区分「类型」和「分类」两个词
            Assert.IsTrue(_parse("记一笔，类型餐饮，金额30", out TransactionDraft oDraft));

            Assert.AreEqual("餐饮", oDraft.CategoryName);
            Assert.IsFalse(oDraft.Type.HasValue, "「餐饮」不在账单类型词表里，不该被认成类型");
        }

        [Test]
        public void Parse_TypeLabelWithTransferValue_SetsTransfer()
        {
            Assert.IsTrue(_parse("记一笔，类型转账，金额30", out TransactionDraft oDraft));

            Assert.AreEqual(TxType.Transfer, oDraft.Type);
        }

        [Test]
        public void Parse_TypeLabelWithUnrecognizedValue_IsReported()
        {
            // 两边都不在时不猜。猜错会把账记到别的分类上，而用户不一定发现；
            // 报出来让他看见，比默默填一个强
            Assert.IsTrue(_parse("记一笔，类型莫名其妙，金额30", out TransactionDraft oDraft));

            Assert.IsFalse(oDraft.Type.HasValue);
            Assert.AreEqual(string.Empty, oDraft.CategoryName);
            Assert.AreEqual(new List<string> { "莫名其妙" }, oDraft.Unrecognized);
            Assert.AreEqual(Money.FromYuan(30).Cents, oDraft.AmountCents);
        }

        /// <summary>
        /// 「类型」后面的分类名听错一个字也认得出（「餐饮」→「餐引」）。
        ///
        /// 这条守的是一条**极容易漏掉的路径**：普通说法「分类餐引」是在
        /// <see cref="DraftResolver"/> 那一步才做匹配的，而「类型X」得先在这里判断
        /// X 算不算一个分类名——这一步若还是严格全等，「类型餐引」当场就被判成
        /// 没听懂，**根本走不到 <see cref="DraftResolver"/>**，
        /// 分类的模糊匹配在这条路上整个失效。
        /// </summary>
        [Test]
        public void Parse_TypeLabelWithTypo_FillsCategoryName()
        {
            Assert.IsTrue(_parse("记一笔，类型餐引，金额30", out TransactionDraft oDraft));

            // 错字**原样**传下去，不在这里纠正：这一层只回答「这算不算一个分类名」，
            // 换成库里哪个 Id 是 DraftResolver 的事
            Assert.AreEqual("餐引", oDraft.CategoryName);
            Assert.IsFalse(oDraft.Type.HasValue, "「餐引」不在账单类型词表里，不该被认成类型");
            Assert.IsEmpty(oDraft.Unrecognized, "认出来了就不该再报「没认出」");
            Assert.AreEqual(Money.FromYuan(30).Cents, oDraft.AmountCents);
        }

        // ── C. 金额 ──────────────────────────────────

        [Test]
        public void Parse_RepeatedAmount_LastOneWins()
        {
            // 人在自我纠正：「金额30……不对，金额50」
            Assert.IsTrue(_parse("金额30，金额50", out TransactionDraft oDraft));

            Assert.AreEqual(Money.FromYuan(50).Cents, oDraft.AmountCents);
        }

        [Test]
        public void Parse_InvalidSecondAmount_KeepsTheFirstValidOne()
        {
            // 「末次为准」指的是末次的**合法**值。非法值被拒绝时不该把
            // 前一次已经解析好的金额清掉——那等于用户纠正到一半，钱没了
            Assert.IsTrue(_parse("金额50，金额abc", out TransactionDraft oDraft));

            Assert.AreEqual(Money.FromYuan(50).Cents, oDraft.AmountCents);
        }

        [Test]
        public void Parse_NonPositiveAmount_IsRejected()
        {
            Assert.IsFalse(_parse("金额0", out _));
            Assert.IsFalse(_parse("金额-5", out _), "MoneyParser 允许负号，这层得自己拦");
        }

        [Test]
        public void Parse_AmountWithTwoDecimals_Parses()
        {
            Assert.IsTrue(_parse("金额30.5", out TransactionDraft oDraft));

            Assert.AreEqual(Money.FromYuan(30.5m).Cents, oDraft.AmountCents);
        }

        [Test]
        public void Parse_MalformedAmount_IsRejected()
        {
            Assert.IsFalse(_parse("金额30.555", out _), "凑不出整分，属于误输入");
            Assert.IsFalse(_parse("金额abc", out _));
            Assert.IsFalse(_parse("金额", out _), "标签后面什么都没有");
        }

        [Test]
        public void Parse_ValueWithConnector_StripsIt()
        {
            // 「金额是30」的值是「是30」。那个「是」是连接词不是内容——
            // 不剥掉的话 MoneyParser 会拒绝它，用户明明说对了却填不上
            Assert.IsTrue(_parse("金额是30", out TransactionDraft oDraft));

            Assert.AreEqual(Money.FromYuan(30).Cents, oDraft.AmountCents);
        }

        // ── D. 匹配优先级 ────────────────────────────

        [Test]
        public void Parse_EveryAmountSynonym_Works()
        {
            foreach (string sWord in new[] { "金额", "多少钱", "钱" })
            {
                Assert.IsTrue(_parse($"{sWord}50", out TransactionDraft oDraft),
                    $"「{sWord}」应能当金额标签");
                Assert.AreEqual(Money.FromYuan(50).Cents, oDraft.AmountCents,
                    $"「{sWord}」的金额解析错了");
            }
        }

        [Test]
        public void Parse_LabelWordBeatsCategoryName()
        {
            // 用户建了个叫「备注」的分类。标签词必须先匹配上——
            // 反过来的话「备注哈哈哈」会被切成一个分类，值变成「哈哈哈」，
            // 看着也能用，只是填错了字段，而且界面上完全看不出来
            List<string> lCategories = new List<string> { "备注", "餐饮" };

            Assert.IsTrue(_parse("备注哈哈哈", lCategories, out TransactionDraft oDraft));

            Assert.AreEqual("哈哈哈", oDraft.Note);
            Assert.AreEqual(string.Empty, oDraft.CategoryName);
        }

        [Test]
        public void Parse_TriggerWordInsideNote_IsNotStripped()
        {
            // 「记账」夹在备注中间时是内容的一部分。无条件剔除的话备注会变成
            // 「这个月真麻烦」——少了两个字，用户对着屏幕也看不出哪里不对
            Assert.IsTrue(_parse("备注 这个月记账真麻烦", out TransactionDraft oDraft));

            Assert.AreEqual("这个月记账真麻烦", oDraft.Note);
        }

        // ── E. 兜底 ──────────────────────────────────

        [Test]
        public void Parse_OnlyTriggerWord_ReturnsFalse()
        {
            // 返回一份全空的草稿会让调用方把用户已经选好的账户、分类、日期
            // 全冲掉，而用户只是说了句没听懂的话
            Assert.IsFalse(_parse("记一笔", out _));
        }

        [Test]
        public void Parse_TextWithNoLabelAtAll_ReturnsFalse()
        {
            Assert.IsFalse(_parse("今天天气不错", out _));
        }

        [Test]
        public void Parse_UnknownCategoryName_IsKeptAsName()
        {
            // 解析器不认分类表，它的活儿到「这是个名字」为止；
            // 名字能不能对上库里的分类，是 DraftResolver 的事
            Assert.IsTrue(_parse("金额30，分类不存在的东西", out TransactionDraft oDraft));

            Assert.AreEqual("不存在的东西", oDraft.CategoryName);
        }

        [Test]
        public void Parse_NoteContainingLabelWord_KeepsTheNoteIntact()
        {
            // 备注是自由文本，里面出现标签词是常态。切分规则要可预测：
            // 每个标签的值一直取到下一个标签为止
            Assert.IsTrue(_parse("备注 这东西金额很大，金额30", out TransactionDraft oDraft));

            Assert.AreEqual("这东西", oDraft.Note);
            Assert.AreEqual(Money.FromYuan(30).Cents, oDraft.AmountCents);
        }

        [Test]
        public void Parse_NoteWithInnerPunctuation_KeepsIt()
        {
            // 清理只该动首尾。备注里写「哈哈哈，真好笑」时那个逗号是内容，
            // 一并去掉就成了另一句话
            Assert.IsTrue(_parse("备注 哈哈哈，真好笑", out TransactionDraft oDraft));

            Assert.AreEqual("哈哈哈，真好笑", oDraft.Note);
        }

        [Test]
        public void Parse_NullCategoryList_DoesNotThrow()
        {
            // 库还没建好时调用方可能传 null。不能抛——
            // 一个语音输入把 App 崩掉，比它不工作严重得多
            Assert.IsTrue(TransactionPhraseParser.TryParse(
                "金额30", null, out TransactionDraft oDraft));
            Assert.AreEqual(Money.FromYuan(30).Cents, oDraft.AmountCents);
        }

        [Test]
        public void Parse_DateWord_SetsDayIndex()
        {
            Assert.IsTrue(_parse("金额30，日期昨天", out TransactionDraft oYesterday));
            Assert.AreEqual(1, oYesterday.DateIndex);

            Assert.IsTrue(_parse("金额30，日期前天", out TransactionDraft oDayBefore));
            Assert.AreEqual(2, oDayBefore.DateIndex);
        }

        [Test]
        public void Parse_UnparsableDateWord_LeavesDateUnset()
        {
            // 「3月5号」表单根本存不下（记账页只有今天/昨天/前天三个选项），
            // 所以这里只能落到「没说」，由表单的默认值接手
            Assert.IsTrue(_parse("金额30，日期3月5号", out TransactionDraft oDraft));
            Assert.IsFalse(oDraft.DateIndex.HasValue);
        }

        // ── 辅助 ────────────────────────────────────

        private static bool _parse(string sText, out TransactionDraft oDraft)
        {
            return TransactionPhraseParser.TryParse(sText, CATEGORIES, out oDraft);
        }

        private static bool _parse(
            string sText, List<string> lCategories, out TransactionDraft oDraft)
        {
            return TransactionPhraseParser.TryParse(sText, lCategories, out oDraft);
        }
    }
}
