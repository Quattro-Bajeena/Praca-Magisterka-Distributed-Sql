using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.IsolationLevels;

[SqlTest(SqlFeatureCategory.Transactions, "Test REPEATABLE READ snapshot consistency")]
public class RepeatableReadSnapshotTest : SqlTest
{
    protected override void SetupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "CREATE TABLE rr_snapshot (id INT PRIMARY KEY, value INT)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO rr_snapshot VALUES (1, 100), (2, 200), (3, 300)";
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

        cmd1.CommandText = "SELECT value FROM rr_snapshot WHERE id = 1";
        object? firstRead = cmd1.ExecuteScalar();
        AssertEqual(100, Convert.ToInt32(firstRead!), "First read should see initial value");

        cmd2.CommandText = "START TRANSACTION";
        cmd2.ExecuteNonQuery();

        cmd2.CommandText = "UPDATE rr_snapshot SET value = 999 WHERE id = 1";
        cmd2.ExecuteNonQuery();

        cmd2.CommandText = "COMMIT";
        cmd2.ExecuteNonQuery();

        cmd1.CommandText = "SELECT value FROM rr_snapshot WHERE id = 1";
        object? secondRead = cmd1.ExecuteScalar();
        AssertEqual(100, Convert.ToInt32(secondRead!), "Second read should still see snapshot value");

        cmd1.CommandText = "SELECT SUM(value) FROM rr_snapshot";
        object? sum1 = cmd1.ExecuteScalar();
        AssertEqual(600L, Convert.ToInt64(sum1!), "Aggregate should use consistent snapshot");

        cmd1.CommandText = "SELECT value FROM rr_snapshot WHERE id = 2";
        object? read2 = cmd1.ExecuteScalar();
        AssertEqual(200, Convert.ToInt32(read2!), "All reads within transaction see same snapshot");

        cmd1.CommandText = "COMMIT";
        cmd1.ExecuteNonQuery();

        cmd1.CommandText = "SELECT value FROM rr_snapshot WHERE id = 1";
        object? afterCommit = cmd1.ExecuteScalar();
        AssertEqual(999, Convert.ToInt32(afterCommit!), "After commit should see new value");
    }

    protected override void CleanupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS rr_snapshot";
        cmd.ExecuteNonQuery();
    }

    protected override void SetupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "CREATE TABLE rr_snapshot (id INT PRIMARY KEY, value INT)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO rr_snapshot VALUES (1, 100), (2, 200), (3, 300)";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd1 = connection.CreateCommand();
        using DbCommand cmd2 = connectionSecond.CreateCommand();

        cmd1.CommandText = "SET TRANSACTION ISOLATION LEVEL REPEATABLE READ";
        cmd1.ExecuteNonQuery();

        cmd1.CommandText = "BEGIN";
        cmd1.ExecuteNonQuery();

        cmd1.CommandText = "SELECT value FROM rr_snapshot WHERE id = 1";
        object? firstRead = cmd1.ExecuteScalar();
        AssertEqual(100, Convert.ToInt32(firstRead!), "First read should see initial value");

        cmd2.CommandText = "BEGIN";
        cmd2.ExecuteNonQuery();

        cmd2.CommandText = "UPDATE rr_snapshot SET value = 999 WHERE id = 1";
        cmd2.ExecuteNonQuery();

        cmd2.CommandText = "COMMIT";
        cmd2.ExecuteNonQuery();

        cmd1.CommandText = "SELECT value FROM rr_snapshot WHERE id = 1";
        object? secondRead = cmd1.ExecuteScalar();
        AssertEqual(100, Convert.ToInt32(secondRead!), "Second read should still see snapshot value");

        cmd1.CommandText = "SELECT SUM(value) FROM rr_snapshot";
        object? sum1 = cmd1.ExecuteScalar();
        AssertEqual(600L, Convert.ToInt64(sum1!), "Aggregate should use consistent snapshot");

        cmd1.CommandText = "SELECT value FROM rr_snapshot WHERE id = 2";
        object? read2 = cmd1.ExecuteScalar();
        AssertEqual(200, Convert.ToInt32(read2!), "All reads within transaction see same snapshot");

        cmd1.CommandText = "COMMIT";
        cmd1.ExecuteNonQuery();

        cmd1.CommandText = "SELECT value FROM rr_snapshot WHERE id = 1";
        object? afterCommit = cmd1.ExecuteScalar();
        AssertEqual(999, Convert.ToInt32(afterCommit!), "After commit should see new value");
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS rr_snapshot";
        cmd.ExecuteNonQuery();
    }
}
