using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.IsolationLevels;

[SqlTest(SqlFeatureCategory.Transactions, "Test READ UNCOMMITTED allows dirty reads", DatabaseType.MySql)]
public class ReadUncommittedDirtyReadTest : SqlTest
{
    protected override void SetupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "CREATE TABLE ru_dirty (id INT PRIMARY KEY, value INT)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO ru_dirty VALUES (1, 100)";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd1 = connection.CreateCommand();
        using DbCommand cmd2 = connectionSecond.CreateCommand();

        cmd1.CommandText = "SET SESSION TRANSACTION ISOLATION LEVEL READ UNCOMMITTED";
        cmd1.ExecuteNonQuery();

        cmd2.CommandText = "START TRANSACTION";
        cmd2.ExecuteNonQuery();

        cmd2.CommandText = "UPDATE ru_dirty SET value = 999 WHERE id = 1";
        cmd2.ExecuteNonQuery();

        cmd1.CommandText = "START TRANSACTION";
        cmd1.ExecuteNonQuery();

        cmd1.CommandText = "SELECT value FROM ru_dirty WHERE id = 1";
        object? dirtyRead = cmd1.ExecuteScalar();
        AssertEqual(999, Convert.ToInt32(dirtyRead!), "Should see uncommitted value");

        cmd2.CommandText = "ROLLBACK";
        cmd2.ExecuteNonQuery();

        cmd1.CommandText = "SELECT value FROM ru_dirty WHERE id = 1";
        object? afterRollback = cmd1.ExecuteScalar();
        AssertEqual(100, Convert.ToInt32(afterRollback!), "Should see original value after rollback");

        cmd1.CommandText = "COMMIT";
        cmd1.ExecuteNonQuery();
    }

    protected override void CleanupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS ru_dirty";
        cmd.ExecuteNonQuery();
    }
}
