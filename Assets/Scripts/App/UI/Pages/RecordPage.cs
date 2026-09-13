using System;
using System.Collections.Generic;
using EasyMoney.Core;
using UnityEngine;
using UnityEngine.UI;

namespace EasyMoney.App.UI.Pages
{
    /// <summary>
    /// 记一笔。打开 App 到记完一笔应该不超过 3 次点击，所以把所有字段摊在一屏里，
    /// 不做二级页面；进页面时账户与分类已经预选好，用户通常只需填个金额。
    ///
    /// 页面只负责取数与展示，金额解析、校验、落库分别交给 MoneyParser、
    /// TransactionValidator、TransactionService。
    /// </summary>
    public class RecordPage : PageBase
    {
        private const float LABEL_WIDTH = 140f;
        private const float CHEVRON_WIDTH = 44f;
        private const float AMOUNT_ROW_HEIGHT = 140f;
        private const float MESSAGE_HEIGHT = 60f;

        /// <summary>算「最近消费均值」时取样的条数。</summary>
        private const int RECENT_SAMPLE_SIZE = 20;

        private const long DAY_MS = 86_400_000L;

        private const string UNSELECTED = "请选择";

        private static readonly string[] SEGMENT_LABELS = { "支出", "收入", "转账" };
        private static readonly string[] DATE_LABELS = { "今天", "昨天", "前天" };

        private TxType m_Type = TxType.Expense;
        private int m_AccountId;
        private int m_ToAccountId;
        private int m_CategoryId;

        /// <summary>账单日期偏移：0 = 今天，1 = 昨天，2 = 前天。</summary>
        private int m_DateIndex;

        private Button[] m_Segments;
        private Button[] m_QuickButtons;
        private InputField m_AmountInput;
        private InputField m_NoteInput;
        private Text m_CategoryValue;
        private Text m_AccountValue;
        private Text m_ToAccountValue;
        private Text m_DateValue;
        private Text m_Message;
        private RectTransform m_CategoryRow;
        private RectTransform m_ToAccountRow;

        public override string Title => "记一笔";

        public override void OnShow()
        {
            _resetForm();
        }

        protected override void _build()
        {
            RectTransform oBody = _createBody(Root);

            _buildSegments(oBody);
            _buildAmountCard(oBody);
            _buildQuickAmounts(oBody);
            _buildDetailCard(oBody);
            _buildMessage(oBody);
            _buildSaveButton(oBody);
        }

        /// <summary>页面主体：贴顶、宽度左右各留 PAGE_PADDING、高度随内容自适应。</summary>
        private static RectTransform _createBody(RectTransform oRoot)
        {
            RectTransform oBody = UiFactory.CreateAutoColumn(oRoot, "Body", Theme.CARD_GAP);
            oBody.anchorMin = new Vector2(0f, 1f);
            oBody.anchorMax = new Vector2(1f, 1f);
            oBody.pivot = new Vector2(0.5f, 1f);
            oBody.sizeDelta = new Vector2(-Theme.PAGE_PADDING * 2f, 0f);
            oBody.anchoredPosition = new Vector2(0f, -Theme.PAGE_GAP);
            return oBody;
        }

        // ── 支出 / 收入 / 转账 ───────────────────────

        private void _buildSegments(RectTransform oBody)
        {
            RectTransform oRow = UiFactory.CreateRowContainer(oBody, "Segments", 4f);
            UiFactory.SetHeight(oRow, Theme.SEGMENT_HEIGHT);

            m_Segments = new Button[SEGMENT_LABELS.Length];

            for (int i = 0; i < SEGMENT_LABELS.Length; i++)
            {
                int iIndex = i;
                m_Segments[i] = UiFactory.CreateButton(oRow, $"Segment{i}", SEGMENT_LABELS[i],
                    () => _selectType(iIndex), Theme.SURFACE, Theme.FONT_BODY);
                UiFactory.SetFlexible(m_Segments[i].GetComponent<RectTransform>());
            }

            _selectType(0);
        }

        private void _selectType(int iIndex)
        {
            m_Type = iIndex switch
            {
                1 => TxType.Income,
                2 => TxType.Transfer,
                _ => TxType.Expense
            };

            // 换了类型，原来选的分类/转入账户都不再适用，必须清掉——
            // 否则会把「收入」记到一个支出分类上，保存时才报错
            m_CategoryId = 0;
            m_ToAccountId = 0;

            for (int i = 0; i < m_Segments.Length; i++)
            {
                bool bSelected = i == iIndex;
                UiFactory.PaintButton(m_Segments[i],
                    bSelected ? Theme.PRIMARY : Theme.SURFACE,
                    bSelected ? Theme.WHITE : Theme.TEXT);
            }

            _refreshVisibility();
            _refreshQuickAmounts();
        }

