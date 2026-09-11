using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EasyMoney.App.UI
{
    /// <summary>
    /// 底部标签栏。图标在上、文字在下，选中项用主色，未选中用弱化色。
    /// 图标资源缺失时只剩文字，版式自动居中，不会塌。
    /// </summary>
    public sealed class TabBar
    {
        private readonly List<string> m_Keys = new List<string>();
        private readonly List<string> m_IconNames = new List<string>();
        private readonly List<Button> m_Buttons = new List<Button>();
        private readonly List<Text> m_Labels = new List<Text>();
        private readonly List<Image> m_Icons = new List<Image>();

        public event Action<string> TabClicked;

        public void Build(RectTransform oRoot, params (string Key, string Icon, string Label)[] lTabs)
        {
            Image oBackground = UiFactory.CreatePanel(oRoot, "TabBar", Theme.SURFACE);
            UiFactory.AnchorBottom(oBackground.rectTransform, Theme.TABBAR_HEIGHT);

            // 顶部一条细线，把标签栏和内容区分开
            Image oTopLine = UiFactory.CreatePanel(oBackground.transform, "TopLine", Theme.DIVIDER);
            UiFactory.AnchorTop(oTopLine.rectTransform, Theme.DIVIDER_HEIGHT);

            RectTransform oContainer = UiFactory.CreateRowContainer(oBackground.transform, "Tabs");
            UiFactory.Stretch(oContainer);
            HorizontalLayoutGroup oLayout = oContainer.GetComponent<HorizontalLayoutGroup>();
            oLayout.childForceExpandWidth = true;

            foreach ((string sKey, string sIcon, string sLabel) in lTabs)
            {
                string sCapturedKey = sKey;

                Button oButton = UiFactory.CreateTabButton(oContainer, $"Tab_{sKey}", sIcon, sLabel,
                    () => TabClicked?.Invoke(sCapturedKey));
                UiFactory.SetFlexible(oButton.GetComponent<RectTransform>());

                m_Keys.Add(sKey);
                m_IconNames.Add(sIcon);
                m_Buttons.Add(oButton);
                m_Labels.Add(UiFactory.GetButtonLabel(oButton));
                m_Icons.Add(UiFactory.GetButtonIcon(oButton));
            }

            if (m_Keys.Count > 0)
            {
                SetSelected(m_Keys[0]);
            }
        }

        public void SetSelected(string sKey)
        {
            for (int i = 0; i < m_Keys.Count; i++)
            {
                bool bSelected = m_Keys[i] == sKey;
                Color oColor = bSelected ? Theme.PRIMARY : Theme.TEXT_WEAK;

                if (m_Labels[i] != null)
                {
                    m_Labels[i].color = oColor;
                    m_Labels[i].fontStyle = bSelected ? FontStyle.Bold : FontStyle.Normal;
                }

                _paintIcon(i, bSelected, oColor);
            }
        }

        /// <summary>
        /// 切换图标。约定：给 tab_list_on.png 就自动用作选中态；
        /// 没给就同一张图换色（所以图标请做成单色 PNG，颜色由代码控制）。
        /// </summary>
        private void _paintIcon(int iIndex, bool bSelected, Color oColor)
        {
            Image oIcon = m_Icons[iIndex];
            if (oIcon == null)
            {
                return;
            }

            if (bSelected)
            {
                Sprite oSelectedSprite = AssetProvider.Icon(m_IconNames[iIndex] + IconNames.SELECTED_SUFFIX);
                if (oSelectedSprite != null)
                {
                    oIcon.sprite = oSelectedSprite;
                }
            }
            else
            {
                Sprite oNormalSprite = AssetProvider.Icon(m_IconNames[iIndex]);
                if (oNormalSprite != null)
                {
                    oIcon.sprite = oNormalSprite;
                }
            }

            oIcon.color = oColor;
        }
    }
}
