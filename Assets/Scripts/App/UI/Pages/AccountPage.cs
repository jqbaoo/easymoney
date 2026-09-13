using System.Collections.Generic;
using EasyMoney.Core;
using UnityEngine;
using UnityEngine.UI;

namespace EasyMoney.App.UI.Pages
{
    /// <summary>
    /// 账户。顶部是总资产，下面列出各账户的实时余额，底部是「添加账户」。
    ///
    /// 余额和总资产都由仓储用 SQL 实时聚合，页面不做任何加减；
    /// 账户类型的标签、表单校验在 <see cref="AccountForm"/> 里。
    /// </summary>
    public class AccountPage : PageBase
    {
        private const float HEADER_HEIGHT = 220f;
        private const float BALANCE_WIDTH = 290f;
        private const float EDIT_BUTTON_WIDTH = 88f;
        private const float EDIT_ICON_SIZE = 36f;
        private const float EMPTY_HINT_HEIGHT = 200f;
        private const float ARCHIVE_TOGGLE_HEIGHT = 80f;

        private const string ARCHIVED_SUFFIX = " · 已归档";

        private Text m_TotalLabel;
        private RectTransform m_ListContent;

        /// <summary>
        /// 是否把已归档账户也列出来。归档是「收起来」不是「删掉」，
        /// 收起来的东西得留个地方能再打开——否则用户误点一次就再也找不回来了。
        /// </summary>
        private bool m_ShowArchived;

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
                Theme.FONT_CAPTION, TextAnchor.UpperCenter, Theme.WHITE);
            oCaption.rectTransform.anchorMin = new Vector2(0f, 1f);
            oCaption.rectTransform.anchorMax = new Vector2(1f, 1f);
            oCaption.rectTransform.pivot = new Vector2(0.5f, 1f);
            oCaption.rectTransform.anchoredPosition = new Vector2(0f, -40f);
            oCaption.rectTransform.sizeDelta = new Vector2(0f, 40f);