        private void _refreshVisibility()
        {
            // 转账没有分类可言，隐藏该行避免填出无意义的数据
            if (m_CategoryRow != null)
            {
                m_CategoryRow.gameObject.SetActive(m_Type != TxType.Transfer);
            }

            if (m_ToAccountRow != null)
            {
                m_ToAccountRow.gameObject.SetActive(m_Type == TxType.Transfer);
            }

            if (m_CategoryValue != null)
            {
                m_CategoryValue.text = UNSELECTED;
            }

            if (m_ToAccountValue != null)
            {
                m_ToAccountValue.text = UNSELECTED;
            }
        }

        // ── 金额 ────────────────────────────────────

        private void _buildAmountCard(RectTransform oBody)
        {
            RectTransform oCard = UiFactory.CreateCard(oBody, "AmountCard");
            RectTransform oRow = UiFactory.CreateBareRow(oCard, "AmountRow", AMOUNT_ROW_HEIGHT);

            Text oLabel = UiFactory.CreateText(oRow, "Label", "金额", Theme.FONT_BODY);
            UiFactory.SetWidth(oLabel.rectTransform, LABEL_WIDTH);

            // 金额是这一页的主角。字重同时管住占位符和实际输入，
            // 免得聚焦那一刻看到 "0.00" 和刚敲的数字粗细不一样
            m_AmountInput = UiFactory.CreateInput(oRow, "AmountInput", "0.00", Theme.FONT_HERO,
                Theme.WEIGHT_AMOUNT);
            UiFactory.SetFlexible(m_AmountInput.GetComponent<RectTransform>());
            m_AmountInput.textComponent.alignment = TextAnchor.MiddleRight;
            m_AmountInput.contentType = InputField.ContentType.DecimalNumber;
            m_AmountInput.characterLimit = 12;
        }

        // ── 快捷金额 ────────────────────────────────

        private void _buildQuickAmounts(RectTransform oBody)
        {
            RectTransform oRow = UiFactory.CreateRowContainer(oBody, "QuickAmounts", 8f);
            UiFactory.SetHeight(oRow, Theme.SEGMENT_HEIGHT);

            // 档位数固定为 4（QuickAmountHelper 的约定），这里一次建好，
            // 之后只改文案，不重建按钮
            m_QuickButtons = new Button[QuickAmountHelper.QUICK_AMOUNT_COUNT];

            for (int i = 0; i < m_QuickButtons.Length; i++)
            {
                // 必须把 i 抄一份：lambda 捕获的是变量本身，直接用 i 的话
                // 回调触发时循环早已结束，下标会指向数组外
                int iIndex = i;
                m_QuickButtons[i] = UiFactory.CreateButton(oRow, $"Quick{i}", string.Empty,
                    () => _fillAmount(m_QuickButtons[iIndex]), Theme.SURFACE, Theme.FONT_CAPTION);
                UiFactory.SetFlexible(m_QuickButtons[i].GetComponent<RectTransform>());
            }
        }

        private void _refreshQuickAmounts()
        {
            if (m_QuickButtons == null)
            {
                return;
            }

            List<Money> lAmounts = QuickAmountHelper.BuildQuickAmounts(_recentAverageCents());

            for (int i = 0; i < m_QuickButtons.Length && i < lAmounts.Count; i++)
            {
                Text oLabel = UiFactory.GetButtonLabel(m_QuickButtons[i]);
                if (oLabel != null)
                {
                    oLabel.text = lAmounts[i].ToString();
                    oLabel.color = Theme.TEXT;
                }
            }
        }

        private void _fillAmount(Button oButton)
        {
            Text oLabel = UiFactory.GetButtonLabel(oButton);
            if (oLabel != null)
            {
                m_AmountInput.text = oLabel.text;
            }

            _clearMessage();
        }

        /// <summary>最近同类账单的平均金额。没有历史或类型是转账时返回 0，由兜底档位接手。</summary>
        private long _recentAverageCents()
        {
            AppContext oContext = AppContext.Instance;
            if (oContext == null || m_Type == TxType.Transfer)
            {
                return 0;
            }

            List<Transaction> lRecent = oContext.Transactions.Query(
                new TransactionQuery { Type = m_Type, Limit = RECENT_SAMPLE_SIZE });

            if (lRecent.Count == 0)
            {
                return 0;
            }

            long iSum = 0;
            foreach (Transaction oTransaction in lRecent)
            {
                iSum += oTransaction.AmountCents;
            }

            return iSum / lRecent.Count;
        }

        // ── 明细 ────────────────────────────────────

