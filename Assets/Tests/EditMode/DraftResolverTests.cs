using System.Collections.Generic;
using EasyMoney.Core;
using NUnit.Framework;

namespace EasyMoney.Tests
{
    /// <summary>
    /// 草稿里的名字 → 数据库 Id 的匹配测试。
    ///
    /// 盯住三件事：**匹配不上返回 0**（而不是抛异常或硬猜一个）、
    /// **分类要按收支方向过滤**——语音说了个支出分类而类型是收入时，
    /// 让它落空比让它填错好：填错了要等到保存才报错，用户会以为问题出在别处、
    /// 以及**模糊匹配只补有把握的**：ASR 听错一个字要能救回来，但两个候选一样近、
    /// 或者长度差得离谱时一律留空——静默填错比留空严重得多。
    ///
    /// ⚠️ 上面那批老用例（「张三」「不存在的东西」「工资对支出」）**在加了模糊匹配之后
    /// 依然全绿**，因为它们离任何候选都差着好几个字。所以它们守不住模糊匹配的边界，
    /// 边界是下面 `── 模糊 ──` 那两节在守。
    /// </summary>
    public class DraftResolverTests
    {
        // ── 账户 ────────────────────────────────────

        [Test]
        public void MatchAccountId_Found_ReturnsId()
        {
            Assert.AreEqual(2, DraftResolver.MatchAccountId("微信", _accounts()));
        }

        [Test]
        public void MatchAccountId_UnknownName_ReturnsZero()
        {
            // 「张三」不是资金账户。返回 0 让表单停在「请选择」，用户看得见
            Assert.AreEqual(0, DraftResolver.MatchAccountId("张三", _accounts()));
        }

        [Test]
        public void MatchAccountId_EmptyName_ReturnsZero()
        {
            Assert.AreEqual(0, DraftResolver.MatchAccountId(string.Empty, _accounts()));
            Assert.AreEqual(0, DraftResolver.MatchAccountId(null, _accounts()));
            Assert.AreEqual(0, DraftResolver.MatchAccountId("   ", _accounts()));
        }

        [Test]
        public void MatchAccountId_TrimsSurroundingSpace()
        {
            Assert.AreEqual(1, DraftResolver.MatchAccountId("  现金 ", _accounts()));
        }

        [Test]
        public void MatchAccountId_NullList_ReturnsZero()
        {
            Assert.AreEqual(0, DraftResolver.MatchAccountId("现金", null));
        }

        // ── 分类 ────────────────────────────────────

        [Test]
        public void MatchCategoryId_Found_ReturnsId()
        {
            Assert.AreEqual(11,
                DraftResolver.MatchCategoryId("餐饮", _categories(), TxType.Expense));
        }

        [Test]
        public void MatchCategoryId_WrongKind_ReturnsZero()
        {
            // 「工资」是收入分类。类型是支出时不认它——宁可留空，
            // 也不能把一个收入分类记到支出上
            Assert.AreEqual(0,
                DraftResolver.MatchCategoryId("工资", _categories(), TxType.Expense));

            Assert.AreEqual(12,
                DraftResolver.MatchCategoryId("工资", _categories(), TxType.Income));
        }

        [Test]
        public void MatchCategoryId_UnknownName_ReturnsZero()
        {
            Assert.AreEqual(0,
                DraftResolver.MatchCategoryId("不存在的东西", _categories(), TxType.Expense));
        }

        [Test]
        public void MatchCategoryId_EmptyName_ReturnsZero()
        {
            Assert.AreEqual(0, DraftResolver.MatchCategoryId(string.Empty, _categories(), TxType.Expense));
            Assert.AreEqual(0, DraftResolver.MatchCategoryId(null, _categories(), TxType.Expense));
        }

        // ── 模糊：能救回来的 ────────────────────────

        /// <summary>ASR 的同音错字（「现金」→「现全」）应当匹配到正确的账户。</summary>
        [Test]
        public void MatchAccountId_OneCharTypo_ReturnsId()
        {
            Assert.AreEqual(1, DraftResolver.MatchAccountId("现全", _accounts()));
            Assert.AreEqual(3, DraftResolver.MatchAccountId("支付包", _accounts()));
        }

        /// <summary>
        /// 库里同时有精确命中的名字时，绝不能被更「像」的模糊候选抢走。
        ///
        /// 这条是**重名的回归保护**：库里存了两条同名记录时，加模糊匹配之前
        /// 取的是第一个；若把「唯一候选」规则套到精确命中上，就会变成
        /// 「并列所以不猜」——用户说了个库里确实有的名字反倒匹配不上。
        /// </summary>
        [Test]
        public void MatchAccountId_ExactBeatsFuzzy()
        {
            List<Account> lAccounts = new List<Account>
            {
                new Account { Id = 1, Name = "现金" },
                new Account { Id = 2, Name = "现金卡" },
                new Account { Id = 3, Name = "现金" }
            };

            // 精确命中「现金」，取第一个（Id 1），而不是「现金卡」
            Assert.AreEqual(1, DraftResolver.MatchAccountId("现金", lAccounts));
        }

