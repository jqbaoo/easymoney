using System.Collections.Generic;
using EasyMoney.Core;
using UnityEngine;
using UnityEngine.UI;

namespace EasyMoney.App.UI.Reports
{
    /// <summary>
    /// 条形视图：一个分类一行，行里再挂一根按占比填充的条。
    ///
    /// 这就是报表页原来的样子，整段从页面里搬过来的——节点名和版式一个没动，
    /// 「轨道 + 填充两层 Image，靠填充层的 anchorMax.x 表达占比」那套也照旧：
    /// 比引入图表库轻得多，也够用。
    /// </summary>
    public class BarReportView : IReportView
    {
        public ReportViewMode Mode => ReportViewMode.Bar;

        public void Render(RectTransform oContent, IList<CategoryBreakdownItem> lItems, TxType oType)
        {
            if (lItems == null || lItems.Count == 0)
            {
                ReportViewParts.AddEmptyHint(oContent);
                return;
            }

            foreach (CategoryBreakdownItem oItem in lItems)
            {
                _addBar(ReportViewParts.AddRow(oContent, oItem, bWithBar: true), oItem.Ratio, oType);
            }
        }

        private static void _addBar(RectTransform oParent, decimal dRatio, TxType oType)
        {
            RectTransform oTrack = UiFactory.CreateNode(oParent, "BarTrack");
            UiFactory.SetHeight(oTrack, Theme.BAR_TRACK_HEIGHT);

            Image oTrackImage = oTrack.gameObject.AddComponent<Image>();
            oTrackImage.sprite = SpriteFactory.Card();
            oTrackImage.type = Image.Type.Sliced;
            oTrackImage.color = Theme.BAR_TRACK;

            // 支出红 / 收入绿。转账不进收支统计、页面也切不到它，真传进来按支出算，
            // 与 ReportForm.BreakdownTitle 的兜底口径一致
            Image oFill = UiFactory.CreatePanel(oTrack, "Fill",
                oType == TxType.Income ? Theme.INCOME : Theme.EXPENSE, bRounded: true);

            // 用锚点右边界表达占比：0 = 一点不画，1 = 铺满整条轨道
            float fRatio = (float)ReportForm.BarWidthRatio(dRatio);

            oFill.rectTransform.anchorMin = new Vector2(0f, 0f);
            oFill.rectTransform.anchorMax = new Vector2(fRatio, 1f);
            oFill.rectTransform.offsetMin = Vector2.zero;
            oFill.rectTransform.offsetMax = Vector2.zero;
        }
    }
}
