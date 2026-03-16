using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Performance;

[SqlTest(SqlFeatureCategory.PerformanceHints, "Test PostgreSQL query planner settings", DatabaseType.PostgreSql)]
public class PostgresQueryPlannerSettingsTest : SqlTest
{
    protected override void SetupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = @"CREATE TABLE planner_test (
                            id INT PRIMARY KEY,
                            name VARCHAR(100),
                            category VARCHAR(50),
                            value INT
                        )";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "CREATE INDEX idx_category ON planner_test(category)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "CREATE INDEX idx_value ON planner_test(value)";
        cmd.ExecuteNonQuery();

        for (int i = 1; i <= 100; i++)
        {
            cmd.CommandText = $"INSERT INTO planner_test VALUES ({i}, 'item{i}', 'cat{i % 10}', {i * 10})";
            cmd.ExecuteNonQuery();
        }
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "SHOW enable_seqscan";
        object? seqscan = cmd.ExecuteScalar();
        AssertTrue(seqscan != null, "Should be able to read enable_seqscan setting");

        cmd.CommandText = "SET LOCAL enable_seqscan = OFF";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM planner_test WHERE category = 'cat1'";
        object? count = cmd.ExecuteScalar();
        AssertTrue(count != null, "Query should work with seqscan disabled");

        cmd.CommandText = "SET LOCAL enable_indexscan = ON";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM planner_test WHERE value > 500";
        count = cmd.ExecuteScalar();
        AssertTrue(count != null, "Query should work with indexscan enabled");

        cmd.CommandText = "SHOW random_page_cost";
        object? cost = cmd.ExecuteScalar();
        AssertTrue(cost != null, "Should be able to read random_page_cost setting");

        cmd.CommandText = "SET LOCAL random_page_cost = 1.5";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM planner_test";
        count = cmd.ExecuteScalar();
        AssertEqual(100L, Convert.ToInt64(count!), "Should have 100 rows");
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS planner_test CASCADE";
        cmd.ExecuteNonQuery();
    }
}
