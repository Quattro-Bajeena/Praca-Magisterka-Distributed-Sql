using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.JSONOperations;

[SqlTest(SqlFeatureCategory.JSONOperations, "Test JSON_SET function")]
public class JsonSetTest : SqlTest
{
    protected override void SetupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = @"CREATE TABLE config (
                            id INT PRIMARY KEY,
                            settings JSON
                        )";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"INSERT INTO config VALUES (1, '{""theme"": ""dark"", ""language"": ""en""}')";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "UPDATE config SET settings = JSON_SET(settings, '$.theme', 'light') WHERE id = 1";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT JSON_EXTRACT(settings, '$.theme') FROM config WHERE id = 1";
        object? theme = cmd.ExecuteScalar();
        AssertTrue(theme != null && theme.ToString()!.Contains("light"), "Theme should be updated to 'light'");

        cmd.CommandText = "UPDATE config SET settings = JSON_SET(settings, '$.timezone', 'UTC') WHERE id = 1";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT JSON_EXTRACT(settings, '$.timezone') FROM config WHERE id = 1";
        object? tz = cmd.ExecuteScalar();
        AssertTrue(tz != null && tz.ToString()!.Contains("UTC"), "Should have new timezone key");
    }

    protected override void CleanupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE config";
        cmd.ExecuteNonQuery();
    }
}
