using System;
using System.Collections.Generic;
using EasyMoney.Core;
using UnityEngine;
using UnityEngine.UI;

namespace EasyMoney.App.UI
{
    /// <summary>
    /// 账户编辑弹窗。新建与编辑共用一套表单，区别只在标题和要不要显示归档。
    ///
    /// 刻意不就地修改传进来的 <see cref="Account"/>：取消时用户改过的名称和类型都该丢掉，
    /// 而 Account 是引用类型，就地改会污染调用方那份实例。表单状态全放局部变量，
    /// 点保存才组装出一个新对象写库。校验规则在 <see cref="AccountForm"/> 里。
    /// </summary>
    public static class AccountEditDialog
    {
        private const float PANEL_WIDTH = 640f;
        private const float PANEL_PADDING = 32f;
        private const float TITLE_HEIGHT = 80f;
        private const float FIELD_HEIGHT = 110f;
        private const float MESSAGE_HEIGHT = 56f;
        private const float BUTTON_HEIGHT = 100f;

        /// <summary>
        /// 弹出编辑框。<paramref name="oAccount"/> 为 null 表示新建账户。
        /// 保存或归档成功后自行销毁，并喊一声「数据变了」，由 AppRoot 去刷新页面。
        /// </summary>
        public static void Show(RectTransform oParent, Account oAccount)
        {
            AppContext oContext = AppContext.Instance;
            if (oContext == null)
            {
                return;
            }

            bool bIsNew = oAccount == null;

            RectTransform oOverlay = UiFactory.CreateNode(oParent, "AccountEditOverlay");
            UiFactory.Stretch(oOverlay);

            // 遮罩同时拦掉穿透到下层的点击：Image 默认 raycastTarget = true
            Image oDim = oOverlay.gameObject.AddComponent<Image>();
            oDim.color = Theme.SCRIM;

            RectTransform oPanel = _createPanel(oOverlay, bIsNew);

            InputField oNameInput = _createField(oPanel, "Name", "账户名称", oAccount?.Name);
            InputField oBalanceInput = _createField(
                oPanel, "Balance", "初始余额（可留空）", _initialBalanceText(oAccount));

            // 闭包捕获的是这个变量本身，所以点保存时读到的一定是用户最后选的类型
            AccountType oSelectedType = oAccount?.Type ?? AccountType.Cash;
            _createTypeRow(oOverlay, oPanel, oSelectedType, oPicked => oSelectedType = oPicked);

            Text oMessage = UiFactory.CreateText(oPanel, "Message", string.Empty,
                Theme.FONT_CAPTION, TextAnchor.MiddleCenter, Theme.EXPENSE);
            UiFactory.SetHeight(oMessage.rectTransform, MESSAGE_HEIGHT);

            RectTransform oButtons = UiFactory.CreateRowContainer(oPanel, "Buttons", 16f);
            UiFactory.SetHeight(oButtons, BUTTON_HEIGHT);

            _addButton(oButtons, "Cancel", "取消",
                () => _close(oOverlay), Theme.BACKGROUND, Theme.TEXT);

            if (!bIsNew)
            {
                _addButton(oButtons, "Archive", oAccount.IsArchived ? "恢复" : "归档",
                    () => _toggleArchive(oOverlay, oAccount, oContext), Theme.BACKGROUND, Theme.EXPENSE);
            }

            _addButton(oButtons, "Save", "保存",
                () => _save(oOverlay, oAccount, bIsNew, oNameInput, oBalanceInput,
                    oMessage, oSelectedType, oContext),
                Theme.PRIMARY, Theme.WHITE);
        }

        // ── 面板骨架 ────────────────────────────────

        private static RectTransform _createPanel(RectTransform oOverlay, bool bIsNew)
        {
            Image oPanelImage = UiFactory.CreatePanel(
                oOverlay, "Panel", Theme.SURFACE, bRounded: true);

            RectTransform oPanel = oPanelImage.rectTransform;

            // 点锚居中 + 宽度固定，高度交给 ContentSizeFitter 按内容算：
            // 新建时少一个按钮，写死高度要么空一截要么撑爆
            oPanel.anchorMin = new Vector2(0.5f, 0.5f);
            oPanel.anchorMax = new Vector2(0.5f, 0.5f);
            oPanel.pivot = new Vector2(0.5f, 0.5f);
            oPanel.anchoredPosition = Vector2.zero;
            oPanel.sizeDelta = new Vector2(PANEL_WIDTH, 0f);

            VerticalLayoutGroup oLayout = oPanel.gameObject.AddComponent<VerticalLayoutGroup>();
            oLayout.childControlWidth = true;
            oLayout.childControlHeight = true;
            oLayout.childForceExpandWidth = true;
            oLayout.childForceExpandHeight = false;
            oLayout.padding = new RectOffset(
                (int)PANEL_PADDING, (int)PANEL_PADDING, (int)PANEL_PADDING, (int)PANEL_PADDING);
            oLayout.spacing = 16f;

            ContentSizeFitter oFitter = oPanel.gameObject.AddComponent<ContentSizeFitter>();
            oFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Text oTitle = UiFactory.CreateText(oPanel, "Title",
                bIsNew ? "添加账户" : "编辑账户", Theme.FONT_TITLE, TextAnchor.MiddleCenter);
            UiFactory.SetHeight(oTitle.rectTransform, TITLE_HEIGHT);

            return oPanel;
        }

