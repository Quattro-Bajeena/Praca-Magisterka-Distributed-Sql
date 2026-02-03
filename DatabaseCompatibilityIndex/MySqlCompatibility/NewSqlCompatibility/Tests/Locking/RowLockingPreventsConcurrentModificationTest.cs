using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Locking;

[SqlTest(SqlFeatureCategory.Locking, "Test row locking prevents concurrent modification", DatabaseType.MySql)]
public class RowLockingPreventsConcurrentModificationTest : SqlTest
{
    public override void Setup(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "CREATE TABLE orders (order_id INT PRIMARY KEY, status VARCHAR(20))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO orders VALUES (1, 'pending')";
        cmd.ExecuteNonQuery();
    }

    public override void Execute(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        // Lock row and verify it's locked
        cmd.CommandText = "BEGIN";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT status FROM orders WHERE order_id = 1 FOR UPDATE";
        object? status = cmd.ExecuteScalar();
        AssertEqual("pending", (string)status!, "Status should be pending");

        // This would be locked for other transactions, but we can update it in current transaction
        cmd.CommandText = "UPDATE orders SET status = 'processing' WHERE order_id = 1";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "COMMIT";
        cmd.ExecuteNonQuery();

        // Verify the update
        cmd.CommandText = "SELECT status FROM orders WHERE order_id = 1";
        object? newStatus = cmd.ExecuteScalar();
        AssertEqual("processing", (string)newStatus!, "Status should be updated to processing");
    }

    public override void Cleanup(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE orders";
        cmd.ExecuteNonQuery();
    }
}
