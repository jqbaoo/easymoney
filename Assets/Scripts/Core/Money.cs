using System;

namespace EasyMoney.Core
{
    /// <summary>
    /// 金额值类型。内部一律用「分」为单位的 long 存储，杜绝浮点误差。
    /// </summary>
    public readonly struct Money : IEquatable<Money>, IComparable<Money>
    {
        public static readonly Money Zero = new Money(0);

        private readonly long m_Cents;

        private Money(long iCents)
        {
            m_Cents = iCents;
        }

        public long Cents => m_Cents;

        public static Money FromCents(long iCents)
        {
            return new Money(iCents);
        }

        public static Money FromYuan(decimal dYuan)
        {
            decimal dCents = dYuan * 100m;
            long iCents = (long)Math.Round(dCents, 0, MidpointRounding.AwayFromZero);
            return new Money(iCents);
        }

        public decimal ToYuan()
        {
            return m_Cents / 100m;
        }

        public static Money operator +(Money oLeft, Money oRight)
        {
            return new Money(oLeft.m_Cents + oRight.m_Cents);
        }

        public static Money operator -(Money oLeft, Money oRight)
        {
            return new Money(oLeft.m_Cents - oRight.m_Cents);
        }

        public static Money operator -(Money oValue)
        {
            return new Money(-oValue.m_Cents);
        }

        public static bool operator <(Money oLeft, Money oRight) => oLeft.m_Cents < oRight.m_Cents;

        public static bool operator >(Money oLeft, Money oRight) => oLeft.m_Cents > oRight.m_Cents;

        public static bool operator <=(Money oLeft, Money oRight) => oLeft.m_Cents <= oRight.m_Cents;

        public static bool operator >=(Money oLeft, Money oRight) => oLeft.m_Cents >= oRight.m_Cents;

        public bool Equals(Money oOther) => m_Cents == oOther.m_Cents;

        public override bool Equals(object oObj) => oObj is Money oOther && Equals(oOther);

        public override int GetHashCode() => m_Cents.GetHashCode();

        public int CompareTo(Money oOther) => m_Cents.CompareTo(oOther.m_Cents);

        public override string ToString() => ToYuan().ToString("0.00");
    }
}
