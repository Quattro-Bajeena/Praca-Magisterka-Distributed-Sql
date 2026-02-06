using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Misc;

[SqlTest(SqlFeatureCategory.Misc, "Test XA Transaction syntax ")]
public class XATransactionTest : SqlTest
{
    protected override void SetupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "CREATE TABLE xa_test (id INT PRIMARY KEY, value INT)";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "XA START 'test_xa_1'";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO xa_test VALUES (1, 100)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "XA END 'test_xa_1'";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "XA PREPARE 'test_xa_1'";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "XA COMMIT 'test_xa_1'";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT value FROM xa_test WHERE id = 1";
        object? result = cmd.ExecuteScalar();
        AssertEqual(100, Convert.ToInt32(result!), "XA transaction should have committed the data");
    }

    protected override void CleanupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS xa_test";
        cmd.ExecuteNonQuery();
    }
}
