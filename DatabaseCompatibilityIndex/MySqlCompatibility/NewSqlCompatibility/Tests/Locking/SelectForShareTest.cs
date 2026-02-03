using NSCI.Testing;using NSCI.Configuration;
using System.Data.Common;

namespace NSCI.Tests.Locking;

[SqlTest(SqlFeatureCategory.Locking, "Test SELECT...FOR SHARE (shared lock)", DatabaseType.MySql)]
public class SelectForShareTest : SqlTest
{
    public override void Setup(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "CREATE TABLE inventory (product_id INT PRIMARY KEY, stock INT)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO inventory VALUES (1, 100)";
        cmd.ExecuteNonQuery();
    }

    public override void Execute(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        // Lock row for shared access
        cmd.CommandText = "BEGIN";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT stock FROM inventory WHERE product_id = 1 FOR SHARE";
        object? stock = cmd.ExecuteScalar();
        AssertEqual(100L, (long)stock!, "Should read shared-locked row");

        cmd.CommandText = "COMMIT";
        cmd.ExecuteNonQuery();

        // Verify data integrity
        cmd.CommandText = "SELECT stock FROM inventory WHERE product_id = 1";
        object? finalStock = cmd.ExecuteScalar();
        AssertEqual(100L, (long)finalStock!, "Stock should remain unchanged");
    }

    public override void Cleanup(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE inventory";
        cmd.ExecuteNonQuery();
    }
}
