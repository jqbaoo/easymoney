using UnityEngine;
using UnityEngine.UI;

namespace EasyMoney.App.UI.Pages
{
    /// <summary>
    /// 记一笔。打开 App 到记完一笔应该不超过 3 次点击，所以把所有字段摊在一屏里，
    /// 不做二级页面。
    /// </summary>
    public class RecordPage : PageBase
    {
        private const float LABEL_WIDTH = 140f;
        private const float CHEVRON_WIDTH = 44f;
        private const float AMOUNT_ROW_HEIGHT = 140f;

        private static readonly string[] SEGMENT_LABELS = { "支出", "收入", "转账" };

        private int m_TypeIndex;
        private Button[] m_Segments;
        private InputField m_AmountInput;
        private InputField m_NoteInput;
        private Text m_CategoryValue;
        private Text m_AccountValue;
        private Text m_DateValue;
        private RectTransform m_CategoryRow;

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
            _buildDetailCard(oBody);
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
            m_TypeIndex = iIndex;

            for (int i = 0; i < m_Segments.Length; i++)
            {
                bool bSelected = i == iIndex;
                UiFactory.PaintButton(m_Segments[i],
                    bSelected ? Theme.PRIMARY : Theme.SURFACE,
                    bSelected ? Theme.WHITE : Theme.TEXT);
            }

            // 转账没有分类可言，隐藏该行避免填出无意义的数据
            if (m_CategoryRow != null)
            {
                m_CategoryRow.gameObject.SetActive(iIndex != 2);
            }
        }

        // ── 金额 ────────────────────────────────────

        private void _buildAmountCard(RectTransform oBody)
        {
            RectTransform oCard = UiFactory.CreateCard(oBody, "AmountCard");
            RectTransform oRow = UiFactory.CreateBareRow(oCard, "AmountRow", AMOUNT_ROW_HEIGHT);

            Text oLabel = UiFactory.CreateText(oRow, "Label", "金额", Theme.FONT_BODY);
            UiFactory.SetWidth(oLabel.rectTransform, LABEL_WIDTH);

            m_AmountInput = UiFactory.CreateInput(oRow, "AmountInput", "0.00", Theme.FONT_HERO);
            UiFactory.SetFlexible(m_AmountInput.GetComponent<RectTransform>());
            m_AmountInput.textComponent.alignment = TextAnchor.MiddleRight;
            m_AmountInput.contentType = InputField.ContentType.DecimalNumber;
            m_AmountInput.characterLimit = 12;
        }

        // ── 明细 ────────────────────────────────────

        private void _buildDetailCard(RectTransform oBody)
        {
            RectTransform oCard = UiFactory.CreateCard(oBody, "DetailCard");

            m_CategoryRow = _addLinkRow(oCard, "分类", DemoData.ExpenseCategoryNames[0], out m_CategoryValue);
            UiFactory.CreateDivider(oCard, "Divider1");

            _addLinkRow(oCard, "账户", "现金", out m_AccountValue);
            UiFactory.CreateDivider(oCard, "Divider2");

            _addLinkRow(oCard, "日期", "今天", out m_DateValue);
            UiFactory.CreateDivider(oCard, "Divider3");

            _addNoteRow(oCard);
        }

        /// <summary>一行「标签 —— 值 —— &gt;」的选择型表单行。</summary>
        private static RectTransform _addLinkRow(
            RectTransform oCard, string sLabel, string sValue, out Text oValueText)
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

        // ── 保存 ────────────────────────────────────

        private void _buildSaveButton(RectTransform oBody)
        {
            Button oSave = UiFactory.CreateButton(oBody, "Save", "保存", _onSave,
                Theme.PRIMARY, Theme.FONT_TITLE);
            UiFactory.SetHeight(oSave.GetComponent<RectTransform>(), Theme.BUTTON_HEIGHT);
        }

        private void _onSave()
        {
            // 原型阶段只清空表单。接上数据层后改为走 TransactionService.Save，
            // 失败时把 ValidationResult.ErrorMessage 显示在金额行下方。
            _resetForm();
        }

        private void _resetForm()
        {
            if (m_AmountInput != null)
            {
                m_AmountInput.text = string.Empty;
            }

            if (m_NoteInput != null)
            {
                m_NoteInput.text = string.Empty;
            }

            _selectType(0);
        }
    }
}
