using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.TableDefinition;

// PostgreSQL rules are a query-rewrite mechanism that transforms incoming SQL commands
// before the planner sees them. A rule is defined on a table and fires on a specific
// event (SELECT, INSERT, UPDATE, or DELETE).
//
// DO ALSO command   — runs the original command AND the extra command.
//                     Useful for audit logging: every INSERT also writes to a log table.
//
// DO INSTEAD command — replaces the original command with the alternative command.
//                      The original command is discarded entirely.
//
// DO INSTEAD NOTHING — silently discards the triggering command.
//                      Commonly used to make a table effectively read-only for a
//                      given operation (e.g., prevent DELETEs).
[SqlTest(SqlFeatureCategory.DDL, "Test CREATE RULE with ALSO and INSTEAD actions rewrites queries correctly", DatabaseType.PostgreSql)]
public class PostgresRuleTest : SqlTest
{
    protected override void SetupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = @"CREATE TABLE orders_rule (
            id    INT         NOT NULL,
            total DECIMAL(10, 2) NOT NULL
        )";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"CREATE TABLE order_log (
            order_id  INT          NOT NULL,
            logged_at TIMESTAMPTZ  NOT NULL DEFAULT NOW()
        )";
        cmd.ExecuteNonQuery();

        // ALSO rule: the original INSERT executes, and an audit INSERT is appended automatically
        cmd.CommandText = @"CREATE RULE log_order_insert AS ON INSERT TO orders_rule
                            DO ALSO INSERT INTO order_log (order_id) VALUES (NEW.id)";
        cmd.ExecuteNonQuery();

        // INSTEAD NOTHING rule: any DELETE against orders_rule is silently discarded
        cmd.CommandText = @"CREATE RULE prevent_delete AS ON DELETE TO orders_rule
                            DO INSTEAD NOTHING";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "INSERT INTO orders_rule (id, total) VALUES (1, 49.99)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO orders_rule (id, total) VALUES (2, 129.00)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO orders_rule (id, total) VALUES (3, 9.50)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM orders_rule";
        long orderCount = Convert.ToInt64(cmd.ExecuteScalar()!);
        AssertEqual(3L, orderCount, "orders_rule should contain the 3 inserted rows");

        // ALSO rule: every INSERT silently appended an audit row to order_log
        cmd.CommandText = "SELECT COUNT(*) FROM order_log";
        long logCount = Convert.ToInt64(cmd.ExecuteScalar()!);
        AssertEqual(3L, logCount, "ALSO rule should have written one audit row per INSERT");

        // Each audit row references a valid order_id
        cmd.CommandText = @"SELECT COUNT(*) FROM order_log
                            WHERE order_id IN (SELECT id FROM orders_rule)";
        long matchedLog = Convert.ToInt64(cmd.ExecuteScalar()!);
        AssertEqual(3L, matchedLog, "Every audit row should reference an existing order_id");

        // INSTEAD NOTHING rule: DELETE is silently discarded — the row count must not change
        cmd.CommandText = "DELETE FROM orders_rule WHERE id = 1";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM orders_rule";
        long afterBlockedDelete = Convert.ToInt64(cmd.ExecuteScalar()!);
        AssertEqual(3L, afterBlockedDelete, "INSTEAD NOTHING rule should silently discard the DELETE");

        // Both rules must appear in the pg_rules catalog
        cmd.CommandText = @"SELECT COUNT(*) FROM pg_rules
                            WHERE tablename = 'orders_rule'
                              AND rulename IN ('log_order_insert', 'prevent_delete')";
        long ruleCount = Convert.ToInt64(cmd.ExecuteScalar()!);
        AssertEqual(2L, ruleCount, "Both rules should be registered in pg_rules");

        // DROP RULE removes the rule; the next DELETE must now execute normally
        cmd.CommandText = "DROP RULE prevent_delete ON orders_rule";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "DELETE FROM orders_rule WHERE id = 1";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM orders_rule";
        long afterRealDelete = Convert.ToInt64(cmd.ExecuteScalar()!);
        AssertEqual(2L, afterRealDelete, "After DROP RULE, DELETE should actually remove the row");
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "DROP TABLE IF EXISTS orders_rule CASCADE";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());

        cmd.CommandText = "DROP TABLE IF EXISTS order_log CASCADE";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());
    }
}
