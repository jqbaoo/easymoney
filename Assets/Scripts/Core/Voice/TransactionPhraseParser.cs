using System;
using System.Collections.Generic;

namespace EasyMoney.Core
{
    /// <summary>
    /// 把一句话解析成 <see cref="TransactionDraft"/>。语音记账里「听懂了」的那一半，
    /// 另一半是把声音变成文字（ASR），那部分在平台侧，与本类无关。
    ///
    /// 句式是**标签锚定**而不是位置固定：找「金额」这个词，它后面跟的就是金额，
    /// 所以字段顺序随便变，ASR 漏掉一段也只丢那一个字段，不会整句崩。
    /// 触发词（「记一笔」）出现在哪都行，它不携带值。
    ///
    /// 解析不出来是**常态**不是异常——用户说错、ASR 听错都会走到这里，
    /// 所以返回 bool 而不是抛。
    /// </summary>
    public static class TransactionPhraseParser
    {
        /// <summary>
        /// 片段分隔符。ASR 的停顿常被识别成逗号，所以连着的几个要能容忍。
        /// 句末标点也在内：它们同样不该混进值里。
        /// </summary>
        private static readonly char[] SEPARATORS =
        {
            ',', '，', '、', ';', '；', ':', '：', ' ', '\t', '\n', '\r',
            '。', '.', '!', '！', '?', '？', '"', '\'', '(', ')', '（', '）'
        };

        /// <summary>
        /// 值开头的连接词。「金额是30」里的值是「是30」，那个「是」不是内容。
        /// 只收这三个——多收了会开始吃正文（比如把「为什么」的「为」咬掉）。
        /// </summary>
        private static readonly string[] CONNECTORS = { "是", "为", "=" };

        /// <summary>
        /// 解析一句话。<paramref name="lCategoryNames"/> 是当前分类名表，
        /// 用途只有一个：「类型」后面的值要靠它判断是分类还是账单类型。
        /// 传 null 或空表时，所有「类型X」都按账单类型处理，认不出就报为未识别。
        /// </summary>
        public static bool TryParse(
            string sText, IList<string> lCategoryNames, out TransactionDraft oDraft)
        {
            oDraft = null;

            if (string.IsNullOrWhiteSpace(sText))
            {
                return false;
            }

            string sWorking = _stripTriggers(sText);
            List<LabelHit> lPicked = _pickLeftToRight(_findLabels(sWorking));

            TransactionDraft oResult = new TransactionDraft();

            // 第一个标签之前的文字没有任何标签认领。空段（ASR 停顿识别出的多余标点）
            // 清理后是空串，在这里自然被跳过
            int iFirstPos = lPicked.Count > 0 ? lPicked[0].Position : sWorking.Length;
            string sPrefix = _cleanValue(sWorking.Substring(0, iFirstPos));
            if (sPrefix.Length > 0)
            {
                oResult.Unrecognized.Add(sPrefix);
            }

            for (int i = 0; i < lPicked.Count; i++)
            {
                LabelHit oHit = lPicked[i];
                int iStart = oHit.Position + oHit.Word.Length;
                int iEnd = i + 1 < lPicked.Count ? lPicked[i + 1].Position : sWorking.Length;

                string sValue = _cleanValue(sWorking.Substring(iStart, iEnd - iStart));
                if (sValue.Length == 0)
                {
                    continue;
                }

                _assign(oResult, oHit.Field, sValue, lCategoryNames);
            }

            // 一个字段都没解析出来就别往下走。返回一份全空的草稿会让调用方
            // 把用户已经选好的账户、分类、日期全冲掉，而用户只是说了句没听懂的话
            if (!oResult.HasAnyField)
            {
                return false;
            }

            oDraft = oResult;
            return true;
        }

        // ── 切分 ────────────────────────────────────

