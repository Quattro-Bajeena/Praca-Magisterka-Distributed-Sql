using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Performance;

[SqlTest(SqlFeatureCategory.PerformanceHints, "Test PostgreSQL EXPLAIN and EXPLAIN ANALYZE", DatabaseType.PostgreSql)]
public class PostgresExplainAnalyzeTest : SqlTest
{
    protected override void SetupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = @"CREATE TABLE explain_test (
                            id INT PRIMARY KEY,
                            status VARCHAR(20),
                            value INT
                        )";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "CREATE INDEX idx_status ON explain_test(status)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "CREATE INDEX idx_value ON explain_test(value)";
        cmd.ExecuteNonQuery();

        for (int i = 1; i <= 100; i++)
        {
            cmd.CommandText = $"INSERT INTO explain_test VALUES ({i}, 'status{i % 3}', {i * 5})";
            cmd.ExecuteNonQuery();
        }
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "EXPLAIN SELECT * FROM explain_test WHERE status = 'status1'";
        using (DbDataReader reader = cmd.ExecuteReader())
        {
            AssertTrue(reader.Read(), "EXPLAIN should return query plan");
            string? plan = reader.GetString(0);
            AssertTrue(plan != null && plan.Length > 0, "Query plan should contain text");
        }

        cmd.CommandText = "EXPLAIN ANALYZE SELECT COUNT(*) FROM explain_test WHERE value > 250";
        using (DbDataReader reader = cmd.ExecuteReader())
        {
            bool hasRows = false;
            while (reader.Read())
            {
                hasRows = true;
                string? line = reader.GetString(0);
                AssertTrue(line != null, "EXPLAIN ANALYZE should return plan lines");
            }
            AssertTrue(hasRows, "EXPLAIN ANALYZE should return results");
        }

        cmd.CommandText = "EXPLAIN (FORMAT JSON) SELECT * FROM explain_test WHERE value < 100";
        using (DbDataReader reader = cmd.ExecuteReader())
        {
            AssertTrue(reader.Read(), "EXPLAIN with FORMAT JSON should work");
            string? json = reader.GetString(0);
            AssertTrue(json != null && json.Contains("Plan"), "JSON plan should contain 'Plan'");
        }

        cmd.CommandText = "EXPLAIN (VERBOSE, BUFFERS) SELECT status, COUNT(*) FROM explain_test GROUP BY status";
        using (DbDataReader reader = cmd.ExecuteReader())
        {
            AssertTrue(reader.Read(), "EXPLAIN with options should work");
        }

        cmd.CommandText = @"EXPLAIN (COSTS OFF) 
                           SELECT e1.id, e2.value 
                           FROM explain_test e1 
                           JOIN explain_test e2 ON e1.id = e2.id 
                           WHERE e1.status = 'status0'";
        using (DbDataReader reader = cmd.ExecuteReader())
        {
            AssertTrue(reader.Read(), "EXPLAIN for JOIN should work");
        }
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS explain_test CASCADE";
        cmd.ExecuteNonQuery();
    }
}
