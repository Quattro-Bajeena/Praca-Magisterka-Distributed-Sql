using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Performance;

[SqlTest(SqlFeatureCategory.PerformanceHints, "Test PostgreSQL CTE materialization hints", DatabaseType.PostgreSql)]
public class PostgresCTEMaterializationTest : SqlTest
{
    protected override void SetupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = @"CREATE TABLE cte_test (
                            id INT PRIMARY KEY,
                            category VARCHAR(50),
                            amount DECIMAL(10,2)
                        )";
        cmd.ExecuteNonQuery();

        for (int i = 1; i <= 50; i++)
        {
            cmd.CommandText = $"INSERT INTO cte_test (id, category, amount) VALUES ({i}, 'cat{i % 5}', {i * 10.50})";
            cmd.ExecuteNonQuery();
        }
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = @"
            WITH high_value AS MATERIALIZED (
                SELECT category, SUM(amount) as total
                FROM cte_test
                WHERE amount > 100
                GROUP BY category
            )
            SELECT COUNT(*) FROM high_value";
        object? count = cmd.ExecuteScalar();
        AssertTrue(count != null, "MATERIALIZED hint should work in CTE");

        cmd.CommandText = @"
            WITH low_value AS NOT MATERIALIZED (
                SELECT category, amount
                FROM cte_test
                WHERE amount < 200
            )
            SELECT COUNT(*) FROM low_value";
        count = cmd.ExecuteScalar();
        AssertTrue(count != null, "NOT MATERIALIZED hint should work in CTE");

        cmd.CommandText = @"
            WITH summary AS MATERIALIZED (
                SELECT category, COUNT(*) as cnt, AVG(amount) as avg_amount
                FROM cte_test
                GROUP BY category
            )
            SELECT category FROM summary WHERE cnt > 5 ORDER BY category LIMIT 1";
        object? result = cmd.ExecuteScalar();
        AssertTrue(result != null, "Complex query with MATERIALIZED CTE should work");

        cmd.CommandText = @"
            WITH RECURSIVE hierarchy AS (
                SELECT id, category, amount, 1 as level
                FROM cte_test
                WHERE id = 1
                UNION ALL
                SELECT t.id, t.category, t.amount, h.level + 1
                FROM cte_test t
                JOIN hierarchy h ON t.id = h.id + 1
                WHERE h.level < 10
            )
            SELECT COUNT(*) FROM hierarchy";
        count = cmd.ExecuteScalar();
        AssertTrue(count != null && Convert.ToInt64(count) >= 1, "Recursive CTE should work");
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS cte_test CASCADE";
        cmd.ExecuteNonQuery();
    }
}
