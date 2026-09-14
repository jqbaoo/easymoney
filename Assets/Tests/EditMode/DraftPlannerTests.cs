using System.Collections.Generic;
using EasyMoney.Core;
using NUnit.Framework;

namespace EasyMoney.Tests
{
    /// <summary>
    /// 草稿 → 表单计划 的规则测试。
    ///
    /// 盯住三件事：**没说到的字段一律不动**（语音只补它说到的那部分，剩下的留给
    /// 用户之前的手选）、**分类按这一句自己说的类型找**（「类型收入，分类工资」
    /// 是句完整的话，不该被表单默认的「支出」搅黄）、**说了名字却没找到时不算「没提」**
    /// （表单据此留空，而不是自动补个默认值——补了用户会以为语音已经填对，直接保存）。
    /// </summary>
    public class DraftPlannerTests
    {
        // ── 不动的那些 ──────────────────────────────

        [Test]
        public void Build_EmptyDraft_ChangesNothing()
        {
            DraftPlan oPlan = _build(new TransactionDraft(), TxType.Expense);

            Assert.IsFalse(oPlan.HasAnyField);
            Assert.IsNull(oPlan.Type);
            Assert.IsNull(oPlan.AmountCents);
            Assert.IsNull(oPlan.Category);
            Assert.IsNull(oPlan.Account);
            Assert.IsNull(oPlan.ToAccount);
            Assert.IsNull(oPlan.DateIndex);
            Assert.IsNull(oPlan.Note);
            Assert.IsEmpty(oPlan.Unhandled);
        }

        [Test]
        public void Build_NullDraft_ReturnsEmptyPlan()
        {
            Assert.IsFalse(_build(null, TxType.Expense).HasAnyField);
        }

        /// <summary>
        /// 这一句没提类型时 Plan.Type 必须是 null。
        ///
        /// 这里最容易顺手写成「一路赋值成 eCurrentType」——最终界面上看着一模一样，
        /// 但表单会以为自己该切一次类型，而切类型会顺手清掉分类与转入，
        /// 用户刚才选好的那两项就没了。
        /// </summary>
        [Test]
        public void Build_SilentType_LeavesFormTypeAlone()
        {
            DraftPlan oPlan = _build(
                new TransactionDraft { AmountCents = Money.FromYuan(30).Cents }, TxType.Income);

            Assert.IsNull(oPlan.Type);
        }

        [Test]
        public void Build_EmptyNote_LeavesNoteUnset()
        {
            // 空串是「没提」，不是「把备注清空」——用户可能是先手打了备注再说的金额
            Assert.IsNull(_build(new TransactionDraft { DateIndex = 0 }, TxType.Expense).Note);
        }

        // ── 带过来的那些 ────────────────────────────

        [Test]
        public void Build_TypeInTheSentence_IsCarriedOver()
        {
            DraftPlan oPlan = _build(
                new TransactionDraft { Type = TxType.Transfer }, TxType.Expense);

            Assert.AreEqual(TxType.Transfer, oPlan.Type);
        }

        [Test]
        public void Build_AmountDateAndNote_AreCarriedOver()
        {
            TransactionDraft oDraft = new TransactionDraft
            {
                AmountCents = Money.FromYuan(30).Cents,
                DateIndex = 1,
                Note = "哈哈哈"
            };

            DraftPlan oPlan = _build(oDraft, TxType.Expense);

            Assert.AreEqual(Money.FromYuan(30).Cents, oPlan.AmountCents);
            Assert.AreEqual(1, oPlan.DateIndex);
            Assert.AreEqual("哈哈哈", oPlan.Note);
        }

        // ── 分类：按哪个方向找 ──────────────────────

        /// <summary>
        /// 「类型收入，分类工资」是一句话里自带的方向，找「工资」得按收入找。
        /// 按表单此刻的「支出」去找的话，用户明明一句话说全了，却被告知分类没听懂。
        /// </summary>
        [Test]
        public void Build_CategoryFollowsTheTypeInTheSameSentence()
        {
            TransactionDraft oDraft = new TransactionDraft
            {
                Type = TxType.Income,
                CategoryName = "工资"
            };

            DraftPlan oPlan = _build(oDraft, TxType.Expense);

            Assert.AreEqual(12, oPlan.Category?.Id);
            Assert.IsEmpty(oPlan.Unhandled);
        }

        [Test]
        public void Build_CategoryFollowsFormTypeWhenTheSentenceIsSilent()
        {
            // 这一句没说类型，就按表单上此刻选着的方向找
            DraftPlan oPlan = _build(
                new TransactionDraft { CategoryName = "工资" }, TxType.Income);

            Assert.AreEqual(12, oPlan.Category?.Id);
        }

