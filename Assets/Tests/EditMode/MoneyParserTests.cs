using EasyMoney.Core;
using NUnit.Framework;

namespace EasyMoney.Tests
{
    public class MoneyParserTests
    {
        [TestCase("12.5", 1250)]
        [TestCase("12.50", 1250)]
        [TestCase("12", 1200)]
        [TestCase("0.01", 1)]
        [TestCase(" 8.8 ", 880)]
        [TestCase("1000000", 100000000)]
        [TestCase("0", 0)]
        public void TryParseYuan_AcceptsValidInput(string sInput, long iExpectedCents)
        {
            bool bOk = MoneyParser.TryParseYuan(sInput, out Money oMoney);
            Assert.IsTrue(bOk, $"应当解析成功: {sInput}");
            Assert.AreEqual(iExpectedCents, oMoney.Cents);
        }

        [TestCase("")]
        [TestCase("   ")]
        [TestCase("abc")]
        [TestCase("1.2.3")]
        [TestCase("1,000")]
        [TestCase("--5")]
        [TestCase("1e3")]
        [TestCase(null)]
        public void TryParseYuan_RejectsInvalidInput(string sInput)
        {
            bool bOk = MoneyParser.TryParseYuan(sInput, out Money oMoney);
            Assert.IsFalse(bOk, $"应当解析失败: {sInput}");
            Assert.AreEqual(0, oMoney.Cents);
        }

        [Test]
        public void TryParseYuan_AcceptsNegative()
        {
            Assert.IsTrue(MoneyParser.TryParseYuan("-3.5", out Money oMoney));
            Assert.AreEqual(-350, oMoney.Cents);
        }

        [Test]
        public void TryParseYuan_RejectsMoreThanTwoDecimals()
        {
            Assert.IsFalse(MoneyParser.TryParseYuan("1.234", out _));
        }
    }
}