        /// <summary>
        /// 剔除触发词。**只剔独占一个片段的那些**——备注里说「这个月记账真麻烦」时
        /// 「记账」是内容的一部分，吃掉它就把备注改坏了，而用户看不出来。
        /// </summary>
        private static string _stripTriggers(string sText)
        {
            string sResult = sText;

            foreach (string sTrigger in PhraseLabels.TRIGGERS)
            {
                int iFrom = 0;
                while (iFrom <= sResult.Length - sTrigger.Length)
                {
                    int iAt = sResult.IndexOf(sTrigger, iFrom, StringComparison.Ordinal);
                    if (iAt < 0)
                    {
                        break;
                    }

                    bool bStandalone = _isBoundary(sResult, iAt - 1)
                        && _isBoundary(sResult, iAt + sTrigger.Length);

                    if (bStandalone)
                    {
                        sResult = sResult.Remove(iAt, sTrigger.Length);
                        iFrom = iAt;
                    }
                    else
                    {
                        iFrom = iAt + 1;
                    }
                }
            }

            return sResult;
        }

        /// <summary>这个下标上是片段边界吗——越界、分隔符、标点都算。</summary>
        private static bool _isBoundary(string sText, int iIndex)
        {
            if (iIndex < 0 || iIndex >= sText.Length)
            {
                return true;
            }

            return Array.IndexOf(SEPARATORS, sText[iIndex]) >= 0;
        }

        /// <summary>
        /// 找出所有标签出现的位置。按 <see cref="PhraseLabels.ALL"/> 的顺序扫，
        /// 那张表已按长度倒序——长的先试，短词就抢不走它的开头。
        /// </summary>
        private static List<LabelHit> _findLabels(string sText)
        {
            List<LabelHit> lHits = new List<LabelHit>();

            foreach (KeyValuePair<PhraseField, string> oLabel in PhraseLabels.ALL)
            {
                int iFrom = 0;
                while (iFrom <= sText.Length - oLabel.Value.Length)
                {
                    int iAt = sText.IndexOf(oLabel.Value, iFrom, StringComparison.Ordinal);
                    if (iAt < 0)
                    {
                        break;
                    }

                    lHits.Add(new LabelHit(iAt, oLabel.Value, oLabel.Key));
                    iFrom = iAt + 1;
                }
            }

            // 位置升序；同一位置长的在前。同位置的两个词（「金额」与「金额多少」）
            // 排好序后长的在前，下面按左到右贪心时它先被选走，短的因重叠被跳过
            lHits.Sort((oLeft, oRight) =>
            {
                int iByPosition = oLeft.Position.CompareTo(oRight.Position);
                return iByPosition != 0
                    ? iByPosition
                    : oRight.Word.Length.CompareTo(oLeft.Word.Length);
            });

            return lHits;
        }

        /// <summary>从左到右贪心，跳过与已选中标签重叠的候选。</summary>
        private static List<LabelHit> _pickLeftToRight(List<LabelHit> lHits)
        {
            List<LabelHit> lPicked = new List<LabelHit>();
            int iLastEnd = -1;

            foreach (LabelHit oHit in lHits)
            {
                if (oHit.Position < iLastEnd)
                {
                    continue;
                }

                lPicked.Add(oHit);
                iLastEnd = oHit.Position + oHit.Word.Length;
            }

            return lPicked;
        }

        /// <summary>
        /// 清理一段值：去掉首尾空白与标点，再去掉开头的连接词。
        /// 值内部的标点保留——备注里写「哈哈哈，真好笑」时那个逗号是内容。
        /// </summary>
        private static string _cleanValue(string sRaw)
        {
            string sValue = sRaw.Trim(SEPARATORS).Trim();

            foreach (string sConnector in CONNECTORS)
            {
                if (sValue.StartsWith(sConnector, StringComparison.Ordinal))
                {
                    sValue = sValue.Substring(sConnector.Length).Trim(SEPARATORS).Trim();
                    break;
                }
            }

            return sValue;
        }

        // ── 赋值 ────────────────────────────────────

