using EasyMoney.Data;

namespace EasyMoney.Tests
{
    /// <summary>
    /// 测试辅助：建一个内存 SQLite 库。内存库跑在进程内，
    /// 每个测试拿到全新的空库，互不干扰，也不需要清理文件。
    /// </summary>
    public static class TestDb
    {
        public static EasyMoneyDb Create()
        {
            EasyMoneyDb oDb = new EasyMoneyDb(":memory:");
            oDb.Open();
            return oDb;
        }
    }
}
