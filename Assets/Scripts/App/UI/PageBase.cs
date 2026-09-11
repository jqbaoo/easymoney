using UnityEngine;

namespace EasyMoney.App.UI
{
    /// <summary>
    /// 页面基类。Root 铺满内容区（顶部标题栏与底部标签栏之间），子类在 Root 下搭界面。
    /// 不用 MonoBehaviour——界面全部由代码构建，不需要挂在 GameObject 上。
    /// </summary>
    public abstract class PageBase
    {
        private RectTransform m_Root;

        public abstract string Title { get; }

        public RectTransform Root => m_Root;

        /// <summary>由 AppRoot 在创建时调用，触发一次性的界面构建。</summary>
        public void Attach(RectTransform oRoot)
        {
            m_Root = oRoot;
            _build();
        }

        /// <summary>页面被切换到时调用，子类在这里刷新数据。</summary>
        public virtual void OnShow()
        {
        }

        protected abstract void _build();
    }
}