        /// <summary>用户多说了一个字（「现金卡」）而库里只有「现金」时，认得出。</summary>
        [Test]
        public void MatchAccountId_SlightlyLongerName_ReturnsId()
        {
            Assert.AreEqual(1, DraftResolver.MatchAccountId("现金卡", _accounts()));
        }

        // ── 模糊：不能猜的那些 ──────────────────────

        /// <summary>
        /// **两个候选一样近，一个都不填。**
        ///
        /// 「现佥」离「现金」和「现钞」都是一步，没有任何依据说明是哪一个。
        /// 随便挑一个的话，账户行会出现一个用户没提过的名字，
        /// 而界面上完全看不出这是猜的——留空他一定看得见。
        /// </summary>
        [Test]
        public void MatchAccountId_TwoEquallyClose_ReturnsZero()
        {
            List<Account> lAccounts = new List<Account>
            {
                new Account { Id = 1, Name = "现金" },
                new Account { Id = 2, Name = "现钞" }
            };

            Assert.AreEqual(0, DraftResolver.MatchAccountId("现佥", lAccounts));
        }

        /// <summary>差得超过阈值不填：4 字名只许改一个字。</summary>
        [Test]
        public void MatchAccountId_OverThreshold_ReturnsZero()
        {
            // 「招尚很行」改了两个位置（尚/商、很/银），4 字名的额度只有一个。
            // ⚠️ 别拿「工商银行」当反例——它和「招商银行」只差一个字，会被认成同一个
            List<Account> lAccounts = new List<Account>
            {
                new Account { Id = 1, Name = "招商银行" }
            };

            Assert.AreEqual(0, DraftResolver.MatchAccountId("招尚很行", lAccounts));
        }

        /// <summary>
        /// **已知代价的显式记录。** 用户说的名字库里没有、而库里恰好只有一个
        /// 与之相差一个字的候选时，会静默填上那个候选。
        ///
        /// 「工作」和「工资」只差一个字，在编辑距离上与 ASR 的典型错法
        /// （「餐饮」→「餐引」）完全同形——**约束层面区分不了**，能区分的只有拼音。
        ///
        /// 收下它的理由：一是认错的结果写在表单上看得见，二是最后那一下保存
        /// 必须用户自己按。代价是所有 2 字名的救回能力都绑在这一条上——
        /// 把阈值收紧到 0 确实能消灭这类误配，但同时也让「张三」→「张散」
        /// 这类**主要收益**归零。属于「要么全救、要么全不救」，本次选了救。
        /// </summary>
        [Test]
        public void MatchAccountId_UnknownNameOneCharFromAKnownOne_FillsThatOne()
        {
            List<Account> lAccounts = new List<Account>
            {
                new Account { Id = 1, Name = "工资" }
            };

            Assert.AreEqual(1, DraftResolver.MatchAccountId("工作", lAccounts));
        }

        /// <summary>
        /// 模糊匹配**跟着方向走**：说支出分类时不会去认一个收入分类。
        ///
        /// 方向错了宁可留空——真填上了要等到保存才报错，用户会以为是别处的问题。
        /// </summary>
        [Test]
        public void MatchCategoryId_FuzzyRespectsKind()
        {
            Assert.AreEqual(11,
                DraftResolver.MatchCategoryId("餐引", _categories(), TxType.Expense));

            // 换成收入向，候选里只剩「工资」，而「餐引」离它差两个字
            Assert.AreEqual(0,
                DraftResolver.MatchCategoryId("餐引", _categories(), TxType.Income));
        }

        /// <summary>分类的精确命中同样优先于模糊候选。</summary>
        [Test]
        public void MatchCategoryId_ExactBeatsFuzzy()
        {
            List<Category> lCategories = new List<Category>
            {
                new Category { Id = 11, Name = "餐饮", Kind = CategoryKind.Expense },
                new Category { Id = 13, Name = "餐饮外卖", Kind = CategoryKind.Expense }
            };

            Assert.AreEqual(11,
                DraftResolver.MatchCategoryId("餐饮", lCategories, TxType.Expense));
        }

        // ── 方向换算 ────────────────────────────────

        [Test]
        public void CategoryKindFor_MapsIncomeAndEverythingElse()
        {
            Assert.AreEqual(CategoryKind.Income, DraftResolver.CategoryKindFor(TxType.Income));

            // 转账没有分类，落到支出方向只是给调用方一个确定值
            Assert.AreEqual(CategoryKind.Expense, DraftResolver.CategoryKindFor(TxType.Expense));
            Assert.AreEqual(CategoryKind.Expense, DraftResolver.CategoryKindFor(TxType.Transfer));
        }

        private static List<Account> _accounts()
        {
            return new List<Account>
            {
                new Account { Id = 1, Name = "现金" },
                new Account { Id = 2, Name = "微信" },
                new Account { Id = 3, Name = "支付宝" }
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
