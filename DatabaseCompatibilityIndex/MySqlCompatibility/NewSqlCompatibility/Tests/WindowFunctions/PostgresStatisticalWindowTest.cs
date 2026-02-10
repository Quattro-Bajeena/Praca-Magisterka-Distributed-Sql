using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.WindowFunctions;

[SqlTest(SqlFeatureCategory.WindowFunctions, "Test PostgreSQL advanced statistical window functions", DatabaseType.PostgreSql)]
public class PostgresStatisticalWindowTest : SqlTest
{
    protected override void SetupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = @"CREATE TABLE exam_scores (
                            id SERIAL PRIMARY KEY,
                            student VARCHAR(100),
                            subject VARCHAR(50),
                            score DECIMAL(5,2)
                        )";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO exam_scores (student, subject, score) VALUES ('Alice', 'Math', 85.5)";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "INSERT INTO exam_scores (student, subject, score) VALUES ('Bob', 'Math', 92.0)";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "INSERT INTO exam_scores (student, subject, score) VALUES ('Charlie', 'Math', 78.5)";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "INSERT INTO exam_scores (student, subject, score) VALUES ('David', 'Math', 88.0)";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "INSERT INTO exam_scores (student, subject, score) VALUES ('Alice', 'Science', 90.0)";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "INSERT INTO exam_scores (student, subject, score) VALUES ('Bob', 'Science', 87.5)";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "INSERT INTO exam_scores (student, subject, score) VALUES ('Charlie', 'Science', 82.0)";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "INSERT INTO exam_scores (student, subject, score) VALUES ('David', 'Science', 95.5)";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = @"SELECT subject, 
                            STDDEV_POP(score) OVER (PARTITION BY subject) as std_dev,
                           VAR_POP(score) OVER (PARTITION BY subject) as variance
                           FROM exam_scores
                           WHERE subject = 'Math'
                           LIMIT 1";

        using (DbDataReader reader = cmd.ExecuteReader())
        {
            AssertTrue(reader.Read(), "Should have Math statistics");
            double stdDev = reader.GetDouble(1);
            double variance = reader.GetDouble(2);
            AssertTrue(stdDev > 0, "Standard deviation should be positive");
            AssertTrue(variance > 0, "Variance should be positive");
        }

        cmd.CommandText = @"SELECT student, subject, score,
                           PERCENT_RANK() OVER (PARTITION BY subject ORDER BY score) as pct_rank,
                           CUME_DIST() OVER (PARTITION BY subject ORDER BY score) as cumulative_dist
                           FROM exam_scores
                           WHERE subject = 'Math'
                           ORDER BY score";

        int rowCount = 0;
        using (DbDataReader reader = cmd.ExecuteReader())
        {
            while (reader.Read())
            {
                rowCount++;
                double pctRank = reader.GetDouble(3);
                double cumeDist = reader.GetDouble(4);
                AssertTrue(pctRank >= 0 && pctRank <= 1, "PERCENT_RANK should be between 0 and 1");
                AssertTrue(cumeDist > 0 && cumeDist <= 1, "CUME_DIST should be between 0 and 1");
            }
        }
        AssertEqual(4, rowCount, "Should have 4 Math scores");

        cmd.CommandText = @"SELECT subject,
                           PERCENTILE_CONT(0.5) WITHIN GROUP (ORDER BY score) as median,
                           PERCENTILE_CONT(0.75) WITHIN GROUP (ORDER BY score) as percentile_75
                           FROM exam_scores
                           GROUP BY subject
                           ORDER BY subject";

        using (DbDataReader reader = cmd.ExecuteReader())
        {
            AssertTrue(reader.Read(), "Should have Math percentiles");
            string subject1 = reader.GetString(0);
            double median1 = reader.GetDouble(1);
            double p75_1 = reader.GetDouble(2);
            AssertTrue(median1 > 0, "Median should be positive");
            AssertTrue(p75_1 >= median1, "75th percentile should be >= median");

            AssertTrue(reader.Read(), "Should have Science percentiles");
            string subject2 = reader.GetString(0);
            double median2 = reader.GetDouble(1);
            double p75_2 = reader.GetDouble(2);
            AssertTrue(median2 > 0, "Median should be positive");
            AssertTrue(p75_2 >= median2, "75th percentile should be >= median");
        }

        cmd.CommandText = @"SELECT student,
                           AVG(score) OVER () as overall_avg,
                           score - AVG(score) OVER () as deviation_from_avg
                           FROM exam_scores
                           ORDER BY student, subject
                           LIMIT 2";

        using (DbDataReader reader = cmd.ExecuteReader())
        {
            AssertTrue(reader.Read(), "Should have first student result");
            Double overallAvg = reader.GetDouble(1);
            AssertTrue(overallAvg > 0 && overallAvg < 100, "Overall average should be reasonable");
        }
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS exam_scores CASCADE";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());
    }
}
