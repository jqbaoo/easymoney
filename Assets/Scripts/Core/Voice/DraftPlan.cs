using System.Collections.Generic;

namespace EasyMoney.Core
{
    /// <summary>
    /// 一句语音最终要在表单上落成什么。
    ///
    /// 里面**只放已经能直接写进控件的东西**——名字在这一步之前已经换成了对象，
    /// 页面拿到它只剩「有值就写上去」，没有判断可写错。真正的规则（怎么匹配、
    /// 匹配不上怎么办、类型变了要不要连坐）都在 <see cref="DraftPlanner"/> 里，
    /// 那边编辑模式测试够得着。
    ///
    /// 每个字段为 null 都是**「这一句没提它，别动表单上的现值」**，而不是「清空」：
    /// 语音补的是用户嘴里说到的那部分，没说到的得留着——他可能刚才已经手选过了。
    /// </summary>
    public sealed class DraftPlan
    {
        public TxType? Type { get; set; }

        public long? AmountCents { get; set; }

        public Account Account { get; set; }

        public Account ToAccount { get; set; }

        public Category Category { get; set; }

        public int? DateIndex { get; set; }

        /// <summary>备注。null 表示这一句压根没提备注。</summary>
        public string Note { get; set; }

        /// <summary>
        /// 这一句提没提账户。**提了但没找到也算提过。**
        ///
        /// 表单靠它决定要不要给账户补默认值：说了「账户张三」而库里没有张三时，
        /// 这一项就该空着让用户自己选。自动补一个默认账户的话，用户会以为语音
        /// 已经填好了，直接点保存——那就记到别的账户上了，是一笔真账。
        /// </summary>
        public bool MentionsAccount { get; set; }

        /// <summary>这一句提没提分类，语义同 <see cref="MentionsAccount"/>。</summary>
        public bool MentionsCategory { get; set; }

        /// <summary>
        /// 听出来但没落地的名字，按听到的先后：库里没有的账户与分类，
        /// 以及完全没标签的散字。
        ///
        /// 两类合成一个列表，是因为**界面上只有一行提示位**：对用户来说
        /// 「张三没找到」和「随便说说没听懂」是同一件事——你说的这几个字我没用上。
        /// </summary>
        public List<string> Unhandled { get; } = new List<string>();

        /// <summary>这一句到底改动了表单上的什么。全 false 时用户等于白说了一句。</summary>
        public bool HasAnyField =>
            Type.HasValue || AmountCents.HasValue || DateIndex.HasValue || Note != null ||
            Account != null || ToAccount != null || Category != null;
    }
}