            m_TotalLabel = UiFactory.CreateText(oHeader.transform, "Total", "0.00",
                Theme.FONT_HERO, TextAnchor.MiddleCenter, Theme.WHITE, Theme.WEIGHT_AMOUNT);
            UiFactory.Stretch(m_TotalLabel.rectTransform);
        }

        // ── 列表 ────────────────────────────────────

        private void _refresh()
        {
            AppContext oContext = AppContext.Instance;
            if (oContext == null)
            {
                return;
            }

            // 总资产用仓储自己的聚合，它已经排除了已归档账户
            m_TotalLabel.text = oContext.Accounts.GetTotalAssets().ToString();

            // 列表一次取全部（含已归档），在页面里按开关过滤：得先知道已归档有几条，
            // 才能决定要不要给出「显示已归档」这个入口
            List<AccountBalance> lAll = oContext.Accounts.GetAllWithBalance(bIncludeArchived: true);

            List<AccountBalance> lVisible = new List<AccountBalance>();
            int iArchivedCount = 0;

            foreach (AccountBalance oBalance in lAll)
            {
                if (!oBalance.Account.IsArchived)
                {
                    lVisible.Add(oBalance);
                    continue;
                }

                iArchivedCount++;
                if (m_ShowArchived)
                {
                    lVisible.Add(oBalance);
                }
            }

            _renderList(lVisible, iArchivedCount);
        }

        private void _renderList(List<AccountBalance> lBalances, int iArchivedCount)
        {
            _clearList();

            if (lBalances.Count == 0)
            {
                _addEmptyHint(iArchivedCount);
            }
            else
            {
                foreach (AccountBalance oBalance in lBalances)
                {
                    _addAccountRow(oBalance);
                }
            }

            // 没有已归档账户时这一行不出现，平时不占地方
            if (iArchivedCount > 0)
            {
                _addArchiveToggle(iArchivedCount);
            }
        }

        private void _addEmptyHint(int iArchivedCount)
        {
            // 全都归档了还说「还没有账户」会让人以为数据丢了
            string sHint = iArchivedCount > 0
                ? "没有在用中的账户，已归档的收在下面"
                : "还没有账户，点下面按钮添加一个";

            Text oEmpty = UiFactory.CreateText(m_ListContent, "Empty", sHint,
                Theme.FONT_CAPTION, TextAnchor.MiddleCenter, Theme.TEXT_WEAK);
            UiFactory.SetHeight(oEmpty.rectTransform, EMPTY_HINT_HEIGHT);
        }

        private void _addAccountRow(AccountBalance oAccountBalance)
        {
            Account oAccount = oAccountBalance.Account;
            RectTransform oRow = UiFactory.CreateRow(
                m_ListContent, $"Account_{oAccount.Id}", Theme.ROW_HEIGHT_LARGE);

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

            Text oType = UiFactory.CreateText(oLeft, "Type", _typeCaption(oAccount),
                Theme.FONT_CAPTION, TextAnchor.UpperLeft, Theme.TEXT_WEAK);
            UiFactory.SetHeight(oType.rectTransform, 46f);

            Text oBalanceText = UiFactory.CreateText(oRow, "Balance", oAccountBalance.Balance.ToString(),
                Theme.FONT_TITLE, TextAnchor.MiddleRight,
                oAccountBalance.Balance.Cents < 0 ? Theme.EXPENSE : Theme.TEXT,
                Theme.WEIGHT_AMOUNT);
            UiFactory.SetWidth(oBalanceText.rectTransform, BALANCE_WIDTH);

            _addEditButton(oRow, oAccount);
        }

        private static string _typeCaption(Account oAccount)
        {
            string sLabel = AccountForm.TypeLabel(oAccount.Type);
            return oAccount.IsArchived ? sLabel + ARCHIVED_SUFFIX : sLabel;
        }

        private void _addEditButton(RectTransform oRow, Account oAccount)
        {
            Button oEdit = UiFactory.CreateButton(
                oRow, "Edit", "改", () => _showEditDialog(oAccount), Theme.TRANSPARENT, Theme.FONT_CAPTION);
            UiFactory.SetWidth(oEdit.GetComponent<RectTransform>(), EDIT_BUTTON_WIDTH);
            UiFactory.PaintButton(oEdit, Theme.TRANSPARENT, Theme.TEXT_WEAK);

            // 有编辑图就用图，没给就是原来的「改」字
            UiFactory.ReplaceButtonLabelWithIcon(oEdit, IconNames.EDIT, EDIT_ICON_SIZE, Theme.TEXT_WEAK);
        }

        private void _addArchiveToggle(int iArchivedCount)
        {
            RectTransform oRow = UiFactory.CreateBareRow(
                m_ListContent, "ArchiveToggle", ARCHIVE_TOGGLE_HEIGHT);

            string sLabel = m_ShowArchived
                ? $"收起已归档的 {iArchivedCount} 个账户"
                : $"显示已归档的 {iArchivedCount} 个账户";

            Text oLabel = UiFactory.CreateText(oRow, "Label", sLabel,
                Theme.FONT_CAPTION, TextAnchor.MiddleCenter, Theme.PRIMARY);
            UiFactory.SetFlexible(oLabel.rectTransform);

            // CreateBareRow 只有布局组、没有 Graphic，接不住点击，补一层透明射线靶
            Image oHit = oRow.gameObject.AddComponent<Image>();
            oHit.color = Theme.TRANSPARENT;

            Button oButton = oRow.gameObject.AddComponent<Button>();
            oButton.targetGraphic = oHit;
            oButton.onClick.AddListener(_toggleArchivedVisibility);
        }

        private void _toggleArchivedVisibility()
        {
            m_ShowArchived = !m_ShowArchived;
            _refresh();
        }

        private void _clearList()
        {
            for (int i = m_ListContent.childCount - 1; i >= 0; i--)
            {
                Object.Destroy(m_ListContent.GetChild(i).gameObject);
            }
        }

        // ── 添加与编辑 ──────────────────────────────

        private void _buildAddButton()
        {
            // 有 icon_add.png 就是「图标 + 添加账户」，没有就退回「+ 添加账户」
            Button oAdd = UiFactory.CreateIconTextButton(Root, "AddAccount", IconNames.ADD, "添加账户",
                _showAddDialog, Theme.PRIMARY, Theme.WHITE, Theme.FONT_TITLE, "+ 添加账户",
                Theme.WEIGHT_STRONG);

            RectTransform oRect = oAdd.GetComponent<RectTransform>();
            oRect.anchorMin = new Vector2(0f, 0f);
            oRect.anchorMax = new Vector2(1f, 0f);
            oRect.pivot = new Vector2(0.5f, 0f);
            oRect.sizeDelta = new Vector2(-Theme.PAGE_PADDING * 2f, Theme.BUTTON_HEIGHT);
            oRect.anchoredPosition = new Vector2(0f, Theme.PAGE_GAP);
        }

        private void _showAddDialog()
        {
            AccountEditDialog.Show(Root, null);
        }

        private void _showEditDialog(Account oAccount)
        {
            AccountEditDialog.Show(Root, oAccount);
        }
    }
}
