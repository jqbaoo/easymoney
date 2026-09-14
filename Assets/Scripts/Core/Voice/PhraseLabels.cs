using System.Collections.Generic;

namespace EasyMoney.Core
{
    /// <summary>一句话里能带的字段。值最终落到 <see cref="TransactionDraft"/> 的对应成员上。</summary>
    public enum PhraseField
    {
        Amount,
        Category,
        Type,
        Account,
        ToAccount,
        Date,
        Note
    }

    /// <summary>
    /// 一句话记账的标签词表。
    ///
    /// **宁窄勿宽。** 每多收一个词，误切分的风险就高一截——备注是自由文本，
    /// 什么词都可能出现。「花了」「付了」这类动词看着自然，但用户备注里说一句
    /// 「这月花了不少」，就会被切出一个假金额。所以只收明确到不会被误认的词。
    ///
    /// 这张表是**语法词汇**，不是用户数据。用户自建的分类（「宠物医疗」）不写在这里，
    /// 分类名表由调用方从数据库取、随句子传进来，见
    /// <see cref="TransactionPhraseParser.TryParse"/>。
    /// 所以用户在账户页新建一个分类，立刻就能说出口，不用改代码。
    /// </summary>
    public static class PhraseLabels
    {
        // ── 标签词 ──────────────────────────────────

        public static readonly string[] AMOUNT = { "金额", "多少钱", "钱" };

        /// <summary>不含「类型」——那是个共享标签，靠值域消歧，见 <see cref="TYPE"/>。</summary>
        public static readonly string[] CATEGORY = { "分类", "类别", "科目" };

        /// <summary>
        /// 「类型」后面的值既可能是账单类型也可能是分类（「类型收入」对「类型餐饮」），
        /// 所以它单独算一个字段，由解析器看值落在哪张表里再分派——
        /// 这样用户不用改口头习惯去区分「类型」和「分类」。
        /// </summary>
        public static readonly string[] TYPE = { "类型" };

        /// <summary>不收「卡」这类单字：备注里出现「卡」的概率远高于用户拿它当账户标签。</summary>
        public static readonly string[] ACCOUNT = { "账户", "账号" };

        public static readonly string[] TO_ACCOUNT = { "转入", "转到" };

        public static readonly string[] DATE = { "日期", "哪天" };

        public static readonly string[] NOTE = { "备注", "说明" };

        // ── 触发词 ──────────────────────────────────

        /// <summary>
        /// 意图词，不携带值，出现即剔除。
        ///
        /// 只有**独占一个片段**的才剔除（前后是标点或句首句尾）：备注里说
        /// 「这个月记账真麻烦」时，「记账」是内容的一部分，吃掉它就把备注改坏了。
        /// </summary>
        public static readonly string[] TRIGGERS = { "记一笔", "记账", "记一下" };

        // ── 值域词表 ────────────────────────────────

        /// <summary>
        /// 「类型」标签的值 → 账单类型。
        ///
        /// 显式写出映射而不是拿数组下标当枚举值：枚举数值改了不会有任何报错，
        /// 只会让「收入」静默变成「转账」。
        /// </summary>
        public static readonly (string Word, TxType Type)[] TYPE_VALUES =
        {
            ("支出", TxType.Expense),
            ("收入", TxType.Income),
            ("转账", TxType.Transfer)
        };

        /// <summary>
        /// 日期词表。**下标就是 <see cref="TransactionDraft.DateIndex"/> 的值**，
        /// 必须与记账页那三个日期选项（今天 / 昨天 / 前天）同序——
        /// 两者错位的话，说「昨天」会记成「前天」，而且没有任何报错。
        /// </summary>
        public static readonly string[] DATE_VALUES = { "今天", "昨天", "前天" };

        // ── 全表 ────────────────────────────────────

        /// <summary>
        /// 全部标签词，**按长度倒序**。
        ///
        /// 倒序是为了「最长优先」：表里若同时有「金额」和「金额多少」，
        /// 短的抢先匹配就会把值切成「多少50」。按这个顺序扫描，
        /// 以后往上面几组里加词时，长的那个永远先匹配，已有的词不会被新词弄坏。
        /// PhraseLabelsTests 里有一条用例钉着这个顺序。
        /// </summary>
        public static readonly KeyValuePair<PhraseField, string>[] ALL;

        static PhraseLabels()
        {
            // 放在静态构造函数里而不是字段初始化器：后者按声明顺序执行，
            // ALL 写在上面几组之前就会读到还没填好的空数组
            ALL = _buildAll();
        }

        private static KeyValuePair<PhraseField, string>[] _buildAll()
        {
            List<KeyValuePair<PhraseField, string>> lAll =
                new List<KeyValuePair<PhraseField, string>>();

            _append(lAll, PhraseField.Amount, AMOUNT);
            _append(lAll, PhraseField.Category, CATEGORY);
            _append(lAll, PhraseField.Type, TYPE);
            _append(lAll, PhraseField.Account, ACCOUNT);
            _append(lAll, PhraseField.ToAccount, TO_ACCOUNT);
            _append(lAll, PhraseField.Date, DATE);
            _append(lAll, PhraseField.Note, NOTE);

            // 长度相同时按词本身排，保证顺序确定——List.Sort 不稳定，
            // 而顺序会直接影响解析结果，不能随运行时飘
            lAll.Sort((oLeft, oRight) =>
            {
                int iByLength = oRight.Value.Length.CompareTo(oLeft.Value.Length);
                return iByLength != 0 ? iByLength : string.CompareOrdinal(oLeft.Value, oRight.Value);
            });

            return lAll.ToArray();
        }

        private static void _append(
            List<KeyValuePair<PhraseField, string>> lAll, PhraseField eField, string[] lWords)
        {
            foreach (string sWord in lWords)
            {
                lAll.Add(new KeyValuePair<PhraseField, string>(eField, sWord));
            }
        }
    }
}
