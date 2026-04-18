using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Transactions;

[SqlTest(SqlFeatureCategory.Transactions, "Test PostgreSQL subtransactions with nested savepoints", DatabaseType.PostgreSql)]
public class PostgresNestedSavepointsTest : SqlTest
{
    protected override void SetupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = @"CREATE TABLE savepoint_test (
                            id INT PRIMARY KEY,
                            value VARCHAR(100)
                        )";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "BEGIN";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO savepoint_test VALUES (1, 'committed')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SAVEPOINT sp1";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO savepoint_test VALUES (2, 'rolled back')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "ROLLBACK TO SAVEPOINT sp1";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM savepoint_test";
        object? count = cmd.ExecuteScalar();
        AssertEqual(1L, Convert.ToInt64(count!), "Should have 1 row after rollback to savepoint");

        cmd.CommandText = "SAVEPOINT sp2";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO savepoint_test VALUES (3, 'also committed')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "RELEASE SAVEPOINT sp2";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "COMMIT";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM savepoint_test";
        object? finalCount = cmd.ExecuteScalar();
        AssertEqual(2L, Convert.ToInt64(finalCount!), "Should have 2 rows after commit");
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS savepoint_test CASCADE";
        cmd.ExecuteNonQuery();
    }
}