        /// <summary>
        /// 把一段值放进对应字段。**同一标签出现多次时以最后一次的合法值为准**——
        /// 人在自我纠正（「金额30……不对，金额50」）。非法的那个值被拒绝，
        /// 所以不会把前一次已经解析好的金额清掉。
        /// </summary>
        private static void _assign(
            TransactionDraft oDraft, PhraseField oField, string sValue,
            IList<string> lCategoryNames)
        {
            switch (oField)
            {
                case PhraseField.Amount:
                    // 复用 MoneyParser：纯数字、最多两位小数、拒绝千分位与科学计数法，
                    // 这套规则连同测试都已经在那边做好了，这里不重写一遍。
                    // 它允许负号，所以正负校验得自己补
                    if (MoneyParser.TryParseYuan(sValue, out Money oAmount) && oAmount.Cents > 0)
                    {
                        oDraft.AmountCents = oAmount.Cents;
                    }

                    break;

                case PhraseField.Type:
                    _assignTypeOrCategory(oDraft, sValue, lCategoryNames);
                    break;

                case PhraseField.Category:
                    oDraft.CategoryName = sValue;
                    break;

                case PhraseField.Account:
                    oDraft.AccountName = sValue;
                    break;

                case PhraseField.ToAccount:
                    oDraft.ToAccountName = sValue;
                    break;

                case PhraseField.Date:
                    int iDay = Array.IndexOf(PhraseLabels.DATE_VALUES, sValue);
                    if (iDay >= 0)
                    {
                        oDraft.DateIndex = iDay;
                    }

                    break;

                case PhraseField.Note:
                    oDraft.Note = sValue;
                    break;
            }
        }

        /// <summary>
        /// 「类型」后面的值到底指什么，**看它落在哪张表里**，不看在哪个标签后面。
        /// 「类型餐饮」是分类，「类型收入」是账单类型——用户不必改口头习惯去区分
        /// 「类型」和「分类」这两个词。
        ///
        /// 认分类名这一步走 <see cref="NameMatcher.PickUniqueIndex"/>，与
        /// <see cref="DraftResolver"/> 用的是**同一套规则**，所以「类型餐引」听错一个字
        /// 也认得出。两处若各用一套，就会出现「这里认了、那边匹配不上」的错位，
        /// 而用户看到的只是「分类莫名其妙没填上」。
        ///
        /// ⚠️ 认出来之后**原样把值传下去**（<c>CategoryName = "餐引"</c>），**不在这里纠正**：
        /// 这里只回答「这算不算一个分类名」，换成库里哪个 Id 是 <see cref="DraftResolver"/>
        /// 的职责。两处都纠一次的话，以后改匹配规则就得改两个地方，漏一个行为就不一致。
        ///
        /// 两边都不在时不猜，报为未识别让用户看见：猜错会把账记到别的分类上，
        /// 而用户不一定发现；留空他补一下就好。
        /// </summary>
        private static void _assignTypeOrCategory(
            TransactionDraft oDraft, string sValue, IList<string> lCategoryNames)
        {
            foreach ((string sWord, TxType eType) in PhraseLabels.TYPE_VALUES)
            {
                if (string.Equals(sWord, sValue, StringComparison.Ordinal))
                {
                    oDraft.Type = eType;
                    return;
                }
            }

            if (NameMatcher.PickUniqueIndex(sValue, lCategoryNames) >= 0)
            {
                oDraft.CategoryName = sValue;
                return;
            }

            oDraft.Unrecognized.Add(sValue);
        }

        /// <summary>一个标签在句子里的某次出现。</summary>
        private readonly struct LabelHit
        {
            public readonly int Position;

            public readonly string Word;

            public readonly PhraseField Field;

            public LabelHit(int iPosition, string sWord, PhraseField eField)
            {
                Position = iPosition;
                Word = sWord;
                Field = eField;
            }
        }
    }
}
