using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.DataTypes;

[SqlTest(SqlFeatureCategory.DataTypes, "Test VARCHAR length and storage")]
public class VarcharTypeTest : SqlTest
{
    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "CREATE TABLE varchar_test (id INT PRIMARY KEY, text VARCHAR(255))";
        cmd.ExecuteNonQuery();

        string longText = new string('a', 255);
        cmd.CommandText = $"INSERT INTO varchar_test VALUES (1, '{longText}')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT LENGTH(text) FROM varchar_test WHERE id = 1";
        object? length = cmd.ExecuteScalar();
        AssertEqual(255L, (long)length!, "VARCHAR should store full 255 chars");

        cmd.CommandText = "DROP TABLE varchar_test";
        cmd.ExecuteNonQuery();
    }
}
