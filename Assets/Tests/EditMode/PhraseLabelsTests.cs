using System.Collections.Generic;
using EasyMoney.Core;
using NUnit.Framework;

namespace EasyMoney.Tests
{
    /// <summary>
    /// 标签词表本身的完整性。不吃任何输入，只盯着表。
    ///
    /// 这类断言很便宜，但拦得住一个很隐蔽的错：手滑把「备注」同时写进两个数组，
    /// 界面上完全看不出来，只有某些句子会诡异解析错——跟 FontSlotsTests 钉字重
    /// 映射是同一个套路：**映射表本身要有人看着**。
    /// </summary>
    public class PhraseLabelsTests
    {
        [Test]
        public void All_HasNoDuplicateWord()
        {
            HashSet<string> lSeen = new HashSet<string>();

            foreach (KeyValuePair<PhraseField, string> oLabel in PhraseLabels.ALL)
            {
                Assert.IsTrue(lSeen.Add(oLabel.Value),
                    $"标签词「{oLabel.Value}」在表里出现了两次");
            }
        }

        [Test]
        public void All_MapsEachWordToExactlyOneField()
        {
            Dictionary<string, PhraseField> lMap = new Dictionary<string, PhraseField>();

            foreach (KeyValuePair<PhraseField, string> oLabel in PhraseLabels.ALL)
            {
                if (lMap.TryGetValue(oLabel.Value, out PhraseField eExisting))
                {
                    Assert.AreEqual(eExisting, oLabel.Key,
                        $"「{oLabel.Value}」同时映射到 {eExisting} 和 {oLabel.Key}，" +
                        "解析时会按先扫到的那个算，结果是哪句解析错完全看不出来");
                    continue;
                }

                lMap[oLabel.Value] = oLabel.Key;
            }
        }

        /// <summary>
        /// 解析器直接按 ALL 的顺序扫，指望长的在前。
        ///
        /// 这条红了不会崩，只会让「金额多少」这类新加的延伸词被短的「金额」抢先匹配，
        /// 值变成「多少50」——加词的人不会想到要去检查顺序。
        /// </summary>
        [Test]
        public void All_IsOrderedLongestFirst()
        {
            for (int i = 1; i < PhraseLabels.ALL.Length; i++)
            {
                string sPrevious = PhraseLabels.ALL[i - 1].Value;
                string sCurrent = PhraseLabels.ALL[i].Value;

                Assert.LessOrEqual(sCurrent.Length, sPrevious.Length,
                    $"「{sPrevious}」（{sPrevious.Length} 字）应排在「{sCurrent}」" +
                    $"（{sCurrent.Length} 字）前面，否则短词会抢走长词的开头");
            }
        }

        /// <summary>
        /// 日期词表的下标就是草稿里的 DateIndex，必须与记账页那三个选项同序。
        /// 错位的话说「昨天」会记成「前天」，而且没有任何报错——只能靠这条钉住。
        /// </summary>
        [Test]
        public void DateValues_MatchRecordPageOrder()
        {
            Assert.AreEqual(new[] { "今天", "昨天", "前天" }, PhraseLabels.DATE_VALUES);
        }
    }
}
