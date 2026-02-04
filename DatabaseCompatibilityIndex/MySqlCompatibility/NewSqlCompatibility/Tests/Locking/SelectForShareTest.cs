using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Locking;

[SqlTest(SqlFeatureCategory.Locking, "Test SELECT...FOR SHARE (shared lock)")]
public class SelectForShareTest : SqlTest
{
    protected override void SetupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "CREATE TABLE inventory (product_id INT PRIMARY KEY, stock INT)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO inventory VALUES (1, 100)";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "BEGIN";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT stock FROM inventory WHERE product_id = 1 FOR SHARE";
        object? stock = cmd.ExecuteScalar();
        AssertEqual(100, (int)stock!, "Should read shared-locked row");

        cmd.CommandText = "COMMIT";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT stock FROM inventory WHERE product_id = 1";
        object? finalStock = cmd.ExecuteScalar();
        AssertEqual(100, (int)finalStock!, "Stock should remain unchanged");
    }

    protected override void CleanupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE inventory";
        cmd.ExecuteNonQuery();
    }
}
