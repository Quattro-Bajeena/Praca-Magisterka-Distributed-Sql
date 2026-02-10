using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.DataTypes;

[SqlTest(SqlFeatureCategory.DataTypes, "Test JSON data type")]
public class JsonTypeTest : SqlTest
{
    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "CREATE TABLE json_test (id INT PRIMARY KEY, data JSON)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"INSERT INTO json_test VALUES (1, '{""name"": ""John"", ""age"": 30}')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"INSERT INTO json_test VALUES (2, '{""name"": ""Jane"", ""age"": 25, ""city"": ""NYC""}')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM json_test";
        object? count = cmd.ExecuteScalar();
        AssertEqual(2L, (long)count!, "JSON type should work");

        cmd.CommandText = "SELECT data FROM json_test WHERE id = 1";
        object? jsonData = cmd.ExecuteScalar();
        AssertTrue(jsonData != null && jsonData.ToString()!.Contains("John"), "Should retrieve JSON data");

        cmd.CommandText = "DROP TABLE json_test";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "CREATE TABLE json_test (id INT PRIMARY KEY, data JSON, data_binary JSONB)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"INSERT INTO json_test VALUES (1, '{""name"": ""John"", ""age"": 30}', '{""name"": ""John"", ""age"": 30}')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"INSERT INTO json_test VALUES (2, '{""name"": ""Jane"", ""age"": 25, ""city"": ""NYC""}', '{""name"": ""Jane"", ""age"": 25, ""city"": ""NYC""}')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM json_test";
        object? count = cmd.ExecuteScalar();
        AssertEqual(2L, (long)count!, "JSON type should work");

        cmd.CommandText = "SELECT data FROM json_test WHERE id = 1";
        object? jsonData = cmd.ExecuteScalar();
        AssertTrue(jsonData != null && jsonData.ToString()!.Contains("John"), "Should retrieve JSON data");

        cmd.CommandText = "SELECT data_binary->>'name' FROM json_test WHERE id = 1";
        object? name = cmd.ExecuteScalar();
        AssertEqual("John", name?.ToString(), "JSONB operator should extract name");

        cmd.CommandText = "SELECT data_binary->'age' FROM json_test WHERE id = 2";
        object? age = cmd.ExecuteScalar();
        AssertTrue(age != null && age.ToString()!.Contains("25"), "JSONB operator should extract age");

        cmd.CommandText = "SELECT COUNT(*) FROM json_test WHERE data_binary @> '{\"city\": \"NYC\"}'";
        count = cmd.ExecuteScalar();
        AssertEqual(1L, (long)count!, "JSONB containment operator should work");

        cmd.CommandText = "DROP TABLE json_test";
        cmd.ExecuteNonQuery();
    }
}
