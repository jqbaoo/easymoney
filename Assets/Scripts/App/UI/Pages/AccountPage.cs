using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EasyMoney.App.UI.Pages
{
    /// <summary>账户。顶部是总资产，下面列出各账户实时余额。</summary>
    public class AccountPage : PageBase
    {
        private const float HEADER_HEIGHT = 220f;
        private const float BALANCE_WIDTH = 300f;

        private Text m_TotalLabel;
        private RectTransform m_ListContent;

        public override string Title => "账户";

        public override void OnShow()
        {
            _refresh();
        }

        protected override void _build()
        {
            _buildHeader();

            RectTransform oListArea = UiFactory.CreateNode(Root, "ListArea");
            UiFactory.StretchWithInsets(oListArea, HEADER_HEIGHT, Theme.BUTTON_HEIGHT + Theme.PAGE_GAP * 2f);
            UiFactory.CreateScroll(oListArea, "Scroll", out m_ListContent);

            _buildAddButton();
        }

        // ── 总资产 ──────────────────────────────────

        private void _buildHeader()
        {
            Image oHeader = UiFactory.CreatePanel(Root, "Header", Theme.PRIMARY);
            UiFactory.AnchorTop(oHeader.rectTransform, HEADER_HEIGHT);

            Text oCaption = UiFactory.CreateText(oHeader.transform, "Caption", "总资产",
                Theme.FONT_CAPTION, TextAnchor.UpperCenter, new Color(1f, 1f, 1f, 0.75f));
            oCaption.rectTransform.anchorMin = new Vector2(0f, 1f);
            oCaption.rectTransform.anchorMax = new Vector2(1f, 1f);
            oCaption.rectTransform.pivot = new Vector2(0.5f, 1f);
            oCaption.rectTransform.anchoredPosition = new Vector2(0f, -40f);
            oCaption.rectTransform.sizeDelta = new Vector2(0f, 40f);

            m_TotalLabel = UiFactory.CreateText(oHeader.transform, "Total", DemoData.TOTAL_ASSETS,
                Theme.FONT_HERO, TextAnchor.MiddleCenter, Theme.WHITE);
            UiFactory.Stretch(m_TotalLabel.rectTransform);
        }

        // ── 账户列表 ────────────────────────────────

        private void _refresh()
        {
            m_TotalLabel.text = DemoData.TOTAL_ASSETS;
            _renderList(DemoData.BuildAccounts());
        }

        private void _renderList(List<DemoData.DemoAccount> lAccounts)
        {
            _clearList();

            foreach (DemoData.DemoAccount oAccount in lAccounts)
            {
                _addAccountRow(oAccount);
            }
        }

        private void _addAccountRow(DemoData.DemoAccount oAccount)
        {
            RectTransform oRow = UiFactory.CreateRow(m_ListContent, $"Account_{oAccount.Name}",
                Theme.ROW_HEIGHT_LARGE);

            Image oBackground = oRow.gameObject.AddComponent<Image>();
            oBackground.sprite = SpriteFactory.Card();
            oBackground.type = Image.Type.Sliced;
            oBackground.color = Theme.SURFACE;

            RectTransform oLeft = UiFactory.CreateNode(oRow, "Left");
            UiFactory.SetFlexible(oLeft);

            VerticalLayoutGroup oLeftLayout = oLeft.gameObject.AddComponent<VerticalLayoutGroup>();
            oLeftLayout.childControlWidth = true;
            oLeftLayout.childControlHeight = true;
            oLeftLayout.childForceExpandWidth = true;
            oLeftLayout.childForceExpandHeight = false;

            Text oName = UiFactory.CreateText(oLeft, "Name", oAccount.Name,
                Theme.FONT_TITLE, TextAnchor.LowerLeft);
            UiFactory.SetHeight(oName.rectTransform, 62f);

            Text oType = UiFactory.CreateText(oLeft, "Type", oAccount.TypeLabel,
                Theme.FONT_CAPTION, TextAnchor.UpperLeft, Theme.TEXT_WEAK);
            UiFactory.SetHeight(oType.rectTransform, 46f);

            Text oBalance = UiFactory.CreateText(oRow, "Balance", oAccount.Balance,
                Theme.FONT_TITLE, TextAnchor.MiddleRight,
                oAccount.IsNegative ? Theme.EXPENSE : Theme.TEXT);
            UiFactory.SetWidth(oBalance.rectTransform, BALANCE_WIDTH);
        }

        private void _clearList()
        {
            for (int i = m_ListContent.childCount - 1; i >= 0; i--)
            {
                Object.Destroy(m_ListContent.GetChild(i).gameObject);
            }
        }

        // ── 添加账户 ────────────────────────────────

        private void _buildAddButton()
        {
            // 有 icon_add.png 就是「图标 + 添加账户」，没有就退回「+ 添加账户」
            Button oAdd = UiFactory.CreateIconTextButton(Root, "AddAccount", IconNames.ADD, "添加账户",
                null, Theme.PRIMARY, Theme.WHITE, Theme.FONT_TITLE, "+ 添加账户");

            RectTransform oRect = oAdd.GetComponent<RectTransform>();
            oRect.anchorMin = new Vector2(0f, 0f);
            oRect.anchorMax = new Vector2(1f, 0f);
            oRect.pivot = new Vector2(0.5f, 0f);
            oRect.sizeDelta = new Vector2(-Theme.PAGE_PADDING * 2f, Theme.BUTTON_HEIGHT);
            oRect.anchoredPosition = new Vector2(0f, Theme.PAGE_GAP);
        }
    }
}
