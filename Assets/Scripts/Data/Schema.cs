namespace EasyMoney.Data
{
    /// <summary>
    /// 建表 SQL。全部 IF NOT EXISTS，可重复执行。
    /// 金额列一律 INTEGER 存「分」，时间列一律 INTEGER 存 Unix 毫秒。
    /// </summary>
    internal static class Schema
    {
        public const string CREATE_SCHEMA_VERSION = @"
CREATE TABLE IF NOT EXISTS schema_version (
    version INTEGER NOT NULL
);";

        public const string CREATE_ACCOUNT = @"
CREATE TABLE IF NOT EXISTS account (
    id              INTEGER PRIMARY KEY AUTOINCREMENT,
    name            TEXT    NOT NULL,
    type            INTEGER NOT NULL DEFAULT 0,
    initial_balance INTEGER NOT NULL DEFAULT 0,
    is_archived     INTEGER NOT NULL DEFAULT 0,
    sort_order      INTEGER NOT NULL DEFAULT 0,
    created_at      INTEGER NOT NULL DEFAULT 0
);";

        public const string CREATE_CATEGORY = @"
CREATE TABLE IF NOT EXISTS category (
    id         INTEGER PRIMARY KEY AUTOINCREMENT,
    name       TEXT    NOT NULL,
    kind       INTEGER NOT NULL DEFAULT 0,
    parent_id  INTEGER NOT NULL DEFAULT 0,
    icon_name  TEXT    NOT NULL DEFAULT '',
    sort_order INTEGER NOT NULL DEFAULT 0,
    is_system  INTEGER NOT NULL DEFAULT 0
);";

        public const string CREATE_TX = @"
CREATE TABLE IF NOT EXISTS tx (
    id            INTEGER PRIMARY KEY AUTOINCREMENT,
    type          INTEGER NOT NULL DEFAULT 0,
    amount        INTEGER NOT NULL DEFAULT 0,
    account_id    INTEGER NOT NULL DEFAULT 0,
    to_account_id INTEGER NOT NULL DEFAULT 0,
    category_id   INTEGER NOT NULL DEFAULT 0,
    note          TEXT    NOT NULL DEFAULT '',
    occurred_at   INTEGER NOT NULL DEFAULT 0,
    created_at    INTEGER NOT NULL DEFAULT 0,
    updated_at    INTEGER NOT NULL DEFAULT 0
);";

        public const string CREATE_INDEXES = @"
CREATE INDEX IF NOT EXISTS idx_tx_occurred  ON tx(occurred_at);
CREATE INDEX IF NOT EXISTS idx_tx_account   ON tx(account_id);
CREATE INDEX IF NOT EXISTS idx_tx_toaccount ON tx(to_account_id);
CREATE INDEX IF NOT EXISTS idx_tx_category  ON tx(category_id);
CREATE INDEX IF NOT EXISTS idx_category_kind ON category(kind);
";
    }
}
