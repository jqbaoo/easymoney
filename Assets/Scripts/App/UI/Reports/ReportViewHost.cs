using System.Collections.Generic;
using EasyMoney.Core;
using UnityEngine;

namespace EasyMoney.App.UI.Reports
{
    /// <summary>
    /// 报表主体：按当前视图把内容区画出来，换视图时先把上一个视图的节点清干净。
    ///
    /// 这一小撮逻辑单独成类，是因为它原本该写在 ReportPage 里——而页面跑不到
    /// EditMode 测试。「切了视图之后旧的还留在下面」这种毛病不会报错、只会叠着画，
    /// 留在页面里就只能靠肉眼发现。收成组件之后，节点名就是它的形状。
    /// </summary>
    public sealed class ReportViewHost
    {
        private readonly RectTransform m_Content;
        private readonly Dictionary<ReportViewMode, IReportView> m_Views =
            new Dictionary<ReportViewMode, IReportView>();

        /// <summary>当前视图。构造时取 <see cref="ReportViews.Default"/>。</summary>
        public ReportViewMode Mode { get; private set; }

        /// <summary>内容区，视图都建在它下面。测试与需要额外塞东西的调用方会用到。</summary>
        public RectTransform Content => m_Content;

        public ReportViewHost(RectTransform oContent, params IReportView[] lViews)
        {
            m_Content = oContent;
            Mode = ReportViews.Default;

            if (lViews == null)
            {
                return;
            }

            foreach (IReportView oView in lViews)
            {
                if (oView != null)
                {
                    m_Views[oView.Mode] = oView;
                }
            }
        }

        /// <summary>
        /// 这个模式有没有挂上实现。文件在 <see cref="ReportViews.ALL"/> 里加了一项、
        /// 却忘了写对应的视图时，靠它兜住——否则下拉里会多出一个点了没反应的选项。
        /// </summary>
        public bool Has(ReportViewMode oMode)
        {
            return m_Views.ContainsKey(oMode);
        }

        /// <summary>换视图。返回是否真的换了——没换就不必让页面重刷一遍。</summary>
        public bool SetMode(ReportViewMode oMode)
        {
            if (oMode == Mode || !Has(oMode))
            {
                return false;
            }

            Mode = oMode;
            return true;
        }

        /// <summary>按当前视图重画内容区。</summary>
        public void Render(IList<CategoryBreakdownItem> lItems, TxType oType)
        {
            // 先清后建，不留上一次的节点：视图之间不共用节点，
            // 留着的话新旧两批会同时参与布局，看着就是「两个视图叠在一起」
            UiFactory.ClearChildren(m_Content);

            if (m_Views.TryGetValue(Mode, out IReportView oView))
            {
                oView.Render(m_Content, lItems, oType);
            }
        }
    }
}
