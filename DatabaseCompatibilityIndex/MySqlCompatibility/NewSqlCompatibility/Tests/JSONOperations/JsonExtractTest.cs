using NSCI.Testing;using NSCI.Configuration;
using System.Data.Common;

namespace NSCI.Tests.JSONOperations;

[SqlTest(SqlFeatureCategory.JSONOperations, "Test JSON_EXTRACT function", DatabaseType.MySql)]
public class JsonExtractTest : SqlTest
{
    public override void Setup(DbConnection connection)
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

    public override void Execute(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        // Extract string value
        cmd.CommandText = "SELECT JSON_EXTRACT(profile, '$.name') FROM users WHERE id = 1";
        object? name = cmd.ExecuteScalar();
        AssertTrue(name != null && name.ToString()!.Contains("Alice"), "Should extract name 'Alice'");

        // Extract numeric value
        cmd.CommandText = "SELECT JSON_EXTRACT(profile, '$.age') FROM users WHERE id = 1";
        object? age = cmd.ExecuteScalar();
        AssertTrue(age != null, "Should extract age value");

        // Extract from multiple rows
        cmd.CommandText = "SELECT COUNT(*) FROM users WHERE JSON_EXTRACT(profile, '$.age') > 25";
        object? count = cmd.ExecuteScalar();
        AssertEqual(1L, (long)count!, "Should find 1 user with age > 25");
    }

    public override void Cleanup(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE users";
        cmd.ExecuteNonQuery();
    }
}
