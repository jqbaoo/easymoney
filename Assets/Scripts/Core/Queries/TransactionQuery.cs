namespace EasyMoney.Core
{
    /// <summary>
    /// 账单筛选条件。所有字段为 null / 空 表示该维度不筛选。
    /// 时间区间为左闭右开 [StartMs, EndMs)。
    /// </summary>
    public class TransactionQuery
    {
        public long? StartMs { get; set; }

        public long? EndMs { get; set; }

        public TxType? Type { get; set; }

        public int? CategoryId { get; set; }

        public int? AccountId { get; set; }

        /// <summary>
        /// 账户筛选是否把「转入该账户」的记录也算进来。
        /// 查账户流水时用 true；统计该账户自身的收支时用 false——
        /// 否则转账会在转出方和转入方各被算一次。
        /// </summary>
        public bool AccountIncludesTransfers { get; set; }

        public long? MinCents { get; set; }

        public long? MaxCents { get; set; }

        /// <summary>备注模糊匹配，大小写不敏感，% 和 _ 按普通字符处理。</summary>
        public string Keyword { get; set; }

        public int Limit { get; set; } = 50;

        public int Offset { get; set; }
    }
}