        private void _buildDetailCard(RectTransform oBody)
        {
            RectTransform oCard = UiFactory.CreateCard(oBody, "DetailCard");

            m_CategoryRow = _addLinkRow(oCard, "分类", UNSELECTED, out m_CategoryValue,
                _showCategoryPicker);
            UiFactory.CreateDivider(oCard, "Divider1");

            _addLinkRow(oCard, "账户", UNSELECTED, out m_AccountValue, _showAccountPicker);
            UiFactory.CreateDivider(oCard, "Divider2");

            m_ToAccountRow = _addLinkRow(oCard, "转入", UNSELECTED, out m_ToAccountValue,
                _showToAccountPicker);
            UiFactory.CreateDivider(oCard, "Divider3");

            _addLinkRow(oCard, "日期", DATE_LABELS[0], out m_DateValue, _showDatePicker);
            UiFactory.CreateDivider(oCard, "Divider4");

            _addNoteRow(oCard);
        }

        /// <summary>一行「标签 —— 值 —— &gt;」的选择型表单行。</summary>
        private static RectTransform _addLinkRow(
            RectTransform oCard, string sLabel, string sValue, out Text oValueText,
            UnityEngine.Events.UnityAction oOnClick)
        {
            RectTransform oRow = UiFactory.CreateBareRow(oCard, $"Row_{sLabel}", Theme.ROW_HEIGHT);

            Text oLabel = UiFactory.CreateText(oRow, "Label", sLabel, Theme.FONT_BODY);
            UiFactory.SetWidth(oLabel.rectTransform, LABEL_WIDTH);

            oValueText = UiFactory.CreateText(oRow, "Value", sValue, Theme.FONT_BODY,
                TextAnchor.MiddleRight, Theme.TEXT);
            UiFactory.SetFlexible(oValueText.rectTransform);

            // 有 chevron_right 图标就是图标，没有就是原来的 ">"
            UiFactory.CreateIconOrText(oRow, "Chevron", IconNames.CHEVRON_RIGHT, ">",
                CHEVRON_WIDTH, Theme.FONT_BODY, Theme.TEXT_WEAK);

            // 整行可点，而不是只有箭头可点——手指没那么准
            Image oHit = oRow.gameObject.AddComponent<Image>();
            oHit.color = Theme.TRANSPARENT;

            Button oButton = oRow.gameObject.AddComponent<Button>();
            oButton.targetGraphic = oHit;
            oButton.onClick.AddListener(oOnClick);

            return oRow;
        }

        private void _addNoteRow(RectTransform oCard)
        {
            RectTransform oRow = UiFactory.CreateBareRow(oCard, "Row_备注", Theme.ROW_HEIGHT);

            Text oLabel = UiFactory.CreateText(oRow, "Label", "备注", Theme.FONT_BODY);
            UiFactory.SetWidth(oLabel.rectTransform, LABEL_WIDTH);

            m_NoteInput = UiFactory.CreateInput(oRow, "NoteInput", "选填", Theme.FONT_BODY);
            UiFactory.SetFlexible(m_NoteInput.GetComponent<RectTransform>());
            m_NoteInput.textComponent.alignment = TextAnchor.MiddleRight;
            m_NoteInput.characterLimit = 50;
        }

        // ── 提示与保存 ──────────────────────────────

        private void _buildMessage(RectTransform oBody)
        {
            m_Message = UiFactory.CreateText(oBody, "Message", string.Empty,
                Theme.FONT_CAPTION, TextAnchor.MiddleCenter, Theme.EXPENSE);
            UiFactory.SetHeight(m_Message.rectTransform, MESSAGE_HEIGHT);
        }

        private void _buildSaveButton(RectTransform oBody)
        {
            Button oSave = UiFactory.CreateButton(oBody, "Save", "保存", _onSave,
                Theme.PRIMARY, Theme.FONT_TITLE, Theme.WEIGHT_STRONG);
            UiFactory.SetHeight(oSave.GetComponent<RectTransform>(), Theme.BUTTON_HEIGHT);
        }

        private void _onSave()
        {
            AppContext oContext = AppContext.Instance;
            if (oContext == null)
            {
                _showMessage("数据尚未就绪，请稍后重试", false);
                return;
            }

            if (!MoneyParser.TryParseYuan(m_AmountInput.text, out Money oAmount))
            {
                _showMessage("请输入正确的金额，例如 12.50", false);
                return;
            }

            bool bIsTransfer = m_Type == TxType.Transfer;

            Transaction oTransaction = new Transaction
            {
                Type = m_Type,
                AmountCents = oAmount.Cents,
                AccountId = m_AccountId,
                ToAccountId = bIsTransfer ? m_ToAccountId : 0,
                CategoryId = bIsTransfer ? 0 : m_CategoryId,
                Note = m_NoteInput.text ?? string.Empty,
                OccurredAtMs = _occurredAtMs()
            };

            ValidationResult oResult = oContext.TxService.Save(
                oTransaction, TimeUtil.ToUnixMs(DateTime.Now));

            if (!oResult.IsValid)
            {
                _showMessage(oResult.ErrorMessage, false);
                return;
            }

            oContext.NotifyDataChanged();
            _resetForm();
            _showMessage("已保存", true);
        }

