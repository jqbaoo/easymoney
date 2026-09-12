using EasyMoney.Core;
using NUnit.Framework;

namespace EasyMoney.Tests
{
    public class MoneyTests
    {
        [Test]
        public void FromYuan_RoundsHalfAwayFromZero()
        {
            Assert.AreEqual(1, Money.FromYuan(0.005m).Cents);
            Assert.AreEqual(1, Money.FromYuan(0.014m).Cents);
            Assert.AreEqual(-1, Money.FromYuan(-0.005m).Cents);
        }

        [Test]
        public void FromYuan_HandlesTypicalAmounts()
        {
            Assert.AreEqual(1250, Money.FromYuan(12.50m).Cents);
            Assert.AreEqual(0, Money.FromYuan(0m).Cents);
            Assert.AreEqual(100000000, Money.FromYuan(1000000m).Cents);
        }

        [Test]
        public void Add_TenTimesPointOne_EqualsExactlyOneYuan()
        {
            Money oSum = Money.Zero;
            for (int i = 0; i < 10; i++)
            {
                oSum = oSum + Money.FromYuan(0.1m);
            }
            Assert.AreEqual(100, oSum.Cents);
            Assert.AreEqual(1.00m, oSum.ToYuan());
        }

        [Test]
        public void Subtract_ProducesNegativeMoney()
        {
            Money oResult = Money.FromYuan(10m) - Money.FromYuan(25.5m);
            Assert.AreEqual(-1550, oResult.Cents);
        }

        [Test]
        public void UnaryMinus_FlipsSign()
        {
            Assert.AreEqual(-500, (-Money.FromYuan(5m)).Cents);
        }

        [Test]
        public void Comparison_OrdersByCents()
        {
            Assert.IsTrue(Money.FromYuan(1m) < Money.FromYuan(2m));
            Assert.IsTrue(Money.FromYuan(2m) >= Money.FromYuan(2m));
            Assert.AreEqual(Money.FromYuan(3.33m), Money.FromCents(333));
        }

        [Test]
        public void ToString_AlwaysTwoDecimals()
        {
            Assert.AreEqual("12.50", Money.FromYuan(12.5m).ToString());
            Assert.AreEqual("-0.07", Money.FromCents(-7).ToString());
            Assert.AreEqual("0.00", Money.Zero.ToString());
        }
    }
}
