using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Performance;

[SqlTest(SqlFeatureCategory.PerformanceHints, "Test PostgreSQL parallel query settings", DatabaseType.PostgreSql)]
public class PostgresParallelQueryTest : SqlTest
{
    protected override void SetupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = @"CREATE TABLE parallel_test (
                            id INT PRIMARY KEY,
                            data VARCHAR(100),
                            category INT
                        )";
        cmd.ExecuteNonQuery();

        for (int i = 1; i <= 1000; i++)
        {
            cmd.CommandText = $"INSERT INTO parallel_test VALUES ({i}, 'data{i}', {i % 10})";
            cmd.ExecuteNonQuery();
        }
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "SHOW max_parallel_workers_per_gather";
        object? workers = cmd.ExecuteScalar();
        AssertTrue(workers != null, "Should be able to read max_parallel_workers_per_gather");

        cmd.CommandText = "SET LOCAL max_parallel_workers_per_gather = 2";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SET LOCAL parallel_setup_cost = 0";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SET LOCAL parallel_tuple_cost = 0";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SET LOCAL min_parallel_table_scan_size = 0";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM parallel_test";
        object? count = cmd.ExecuteScalar();
        AssertEqual(1000L, Convert.ToInt64(count!), "Should have 1000 rows");

        cmd.CommandText = "SELECT category, COUNT(*) as cnt FROM parallel_test GROUP BY category";
        using (DbDataReader reader = cmd.ExecuteReader())
        {
            int groupCount = 0;
            while (reader.Read())
            {
                groupCount++;
            }
            AssertEqual(10, groupCount, "Should have 10 categories");
        }

        cmd.CommandText = "SHOW max_parallel_workers";
        object? forceMode = cmd.ExecuteScalar();
        AssertTrue(forceMode != null, "Should be able to read max_parallel_workers");

        cmd.CommandText = "SET LOCAL enable_parallel_append = ON";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SET LOCAL enable_parallel_hash = ON";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM parallel_test WHERE category IN (1, 2, 3)";
        count = cmd.ExecuteScalar();
        AssertTrue(count != null, "Query with parallel settings should work");
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS parallel_test CASCADE";
        cmd.ExecuteNonQuery();
    }
}
