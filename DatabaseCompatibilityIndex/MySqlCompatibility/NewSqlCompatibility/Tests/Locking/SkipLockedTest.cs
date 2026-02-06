using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Locking;

[SqlTest(SqlFeatureCategory.Locking, "Test SKIP LOCKED ")]
public class SkipLockedTest : SqlTest
{
    protected override void SetupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "CREATE TABLE queue (id INT PRIMARY KEY, status VARCHAR(20), data VARCHAR(100))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO queue VALUES (1, 'pending', 'task1'), (2, 'pending', 'task2'), (3, 'pending', 'task3')";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "START TRANSACTION";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT * FROM queue WHERE id = 1 FOR UPDATE";
        using (DbDataReader reader = cmd.ExecuteReader())
        {
            AssertTrue(reader.Read(), "Should find row 1");
        }

        using DbCommand cmd2 = connectionSecond.CreateCommand();
        cmd2.CommandText = "SELECT id FROM queue WHERE status = 'pending' ORDER BY id LIMIT 1 FOR UPDATE SKIP LOCKED";
        object? nextId = cmd2.ExecuteScalar();
        AssertEqual(2, Convert.ToInt32(nextId!), "SKIP LOCKED should skip locked row and return next available");

        cmd.CommandText = "COMMIT";
        cmd.ExecuteNonQuery();
    }

    protected override void CleanupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS queue";
        cmd.ExecuteNonQuery();
    }
}
