using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.IsolationLevels;

[SqlTest(SqlFeatureCategory.Transactions, "Test REPEATABLE READ prevents phantom reads in range")]
public class RepeatableReadPhantomPreventionTest : SqlTest
{
    protected override void SetupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "CREATE TABLE rr_phantom (id INT PRIMARY KEY, category VARCHAR(20), value INT)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO rr_phantom VALUES (1, 'A', 100), (2, 'A', 200)";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd1 = connection.CreateCommand();
        using DbCommand cmd2 = connectionSecond.CreateCommand();

        cmd1.CommandText = "SET SESSION TRANSACTION ISOLATION LEVEL REPEATABLE READ";
        cmd1.ExecuteNonQuery();

        cmd1.CommandText = "START TRANSACTION";
        cmd1.ExecuteNonQuery();

        cmd1.CommandText = "SELECT COUNT(*) FROM rr_phantom WHERE category = 'A'";
        object? firstCount = cmd1.ExecuteScalar();
        AssertEqual(2L, Convert.ToInt64(firstCount!), "Should initially have 2 rows");

        cmd2.CommandText = "START TRANSACTION";
        cmd2.ExecuteNonQuery();

        cmd2.CommandText = "INSERT INTO rr_phantom VALUES (3, 'A', 300)";
        cmd2.ExecuteNonQuery();

        cmd2.CommandText = "COMMIT";
        cmd2.ExecuteNonQuery();

        cmd1.CommandText = "SELECT COUNT(*) FROM rr_phantom WHERE category = 'A'";
        object? secondCount = cmd1.ExecuteScalar();
        AssertEqual(2L, Convert.ToInt64(secondCount!), "Should still see 2 rows (no phantom)");

        cmd1.CommandText = "SELECT SUM(value) FROM rr_phantom WHERE category = 'A'";
        object? sum = cmd1.ExecuteScalar();
        AssertEqual(300L, Convert.ToInt64(sum!), "Sum should not include new row");

        cmd1.CommandText = "COMMIT";
        cmd1.ExecuteNonQuery();

        cmd1.CommandText = "SELECT COUNT(*) FROM rr_phantom WHERE category = 'A'";
        object? finalCount = cmd1.ExecuteScalar();
        AssertEqual(3L, Convert.ToInt64(finalCount!), "After commit should see 3 rows");
    }

    protected override void CleanupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS rr_phantom";
        cmd.ExecuteNonQuery();
    }
}
