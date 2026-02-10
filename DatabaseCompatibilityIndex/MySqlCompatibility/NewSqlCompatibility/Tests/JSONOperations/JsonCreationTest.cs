using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.JSONOperations;

[SqlTest(SqlFeatureCategory.JSONOperations, "Test JSON_ARRAY and JSON_OBJECT functions")]
public class JsonCreationTest : SqlTest
{
    protected override void SetupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = @"CREATE TABLE json_data (
                            id INT PRIMARY KEY AUTO_INCREMENT,
                            data JSON
                        )";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "INSERT INTO json_data (data) VALUES (JSON_ARRAY(1, 2, 3, 'four'))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT JSON_EXTRACT(data, '$[0]') FROM json_data WHERE id = 1";
        object? value = cmd.ExecuteScalar();
        AssertTrue(value != null, "Should have created JSON array");

        cmd.CommandText = "INSERT INTO json_data (data) VALUES (JSON_OBJECT('key1', 'value1', 'key2', 123))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT JSON_EXTRACT(data, '$.key1') FROM json_data WHERE id = 2";
        object? objValue = cmd.ExecuteScalar();
        AssertTrue(objValue != null && objValue.ToString()!.Contains("value1"), "Should have created JSON object");
    }

    protected override void CleanupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE json_data";
        cmd.ExecuteNonQuery();
    }

    protected override void SetupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = @"CREATE TABLE json_data (
                            id SERIAL PRIMARY KEY,
                            data JSONB
                        )";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "INSERT INTO json_data (data) VALUES (jsonb_build_array(1, 2, 3, 'four'))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT data->0 FROM json_data WHERE id = 1";
        object? value = cmd.ExecuteScalar();
        AssertTrue(value != null, "Should have created JSON array");

        cmd.CommandText = "INSERT INTO json_data (data) VALUES (jsonb_build_object('key1', 'value1', 'key2', 123))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT data->>'key1' FROM json_data WHERE id = 2";
        object? objValue = cmd.ExecuteScalar();
        AssertTrue(objValue != null && objValue.ToString()!.Contains("value1"), "Should have created JSON object");
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE json_data";
        cmd.ExecuteNonQuery();
    }
}
