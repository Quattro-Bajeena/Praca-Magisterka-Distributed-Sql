using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.DataTypes;

[SqlTest(SqlFeatureCategory.DataTypes, "Test JSON data type (if supported)", DatabaseType.MySql)]
public class JsonTypeTest : SqlTest
{
    public override void Execute(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "CREATE TABLE json_test (id INT PRIMARY KEY, data JSON)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"INSERT INTO json_test VALUES (1, '{""name"": ""John"", ""age"": 30}')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM json_test";
        object? count = cmd.ExecuteScalar();
        AssertEqual(1L, (long)count!, "JSON type should work");

        cmd.CommandText = "DROP TABLE json_test";
        cmd.ExecuteNonQuery();
    }
}
