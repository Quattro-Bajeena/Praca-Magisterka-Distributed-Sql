using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Partitioning;

[SqlTest(SqlFeatureCategory.Partitioning, "Test LIST partitioning")]
public class ListPartitioningTest : SqlTest
{
    protected override void SetupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = @"CREATE TABLE user_activities (
                            id INT,
                            username VARCHAR(100),
                            region VARCHAR(50)
                        ) PARTITION BY LIST COLUMNS(region) (
                            PARTITION p_us VALUES IN ('CA', 'NY', 'TX'),
                            PARTITION p_eu VALUES IN ('UK', 'DE', 'FR'),
                            PARTITION p_other VALUES IN ('JP', 'AU', 'BR')
                        )";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "INSERT INTO user_activities VALUES (1, 'alice', 'CA')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO user_activities VALUES (2, 'bob', 'UK')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO user_activities VALUES (3, 'charlie', 'JP')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM user_activities WHERE region IN ('CA', 'NY', 'TX')";
        object? usCount = cmd.ExecuteScalar();
        AssertEqual(1L, Convert.ToInt64(usCount!), "Should have 1 US user");

        cmd.CommandText = "SELECT COUNT(*) FROM user_activities WHERE region IN ('UK', 'DE', 'FR')";
        object? euCount = cmd.ExecuteScalar();
        AssertEqual(1L, Convert.ToInt64(euCount!), "Should have 1 EU user");

        cmd.CommandText = "SELECT COUNT(*) FROM user_activities WHERE region IN ('JP', 'AU', 'BR')";
        object? otherCount = cmd.ExecuteScalar();
        AssertEqual(1L, Convert.ToInt64(otherCount!), "Should have 1 other region user");
    }

    protected override string? CleanupCommandMy => "DROP TABLE user_activities";
}