        private long _occurredAtMs()
        {
            return TimeUtil.ToUnixMs(DateTime.Now) - m_DateIndex * DAY_MS;
        }

        private void _showMessage(string sMessage, bool bSuccess)
        {
            if (m_Message == null)
            {
                return;
            }

            m_Message.text = sMessage;
            m_Message.color = bSuccess ? Theme.INCOME : Theme.EXPENSE;
        }

        private void _clearMessage()
        {
            if (m_Message != null)
            {
                m_Message.text = string.Empty;
            }
        }

        /// <summary>
        /// 回到「刚打开这一页」的状态：清空输入，并把账户与分类预选到第一项，
        /// 让用户少点两下。
        /// </summary>
        private void _resetForm()
        {
            if (m_AmountInput == null)
            {
                return;
            }

            m_AmountInput.text = string.Empty;
            m_NoteInput.text = string.Empty;

            m_AccountId = 0;
            m_ToAccountId = 0;
            m_CategoryId = 0;
            m_DateIndex = 0;
            m_DateValue.text = DATE_LABELS[0];

            _selectType(0);
            _applyDefaults();
            _clearMessage();
        }

        private void _applyDefaults()
        {
            AppContext oContext = AppContext.Instance;
            if (oContext == null)
            {
                return;
            }

            List<Category> lCategories = oContext.Categories.GetByKind(_categoryKind());
            if (lCategories.Count > 0)
            {
                _pickCategory(lCategories[0]);
            }

            List<Account> lAccounts = oContext.Accounts.GetAll();
            if (lAccounts.Count > 0)
            {
                _pickAccount(lAccounts[0]);
            }
        }

        private CategoryKind _categoryKind()
        {
            return m_Type == TxType.Income ? CategoryKind.Income : CategoryKind.Expense;
        }

        // ── 选择器 ──────────────────────────────────

        private void _showCategoryPicker()
        {
            AppContext oContext = AppContext.Instance;
            if (oContext == null)
            {
                return;
            }

            List<Category> lCategories = oContext.Categories.GetByKind(_categoryKind());
            List<string> lLabels = new List<string>();
            foreach (Category oCategory in lCategories)
            {
                lLabels.Add(oCategory.Name);
            }

            PickerDialog.Show(Root, "选择分类", lLabels,
                iIndex => _pickCategory(lCategories[iIndex]),
                "还没有分类，请先去「账户」页检查");
        }

        private void _pickCategory(Category oCategory)
        {
            m_CategoryId = oCategory.Id;
            m_CategoryValue.text = oCategory.Name;
            m_CategoryValue.color = Theme.TEXT;
        }

        private void _showAccountPicker()
        {
            _showAccountPickerInternal(false);
        }

        private void _showToAccountPicker()
        {
            _showAccountPickerInternal(true);
        }

        private void _showAccountPickerInternal(bool bIsTarget)
        {
            AppContext oContext = AppContext.Instance;
            if (oContext == null)
            {
                return;
            }

            List<Account> lAccounts = oContext.Accounts.GetAll();
            List<string> lLabels = new List<string>();
            foreach (Account oAccount in lAccounts)
            {
                lLabels.Add(oAccount.Name);
            }

            PickerDialog.Show(Root, bIsTarget ? "选择转入账户" : "选择账户", lLabels,
                iIndex =>
                {
                    if (bIsTarget)
                    {
                        _pickToAccount(lAccounts[iIndex]);
                    }
                    else
                    {
                        _pickAccount(lAccounts[iIndex]);
                    }
                },
                "还没有账户，请先去「账户」页添加");
        }

        private void _pickAccount(Account oAccount)
        {
            m_AccountId = oAccount.Id;
            m_AccountValue.text = oAccount.Name;
            m_AccountValue.color = Theme.TEXT;
        }

        private void _pickToAccount(Account oAccount)
        {
            m_ToAccountId = oAccount.Id;
            m_ToAccountValue.text = oAccount.Name;
            m_ToAccountValue.color = Theme.TEXT;
        }

        private void _showDatePicker()
        {
            List<string> lLabels = new List<string>(DATE_LABELS);

            PickerDialog.Show(Root, "选择日期", lLabels, iIndex =>
            {
                m_DateIndex = iIndex;
                m_DateValue.text = DATE_LABELS[iIndex];
            });
        }
    }
}
