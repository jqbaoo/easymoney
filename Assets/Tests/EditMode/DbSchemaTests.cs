using System.Collections.Generic;
using EasyMoney.Data;
using NUnit.Framework;
using SQLite;

namespace EasyMoney.Tests
{
    public class DbSchemaTests
    {
        [Test]
        public void Open_CreatesAllThreeTables()
        {
            using (EasyMoneyDb oDb = TestDb.Create())
            {
                List<TableName> lTables = oDb.Connection.Query<TableName>(
                    "SELECT name AS Name FROM sqlite_master WHERE type='table' ORDER BY name");

                List<string> lNames = new List<string>();
                foreach (TableName oRow in lTables)
                {
                    lNames.Add(oRow.Name);
                }

                Assert.Contains("account", lNames);
                Assert.Contains("category", lNames);
                Assert.Contains("tx", lNames);
                Assert.Contains("schema_version", lNames);
            }
        }

        [Test]
        public void Open_SetsSchemaVersion()
        {
            using (EasyMoneyDb oDb = TestDb.Create())
            {
                int iVersion = oDb.Connection.ExecuteScalar<int>(
                    "SELECT version FROM schema_version LIMIT 1");
                Assert.AreEqual(EasyMoneyDb.SCHEMA_VERSION, iVersion);
            }
        }

        [Test]
        public void Open_IsIdempotent()
        {
            using (EasyMoneyDb oDb = TestDb.Create())
            {
                oDb.Open();
                oDb.Open();
                int iCount = oDb.Connection.ExecuteScalar<int>("SELECT COUNT(*) FROM schema_version");
                Assert.AreEqual(1, iCount);
            }
        }

        [Test]
        public void Tx_TableHasOccurredAtIndex()
        {
            using (EasyMoneyDb oDb = TestDb.Create())
            {
                int iCount = oDb.Connection.ExecuteScalar<int>(
                    "SELECT COUNT(*) FROM sqlite_master WHERE type='index' AND name='idx_tx_occurred'");
                Assert.AreEqual(1, iCount);
            }
        }

        private class TableName
        {
            public string Name { get; set; }
        }
    }
}