        private static InputField _createField(
            RectTransform oPanel, string sName, string sPlaceholder, string sInitialText)
        {
            InputField oInput = UiFactory.CreateInput(oPanel, sName, sPlaceholder, Theme.FONT_BODY);
            oInput.text = sInitialText ?? string.Empty;
            oInput.characterLimit = 20;
            UiFactory.SetHeight(oInput.GetComponent<RectTransform>(), FIELD_HEIGHT);
            return oInput;
        }

        private static string _initialBalanceText(Account oAccount)
        {
            // 新建时留空，让 placeholder 起作用；编辑时才回填当前值。
            // 0 也留空——否则用户得先删掉那个 "0.00" 才能输入
            if (oAccount == null || oAccount.InitialBalanceCents == 0)
            {
                return string.Empty;
            }

            return Money.FromCents(oAccount.InitialBalanceCents).ToString();
        }

        // ── 类型选择 ────────────────────────────────

        private static void _createTypeRow(
            RectTransform oOverlay, RectTransform oPanel, AccountType oInitialType,
            Action<AccountType> oOnChanged)
        {
            RectTransform oRow = UiFactory.CreateRowContainer(oPanel, "Type");
            UiFactory.SetHeight(oRow, FIELD_HEIGHT);

            Text oCaption = UiFactory.CreateText(oRow, "Caption", "类型", Theme.FONT_BODY);
            UiFactory.SetWidth(oCaption.rectTransform, 100f);

            Text oValue = UiFactory.CreateText(oRow, "Value", AccountForm.TypeLabel(oInitialType),
                Theme.FONT_BODY, TextAnchor.MiddleRight, Theme.TEXT);
            UiFactory.SetFlexible(oValue.rectTransform);

            UiFactory.CreateIconOrText(oRow, "Chevron", IconNames.CHEVRON_RIGHT, ">",
                44f, Theme.FONT_BODY, Theme.TEXT_WEAK);

            // CreateRowContainer 只有布局组、没有 Graphic，接不住点击，补一层透明射线靶
            Image oHit = oRow.gameObject.AddComponent<Image>();
            oHit.color = Theme.TRANSPARENT;

            Button oButton = oRow.gameObject.AddComponent<Button>();
            oButton.targetGraphic = oHit;
            oButton.onClick.AddListener(() => _showTypePicker(oOverlay, oValue, oOnChanged));
        }

        private static void _showTypePicker(
            RectTransform oOverlay, Text oValueLabel, Action<AccountType> oOnChanged)
        {
            List<string> lLabels = new List<string>();
            foreach (AccountType oType in AccountForm.TYPE_CYCLE)
            {
                lLabels.Add(AccountForm.TypeLabel(oType));
            }

            // 挂在编辑弹窗自己的 overlay 上，层级比面板高，选完自行销毁
            PickerDialog.Show(oOverlay, "选择账户类型", lLabels, iIndex =>
            {
                AccountType oPicked = AccountForm.TYPE_CYCLE[iIndex];
                oValueLabel.text = AccountForm.TypeLabel(oPicked);
                oOnChanged(oPicked);
            });
        }

        // ── 按钮与写库 ──────────────────────────────

        private static void _addButton(
            RectTransform oRow, string sName, string sLabel, UnityEngine.Events.UnityAction oOnClick,
            Color oBackground, Color oLabelColor)
        {
            Button oButton = UiFactory.CreateButton(
                oRow, sName, sLabel, oOnClick, oBackground, Theme.FONT_BODY);
            UiFactory.SetFlexible(oButton.GetComponent<RectTransform>());
            UiFactory.PaintButton(oButton, oBackground, oLabelColor);
        }

        private static void _save(
            RectTransform oOverlay, Account oAccount, bool bIsNew,
            InputField oNameInput, InputField oBalanceInput, Text oMessage,
            AccountType oSelectedType, AppContext oContext)
        {
            AccountFormResult oResult = AccountForm.Validate(oNameInput.text, oBalanceInput.text);
            if (!oResult.IsValid)
            {
                // 校验没过就不关弹窗，让用户接着改
                oMessage.text = oResult.ErrorMessage;
                return;
            }

            if (bIsNew)
            {
                oContext.Accounts.Insert(new Account
                {
                    Name = oResult.Name,
                    Type = oSelectedType,
                    InitialBalanceCents = oResult.InitialBalanceCents,
                    CreatedAtMs = TimeUtil.ToUnixMs(DateTime.Now)
                });
            }
            else
            {
                // 重新组装而不是就地改 oAccount：后者会让「取消」变成改了一半的假取消
                oContext.Accounts.Update(new Account
                {
                    Id = oAccount.Id,
                    Name = oResult.Name,
                    Type = oSelectedType,
                    InitialBalanceCents = oResult.InitialBalanceCents,
                    IsArchived = oAccount.IsArchived,
                    SortOrder = oAccount.SortOrder,
                    CreatedAtMs = oAccount.CreatedAtMs
                });
            }

            _close(oOverlay);
            oContext.NotifyDataChanged();
        }

        private static void _toggleArchive(
            RectTransform oOverlay, Account oAccount, AppContext oContext)
        {
            oContext.Accounts.SetArchived(oAccount.Id, !oAccount.IsArchived);
            _close(oOverlay);
            oContext.NotifyDataChanged();
        }

        private static void _close(RectTransform oOverlay)
        {
            UnityEngine.Object.Destroy(oOverlay.gameObject);
        }
    }
}
