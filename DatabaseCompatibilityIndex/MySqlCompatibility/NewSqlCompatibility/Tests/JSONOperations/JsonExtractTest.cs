using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.JSONOperations;

[SqlTest(SqlFeatureCategory.JSONOperations, "Test JSON_EXTRACT function")]
public class JsonExtractTest : SqlTest
{
    protected override void SetupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = @"CREATE TABLE users (
                            id INT PRIMARY KEY,
                            profile JSON
                        )";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"INSERT INTO users VALUES (1, '{""name"": ""Alice"", ""age"": 30, ""city"": ""New York""}')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"INSERT INTO users VALUES (2, '{""name"": ""Bob"", ""age"": 25, ""city"": ""San Francisco""}')";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "SELECT JSON_EXTRACT(profile, '$.name') FROM users WHERE id = 1";
        object? name = cmd.ExecuteScalar();
        AssertTrue(name != null && name.ToString()!.Contains("Alice"), "Should extract name 'Alice'");

        cmd.CommandText = "SELECT JSON_EXTRACT(profile, '$.age') FROM users WHERE id = 1";
        object? age = cmd.ExecuteScalar();
        AssertTrue(age != null, "Should extract age value");

        cmd.CommandText = "SELECT COUNT(*) FROM users WHERE JSON_EXTRACT(profile, '$.age') > 25";
        object? count = cmd.ExecuteScalar();
        AssertEqual(1L, (long)count!, "Should find 1 user with age > 25");
    }

    protected override void CleanupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE users";
        cmd.ExecuteNonQuery();
    }
}
