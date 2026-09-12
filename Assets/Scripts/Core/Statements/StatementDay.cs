using System;
using System.Collections.Generic;

namespace EasyMoney.Core
{
    /// <summary>
    /// 账单列表里的一天：一个日期标题，底下挂着当天的明细行。
    /// </summary>
    public sealed class StatementDay
    {
        /// <summary>当天的本地日期，时间部分已归零。</summary>
        public DateTime Date { get; set; }

        /// <summary>日期标题，形如「9月11日 周五」。</summary>
        public string DateLabel { get; set; } = string.Empty;

        public List<StatementRow> Items { get; set; } = new List<StatementRow>();
    }
}
