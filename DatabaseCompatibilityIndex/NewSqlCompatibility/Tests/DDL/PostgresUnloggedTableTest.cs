using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.TableDefinition;

[SqlTest(SqlFeatureCategory.DDL, "Test UNLOGGED table creation that skips WAL", DatabaseType.PostgreSql)]
public class PostgresUnloggedTableTest : SqlTest
{
    protected override void SetupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = @"CREATE UNLOGGED TABLE unlogged_events (
            id SERIAL PRIMARY KEY,
            event_name TEXT NOT NULL,
            occurred_at TIMESTAMPTZ DEFAULT NOW()
        )";
        cmd.ExecuteNonQuery();

        string[] inserts =
        [
            "('login')",
            "('logout')",
            "('purchase')",
        ];

        foreach (string values in inserts)
        {
            cmd.CommandText = $"INSERT INTO unlogged_events (event_name) VALUES {values}";
            cmd.ExecuteNonQuery();
        }
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "SELECT COUNT(*) FROM unlogged_events";
        long count = Convert.ToInt64(cmd.ExecuteScalar()!);
        AssertEqual(3L, count, "Unlogged table should contain 3 rows");

        cmd.CommandText = "SELECT event_name FROM unlogged_events WHERE id = 1";
        object? name = cmd.ExecuteScalar();
        AssertEqual("login", name?.ToString(), "First event should be 'login'");

        cmd.CommandText = "SELECT relpersistence FROM pg_class WHERE oid = 'unlogged_events'::regclass";
        object? persistence = cmd.ExecuteScalar();
        AssertEqual("u", persistence?.ToString(), "Table relpersistence should be 'u' (unlogged)");
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS unlogged_events CASCADE";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());
    }
}
