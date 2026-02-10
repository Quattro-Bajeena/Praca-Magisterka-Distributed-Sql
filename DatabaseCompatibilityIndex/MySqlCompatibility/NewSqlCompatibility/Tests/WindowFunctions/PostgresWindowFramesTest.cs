using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.WindowFunctions;

[SqlTest(SqlFeatureCategory.WindowFunctions, "Test PostgreSQL advanced window frame specifications", DatabaseType.PostgreSql)]
public class PostgresWindowFramesTest : SqlTest
{
    protected override void SetupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = @"CREATE TABLE sales_frames (
                            id SERIAL PRIMARY KEY,
                            day INT,
                            amount DECIMAL(10,2)
                        )";
        cmd.ExecuteNonQuery();

        for (int i = 1; i <= 10; i++)
        {
            cmd.CommandText = $"INSERT INTO sales_frames (day, amount) VALUES ({i}, {i * 100})";
            cmd.ExecuteNonQuery();
        }
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = @"SELECT day, amount,
                           SUM(amount) OVER (ORDER BY day ROWS BETWEEN 2 PRECEDING AND CURRENT ROW) as moving_sum_3
                           FROM sales_frames
                           WHERE day <= 5
                           ORDER BY day";

        using (DbDataReader reader = cmd.ExecuteReader())
        {
            AssertTrue(reader.Read(), "Should have results");
            AssertEqual(1, reader.GetInt32(0), "Day 1");
            AssertEqual(100m, reader.GetDecimal(2), "Day 1: sum of just day 1");

            AssertTrue(reader.Read(), "Should have day 2");
            AssertEqual(2, reader.GetInt32(0), "Day 2");
            AssertEqual(300m, reader.GetDecimal(2), "Day 2: sum of days 1-2");

            AssertTrue(reader.Read(), "Should have day 3");
            AssertEqual(3, reader.GetInt32(0), "Day 3");
            AssertEqual(600m, reader.GetDecimal(2), "Day 3: sum of days 1-3");

            AssertTrue(reader.Read(), "Should have day 4");
            AssertEqual(4, reader.GetInt32(0), "Day 4");
            AssertEqual(900m, reader.GetDecimal(2), "Day 4: sum of days 2-4 (window of 3)");

            AssertTrue(reader.Read(), "Should have day 5");
            AssertEqual(5, reader.GetInt32(0), "Day 5");
            AssertEqual(1200m, reader.GetDecimal(2), "Day 5: sum of days 3-5 (window of 3)");
        }

        cmd.CommandText = @"SELECT day, amount,
                           AVG(amount) OVER (ORDER BY day ROWS BETWEEN 1 PRECEDING AND 1 FOLLOWING) as moving_avg
                           FROM sales_frames
                           WHERE day BETWEEN 5 AND 7
                           ORDER BY day";

        using (DbDataReader reader = cmd.ExecuteReader())
        {
            AssertTrue(reader.Read(), "Should have day 5");
            double avg5 = reader.GetDouble(2);
            AssertEqual(550.0, avg5, "Day 5: avg of days 5-6 (500+600)/2 = 500");

            AssertTrue(reader.Read(), "Should have day 6");
            double avg6 = reader.GetDouble(2);
            AssertEqual(600.0, avg6, "Day 6: avg of days 5-7 (500+600+700)/3 = 600");

            AssertTrue(reader.Read(), "Should have day 7");
            double avg7 = reader.GetDouble(2);
            AssertEqual(650.0, avg7, "Day 7: avg of days 7-8 (700+800)/2 = 650");
        }

        cmd.CommandText = @"SELECT day, amount,
                           SUM(amount) OVER (ORDER BY day RANGE BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW) as cumulative_sum
                           FROM sales_frames
                           WHERE day <= 4
                           ORDER BY day";

        using (DbDataReader reader = cmd.ExecuteReader())
        {
            AssertTrue(reader.Read(), "Day 1");
            AssertEqual(100m, reader.GetDecimal(2), "Cumulative: 100");

            AssertTrue(reader.Read(), "Day 2");
            AssertEqual(300m, reader.GetDecimal(2), "Cumulative: 100+200");

            AssertTrue(reader.Read(), "Day 3");
            AssertEqual(600m, reader.GetDecimal(2), "Cumulative: 100+200+300");

            AssertTrue(reader.Read(), "Day 4");
            AssertEqual(1000m, reader.GetDecimal(2), "Cumulative: 100+200+300+400");
        }

        cmd.CommandText = @"SELECT COUNT(*) FROM (
                           SELECT day,
                                  FIRST_VALUE(amount) OVER (ORDER BY day ROWS BETWEEN UNBOUNDED PRECEDING AND UNBOUNDED FOLLOWING) as first_val,
                                  LAST_VALUE(amount) OVER (ORDER BY day ROWS BETWEEN UNBOUNDED PRECEDING AND UNBOUNDED FOLLOWING) as last_val
                           FROM sales_frames
                           ) t";
        object? count = cmd.ExecuteScalar();
        AssertEqual(10L, Convert.ToInt64(count!), "FIRST_VALUE and LAST_VALUE should work");
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS sales_frames CASCADE";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());
    }
}
