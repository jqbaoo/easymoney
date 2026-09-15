using System;
using System.Collections.Generic;

namespace EasyMoney.Core
{
    /// <summary>
    /// 名字相似度：给「用户说的名字」在候选表里找一个**最近似的**。
    ///
    /// 存在的理由：ASR 的中文错字以**同音替换**为主（「张三」→「张散」、
    /// 「餐饮」→「餐引」），而原先的匹配是严格全等——错一个字整个字段落空，
    /// 用户得手动补。说三个字段错两个的话，还不如直接打字。
    ///
    /// ⚠️ <b>这个类是「宁可猜不中，也不能猜错」的那一边。</b>
    /// 它挑出来的名字会被直接填进表单，而用户可能看都不看就保存——**猜错了
    /// 是一笔记到别的账户上的真账，且界面上看不出任何异常**。所以这里处处往严里走：
    /// 阈值随长度收紧、长度差超一个字就不认、一样近的候选并列时一个都不猜。
    /// 放宽任何一条之前，先回去看 <c>NameMatcherTests</c> 里对应的那组用例。
    ///
    /// 不做拼音匹配：那需要一张汉字→拼音的大表和多音字处理，而编辑距离已经覆盖
    /// 「一字之差」这个主要形态。真到了同音字隔着两个位置的时候再说。
    /// </summary>
    public static class NameMatcher
    {
        /// <summary>
        /// Levenshtein 编辑距离：把一个串改成另一个串，最少要几步
        /// （替换 / 插入 / 删除各算一步）。
        ///
        /// 滚动数组实现，空间 O(右串长度) 而不是 O(m×n)——名字只有 2-10 个字，
        /// 这点开销本来无所谓，但完整矩阵每次调用都要分配一个二维数组，
        /// 而匹配是**每说一句话就要对整张账户表和分类表各跑一遍**的。
        ///
        /// ⚠️ 按 <c>char</c> 比较，也就是按 UTF-16 码元。中文在基本平面里一字一码元，
        /// 所以按字算没问题；emoji 这类代理对会被算成两个，但那不会出现在账户名里。
        /// </summary>
        public static int EditDistance(string sLeft, string sRight)
        {
            if (string.IsNullOrEmpty(sLeft))
            {
                return sRight?.Length ?? 0;
            }

            if (string.IsNullOrEmpty(sRight))
            {
                return sLeft.Length;
            }

            int[] lPrevious = new int[sRight.Length + 1];
            int[] lCurrent = new int[sRight.Length + 1];

            for (int i = 0; i <= sRight.Length; i++)
            {
                lPrevious[i] = i;
            }

            for (int i = 0; i < sLeft.Length; i++)
            {
                lCurrent[0] = i + 1;

                for (int j = 0; j < sRight.Length; j++)
                {
                    int iReplace = lPrevious[j] + (sLeft[i] == sRight[j] ? 0 : 1);
                    int iRemove = lPrevious[j + 1] + 1;
                    int iInsert = lCurrent[j] + 1;

                    lCurrent[j + 1] = Math.Min(iReplace, Math.Min(iRemove, iInsert));
                }

                int[] lSwap = lPrevious;
                lPrevious = lCurrent;
                lCurrent = lSwap;
            }

            return lPrevious[sRight.Length];
        }

        /// <summary>
        /// 这么长的名字，最多允许差几个字。
        ///
        /// 阶梯是**按字数收紧**的：名字越短，改一个字的相对改动就越大。
        /// 一个字的名字改一下就完全变成另一个名字了，所以不给任何容错空间；
        /// 2-4 字允许改一个（「张三」→「张散」是本次要救的主要场景）；
        /// 5 字起才允许改两个——长名字里改一处仍然是同一个名字的可能性更高。
        /// </summary>
        public static int MaxDistanceFor(int iLength)
        {
            if (iLength <= 1)
            {
                return 0;
            }

            return iLength <= 4 ? 1 : 2;
        }

        /// <summary>
        /// 在 <paramref name="lNames"/> 里找 <paramref name="sWanted"/> 最像的那一个，
        /// 返回它的**下标**；没有把握时返回 <b>-1</b>（不抛异常）。
        ///
        /// 两条通道，顺序不能换：
        ///
        /// ① **精确全等优先。** 命中就返回，且**多个同名时取第一个**——
        ///    这与加模糊匹配之前的行为一致（那时也是线性找到第一个就返回）。
        ///    ⚠️ 这一条不能并进 ② 的「唯一候选」里：库里存了两条同名记录时，
        ///    那样会变成「并列所以不猜」，用户说了个库里确实有的名字反倒匹配不上。
        ///
        /// ② **模糊回退**，且必须同时满足四条，缺一条就不猜：
        ///    - 长度差不超过一个字的（防「招商银行支行」被截成「招商银行」）
        ///    - 距离不超 <see cref="MaxDistanceFor"/> 的
        ///    - 距离是全场最小的
        ///    - 距离最小的候选**只有一个**——两个一样近时没有任何依据说明是哪一个，
        ///      而挑错的那个会静静地填进表单
        ///
        /// 阈值按**两个名字里较短的那个**算，不是按用户说的那个：容错空间该由
        /// 信息量少的一方决定，否则「4 字候选 vs 5 字候选」这种边缘情形会拿到
        /// 更宽松的额度。
        /// </summary>
        public static int PickUniqueIndex(string sWanted, IList<string> lNames)
        {
            if (string.IsNullOrWhiteSpace(sWanted) || lNames == null || lNames.Count == 0)
            {
                return -1;
            }

            string sTarget = sWanted.Trim();

            for (int i = 0; i < lNames.Count; i++)
            {
                if (string.Equals(lNames[i], sTarget, StringComparison.Ordinal))
                {
                    return i;
                }
            }

            int iBest = -1;
            int iBestDistance = int.MaxValue;
            bool bTied = false;

            for (int i = 0; i < lNames.Count; i++)
            {
                string sName = lNames[i];
                if (string.IsNullOrEmpty(sName))
                {
                    continue;
                }

                if (Math.Abs(sName.Length - sTarget.Length) > 1)
                {
                    continue;
                }

                int iDistance = EditDistance(sTarget, sName);
                if (iDistance > MaxDistanceFor(Math.Min(sName.Length, sTarget.Length)))
                {
                    continue;
                }

                if (iDistance < iBestDistance)
                {
                    iBestDistance = iDistance;
                    iBest = i;
                    bTied = false;
                }
                else if (iDistance == iBestDistance)
                {
                    bTied = true;
                }
            }

            return bTied ? -1 : iBest;
        }
    }
}
