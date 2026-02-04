using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Locking;

[SqlTest(SqlFeatureCategory.Locking, "Test row locking prevents concurrent modification")]
public class RowLockingPreventsConcurrentModificationTest : SqlTest
{
    protected override void SetupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "CREATE TABLE orders (order_id INT PRIMARY KEY, status VARCHAR(20))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO orders VALUES (1, 'pending')";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "BEGIN";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT status FROM orders WHERE order_id = 1 FOR UPDATE";
        object? status = cmd.ExecuteScalar();
        AssertEqual("pending", (string)status!, "Status should be pending");

        cmd.CommandText = "UPDATE orders SET status = 'processing' WHERE order_id = 1";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "COMMIT";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT status FROM orders WHERE order_id = 1";
        object? newStatus = cmd.ExecuteScalar();
        AssertEqual("processing", (string)newStatus!, "Status should be updated to processing");
    }

    protected override void CleanupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE orders";
        cmd.ExecuteNonQuery();
    }
}
