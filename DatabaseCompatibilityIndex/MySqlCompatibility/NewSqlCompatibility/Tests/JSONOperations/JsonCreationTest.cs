using NSCI.Testing;using NSCI.Configuration;
using System.Data.Common;

namespace NSCI.Tests.JSONOperations;

[SqlTest(SqlFeatureCategory.JSONOperations, "Test JSON_ARRAY and JSON_OBJECT functions", DatabaseType.MySql)]
public class JsonCreationTest : SqlTest
{
    public override void Setup(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = @"CREATE TABLE json_data (
                            id INT PRIMARY KEY AUTO_INCREMENT,
                            data JSON
                        )";
        cmd.ExecuteNonQuery();
    }

    public override void Execute(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        // Create JSON array
        cmd.CommandText = "INSERT INTO json_data (data) VALUES (JSON_ARRAY(1, 2, 3, 'four'))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT JSON_EXTRACT(data, '$[0]') FROM json_data WHERE id = 1";
        object? value = cmd.ExecuteScalar();
        AssertTrue(value != null, "Should have created JSON array");

        // Create JSON object
        cmd.CommandText = "INSERT INTO json_data (data) VALUES (JSON_OBJECT('key1', 'value1', 'key2', 123))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT JSON_EXTRACT(data, '$.key1') FROM json_data WHERE id = 2";
        object? objValue = cmd.ExecuteScalar();
        AssertTrue(objValue != null && objValue.ToString()!.Contains("value1"), "Should have created JSON object");
    }

    public override void Cleanup(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE json_data";
        cmd.ExecuteNonQuery();
    }
}