        /// <summary>
        /// 方向对不上时留空并报出来。硬塞进去的话要等到保存那一刻才报错，
        /// 而用户会以为是别的地方出了问题。
        /// </summary>
        [Test]
        public void Build_CategoryOfTheOtherKind_IsReported()
        {
            DraftPlan oPlan = _build(
                new TransactionDraft { CategoryName = "工资" }, TxType.Expense);

            Assert.IsNull(oPlan.Category);
            Assert.AreEqual(new List<string> { "工资" }, oPlan.Unhandled);
        }

        [Test]
        public void Build_UnknownCategory_IsReportedAndStillMarksMentioned()
        {
            DraftPlan oPlan = _build(
                new TransactionDraft { CategoryName = "不存在的东西" }, TxType.Expense);

            Assert.IsNull(oPlan.Category);
            Assert.IsTrue(oPlan.MentionsCategory, "说了分类名就算提过，哪怕库里没有");
            Assert.AreEqual(new List<string> { "不存在的东西" }, oPlan.Unhandled);
        }

        // ── 转账的两个方向 ──────────────────────────

        /// <summary>
        /// 转账没有分类。这一句里多说的那个分类不落地，**也不报**——
        /// 转账时界面上根本没有分类这一行，提示「餐饮没处理」只会让人困惑。
        /// </summary>
        [Test]
        public void Build_TransferDropsTheCategory()
        {
            TransactionDraft oDraft = new TransactionDraft
            {
                Type = TxType.Transfer,
                CategoryName = "餐饮"
            };

            DraftPlan oPlan = _build(oDraft, TxType.Expense);

            Assert.IsNull(oPlan.Category);
            Assert.IsEmpty(oPlan.Unhandled);
        }

        [Test]
        public void Build_TransferKeepsTheToAccount()
        {
            TransactionDraft oDraft = new TransactionDraft
            {
                Type = TxType.Transfer,
                ToAccountName = "微信"
            };

            DraftPlan oPlan = _build(oDraft, TxType.Expense);

            Assert.AreEqual(2, oPlan.ToAccount?.Id);
        }

        [Test]
        public void Build_NonTransferDropsTheToAccount()
        {
            // 「转入」只在转账时有意义，别的类型下填了也看不见
            DraftPlan oPlan = _build(
                new TransactionDraft { ToAccountName = "微信" }, TxType.Expense);

            Assert.IsNull(oPlan.ToAccount);
        }

        // ── 账户 ────────────────────────────────────

        [Test]
        public void Build_AccountFound_ReturnsAccount()
        {
            DraftPlan oPlan = _build(
                new TransactionDraft { AccountName = "现金" }, TxType.Expense);

            Assert.AreEqual(1, oPlan.Account?.Id);
        }

        /// <summary>
        /// 「提过」和「找到了」是两件事：报出来是给用户看，
        /// <c>MentionsAccount</c> 是让表单别自动补一个默认账户——
        /// 补了他会以为语音填对了，直接保存就记到别的账户上了。
        /// </summary>
        [Test]
        public void Build_UnknownAccount_IsReportedButStillCountsAsMentioned()
        {
            DraftPlan oPlan = _build(
                new TransactionDraft { AccountName = "张三" }, TxType.Expense);

            Assert.IsNull(oPlan.Account);
            Assert.IsTrue(oPlan.MentionsAccount);
            Assert.AreEqual(new List<string> { "张三" }, oPlan.Unhandled);
        }

        [Test]
        public void Build_SilentAccount_IsNotMentioned()
        {
            // 没提过才该走「补默认值」那条路
            Assert.IsFalse(_build(
                new TransactionDraft { AmountCents = 100 }, TxType.Expense).MentionsAccount);
        }

        // ── 散字 ────────────────────────────────────

        [Test]
        public void Build_LooseText_IsReported()
        {
            // 没标签的散字一个字都没用上，而屏幕上别处都填好了——
            // 不说的话用户会以为整句都听懂了
            TransactionDraft oDraft = new TransactionDraft { AmountCents = 100 };
            oDraft.Unrecognized.Add("随便说说");

            DraftPlan oPlan = _build(oDraft, TxType.Expense);

            Assert.AreEqual(new List<string> { "随便说说" }, oPlan.Unhandled);
        }

        // ── 辅助 ────────────────────────────────────

        private static DraftPlan _build(TransactionDraft oDraft, TxType eCurrentType)
        {
            return DraftPlanner.Build(oDraft, eCurrentType, _accounts(), _categories());
        }

        private static List<Account> _accounts()
        {
            return new List<Account>
            {
                new Account { Id = 1, Name = "现金" },
                new Account { Id = 2, Name = "微信" }
            };
        }

        private static List<Category> _categories()
        {
            return new List<Category>
            {
                new Category { Id = 11, Name = "餐饮", Kind = CategoryKind.Expense },
                new Category { Id = 12, Name = "工资", Kind = CategoryKind.Income }
            };
        }
    }
}
