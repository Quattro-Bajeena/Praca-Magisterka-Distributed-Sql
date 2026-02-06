using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.DataTypes;

[SqlTest(SqlFeatureCategory.DataTypes, "Test BOOLEAN type (TINYINT)")]
public class BooleanTypeTest : SqlTest
{
    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "CREATE TABLE bool_test (id INT PRIMARY KEY, is_active BOOLEAN)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO bool_test VALUES (1, TRUE), (2, FALSE), (3, TRUE)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM bool_test WHERE is_active = TRUE";
        object? count = cmd.ExecuteScalar();
        AssertEqual(2L, (long)count!, "BOOLEAN filtering should work");

        cmd.CommandText = "DROP TABLE bool_test";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "CREATE TABLE bool_test (id INT PRIMARY KEY, is_active BOOLEAN)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO bool_test VALUES (1, TRUE), (2, FALSE), (3, TRUE)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM bool_test WHERE is_active = TRUE";
        object? count = cmd.ExecuteScalar();
        AssertEqual(2L, (long)count!, "BOOLEAN filtering should work");

        cmd.CommandText = "DROP TABLE bool_test";
        cmd.ExecuteNonQuery();
    }
}
