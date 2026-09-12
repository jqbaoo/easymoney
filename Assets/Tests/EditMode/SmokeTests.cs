using NUnit.Framework;

namespace EasyMoney.Tests
{
    public class SmokeTests
    {
        [Test]
        public void TestRunner_IsWiredUp()
        {
            Assert.AreEqual(2, 1 + 1);
        }
    }
}
