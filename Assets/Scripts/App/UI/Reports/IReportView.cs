using System.Collections.Generic;
using EasyMoney.Core;
using UnityEngine;

namespace EasyMoney.App.UI.Reports
{
    /// <summary>
    /// 报表主体的一种画法。条形、环形各一份实现，以后再加别的形态就是再写一份、
    /// 去 <see cref="ReportViews.ALL"/> 里挂个号——页面和下拉列表都不用动。
    ///
    /// 做成接口而不是在页面里 switch，理由与 MonthBar 一样：页面里的分支
    /// EditMode 测试够不着，做成一个能被 <c>new</c> 出来的东西，节点名就是它的形状，
    /// 于是「环形图那几行到底建出来没有」也能被断言。
    /// </summary>
    public interface IReportView
    {
        /// <summary>这一份实现画的是哪种视图。</summary>
        ReportViewMode Mode { get; }

        /// <summary>
        /// 在 <paramref name="oContent"/> 下建出这一版的全部节点。
        ///
        /// 调用方（<see cref="ReportViewHost"/>）保证进来时 oContent 是空的，
        /// 所以实现里不用自己清场——清场只有一处，才不会漏。
        /// </summary>
        /// <param name="oType">当前看的是支出还是收入。定配色用，不参与取数。</param>
        void Render(RectTransform oContent, IList<CategoryBreakdownItem> lItems, TxType oType);
    }
}
