using System.Collections.Generic;

namespace EasyMoney.Core
{
    /// <summary>
    /// 一句话解析出来的账单草稿。**只装名字，不装 Id**——
    /// 解析器在 Core 层，不认识数据库（依赖方向 Core ← Data ← App），
    /// 「餐饮」这个名字对应哪个分类，得等 <see cref="DraftResolver"/> 拿着分类表比对才知道。
    ///
    /// 金额与日期用可空类型，是为了区分「用户没说」和「说了个零值」：
    /// 金额没说该留给表单的默认值，说了 0 则该被拒绝。两者都存成 0 就分不开了。
    /// </summary>
    public sealed class TransactionDraft
    {
        /// <summary>金额，单位「分」。null = 用户没说。</summary>
        public long? AmountCents { get; set; }

        /// <summary>账单类型。null = 用户没说，交给表单的默认值（支出）。</summary>
        public TxType? Type { get; set; }

        public string CategoryName { get; set; } = string.Empty;

        public string AccountName { get; set; } = string.Empty;

        public string ToAccountName { get; set; } = string.Empty;

        /// <summary>日期偏移：0 = 今天，1 = 昨天，2 = 前天。与记账页的三个选项一一对应。</summary>
        public int? DateIndex { get; set; }

        public string Note { get; set; } = string.Empty;

        /// <summary>
        /// 没有标签认领、也没能归入任何字段的文字。收在这里而不是丢掉，
        /// 是为了能告诉用户「这几个字我没认出来」——静默吞掉的话，
        /// 用户看到金额填上了，不会发现「张三」不见了。
        /// </summary>
        public List<string> Unrecognized { get; } = new List<string>();

        /// <summary>
        /// 至少解析出了一个字段没有。
        ///
        /// 一个字段都没有时 <see cref="TransactionPhraseParser.TryParse"/> 返回 false：
        /// 拿一份全空的草稿去覆盖表单，会把用户已经选好的账户、分类、日期全冲掉，
        /// 而用户只是说了一句没听懂的话。
        /// </summary>
        public bool HasAnyField =>
            AmountCents.HasValue || Type.HasValue || DateIndex.HasValue ||
            CategoryName.Length > 0 || AccountName.Length > 0 ||
            ToAccountName.Length > 0 || Note.Length > 0;
    }
}
