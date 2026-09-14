using System;
using System.Collections.Generic;
using UnityEngine;

namespace EasyMoney.App.UI
{
    /// <summary>页面路由。同一时刻只显示一个页面，切换时调用 OnShow 让页面刷新数据。</summary>
    public sealed class PageRouter
    {
        private readonly Dictionary<string, PageBase> m_Pages = new Dictionary<string, PageBase>();
        private readonly List<string> m_Order = new List<string>();
        private string m_CurrentKey;

        public event Action<string> PageChanged;

        public string CurrentKey => m_CurrentKey;

        /// <summary>
        /// 当前显示的那一页。在 PageChanged 回调里读到的已经是新页——
        /// Show 先改 m_CurrentKey 再发通知，标题栏靠这个顺序取到正确的页面。
        /// </summary>
        public PageBase Current =>
            m_CurrentKey != null && m_Pages.TryGetValue(m_CurrentKey, out PageBase oPage)
                ? oPage
                : null;

        public void Register(string sKey, PageBase oPage)
        {
            if (!m_Pages.ContainsKey(sKey))
            {
                m_Order.Add(sKey);
            }

            m_Pages[sKey] = oPage;
        }

        public void Show(string sKey)
        {
            if (!m_Pages.TryGetValue(sKey, out PageBase oTarget))
            {
                Debug.LogError($"未注册的页面: {sKey}");
                return;
            }

            foreach (string sEachKey in m_Order)
            {
                m_Pages[sEachKey].Root.gameObject.SetActive(sEachKey == sKey);
            }

            m_CurrentKey = sKey;
            oTarget.OnShow();
            PageChanged?.Invoke(sKey);
        }

        /// <summary>把当前页面重新走一遍 OnShow，用于数据变化后刷新。</summary>
        public void RefreshCurrent()
        {
            if (m_CurrentKey != null && m_Pages.TryGetValue(m_CurrentKey, out PageBase oPage))
            {
                oPage.OnShow();
            }
        }
    }
}
