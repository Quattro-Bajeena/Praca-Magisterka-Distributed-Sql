using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.IsolationLevels;

[SqlTest(SqlFeatureCategory.Transactions, "Test READ COMMITTED allows phantom reads")]
public class ReadCommittedPhantomTest : SqlTest
{
    protected override void SetupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "CREATE TABLE rc_phantom (id INT PRIMARY KEY, category VARCHAR(20), value INT)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO rc_phantom VALUES (1, 'A', 100), (2, 'A', 200)";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd1 = connection.CreateCommand();
        using DbCommand cmd2 = connectionSecond.CreateCommand();

        cmd1.CommandText = "SET SESSION TRANSACTION ISOLATION LEVEL READ COMMITTED";
        cmd1.ExecuteNonQuery();

        cmd1.CommandText = "START TRANSACTION";
        cmd1.ExecuteNonQuery();

        cmd1.CommandText = "SELECT COUNT(*) FROM rc_phantom WHERE category = 'A'";
        object? firstCount = cmd1.ExecuteScalar();
        AssertEqual(2L, Convert.ToInt64(firstCount!), "Should initially have 2 rows");

        cmd2.CommandText = "START TRANSACTION";
        cmd2.ExecuteNonQuery();

        cmd2.CommandText = "INSERT INTO rc_phantom VALUES (3, 'A', 300)";
        cmd2.ExecuteNonQuery();

        cmd2.CommandText = "COMMIT";
        cmd2.ExecuteNonQuery();

        cmd1.CommandText = "SELECT COUNT(*) FROM rc_phantom WHERE category = 'A'";
        object? secondCount = cmd1.ExecuteScalar();
        AssertEqual(3L, Convert.ToInt64(secondCount!), "Should now see 3 rows (phantom read)");

        cmd1.CommandText = "SELECT SUM(value) FROM rc_phantom WHERE category = 'A'";
        object? sum = cmd1.ExecuteScalar();
        AssertEqual(600L, Convert.ToInt64(sum!), "Sum should include new row");

        cmd1.CommandText = "COMMIT";
        cmd1.ExecuteNonQuery();
    }

    protected override void CleanupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS rc_phantom";
        cmd.ExecuteNonQuery();
    }

    protected override void SetupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "CREATE TABLE rc_phantom (id INT PRIMARY KEY, category VARCHAR(20), value INT)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO rc_phantom VALUES (1, 'A', 100), (2, 'A', 200)";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd1 = connection.CreateCommand();
        using DbCommand cmd2 = connectionSecond.CreateCommand();

        cmd1.CommandText = "BEGIN TRANSACTION ISOLATION LEVEL READ COMMITTED";
        cmd1.ExecuteNonQuery();

        cmd1.CommandText = "SELECT COUNT(*) FROM rc_phantom WHERE category = 'A'";
        object? firstCount = cmd1.ExecuteScalar();
        AssertEqual(2L, Convert.ToInt64(firstCount!), "Should initially have 2 rows");

        cmd2.CommandText = "BEGIN";
        cmd2.ExecuteNonQuery();

        cmd2.CommandText = "INSERT INTO rc_phantom VALUES (3, 'A', 300)";
        cmd2.ExecuteNonQuery();

        cmd2.CommandText = "COMMIT";
        cmd2.ExecuteNonQuery();

        cmd1.CommandText = "SELECT COUNT(*) FROM rc_phantom WHERE category = 'A'";
        object? secondCount = cmd1.ExecuteScalar();
        AssertEqual(3L, Convert.ToInt64(secondCount!), "Should now see 3 rows (phantom read)");

        cmd1.CommandText = "SELECT SUM(value) FROM rc_phantom WHERE category = 'A'";
        object? sum = cmd1.ExecuteScalar();
        AssertEqual(600L, Convert.ToInt64(sum!), "Sum should include new row");

        cmd1.CommandText = "COMMIT";
        cmd1.ExecuteNonQuery();
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS rc_phantom";
        cmd.ExecuteNonQuery();
    }
}
