using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.BasicQueries;

// TABLESAMPLE is a FROM-clause modifier introduced in SQL:2003 and supported by PostgreSQL.
// It instructs the query engine to read only a statistical sample of the table instead of
// every page, which can dramatically reduce I/O for approximate aggregations on large tables.
//
// PostgreSQL provides two built-in sampling methods:
//
//   BERNOULLI(p)  — Scans all physical pages but probabilistically includes each individual
//                   row with probability p/100. The result is an unbiased random sample:
//                   every row has an equal and independent chance of being selected.
//                   This is slower than SYSTEM for large percentages because every page
//                   is still read, but the resulting sample is more uniformly distributed.
//
//   SYSTEM(p)     — Operates at the page (block) level: each 8 kB page is either included
//                   or excluded as a whole with probability p/100. Pages that are selected
//                   contribute all their rows to the result. SYSTEM is faster than BERNOULLI
//                   because it skips entire pages, but rows from the same page are correlated,
//                   so the sample can be clustered rather than truly random.
//
// REPEATABLE(seed)
//   Both methods accept an optional REPEATABLE clause that fixes the pseudo-random seed.
//   Given the same seed, the same table state, and the same sampling percentage, every
//   execution produces the identical set of rows. This is useful for reproducible tests,
//   debugging, and auditing. The seed is a floating-point value; any change to it will
//   (almost certainly) yield a different sample.
[SqlTest(SqlFeatureCategory.BasicQueries, "Test TABLESAMPLE BERNOULLI and SYSTEM methods with REPEATABLE seed", DatabaseType.PostgreSql)]
public class PostgresTablesampleTest : SqlTest
{
    protected override void SetupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = @"CREATE TABLE sample_data (
            id    SERIAL PRIMARY KEY,
            value INT NOT NULL
        )";
        cmd.ExecuteNonQuery();

        // generate_series(1, 1000) produces a set-returning function that emits integers
        // 1..1000 as rows, allowing a single INSERT to populate the table without a loop.
        cmd.CommandText = "INSERT INTO sample_data (value) SELECT generate_series(1, 1000)";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        // BERNOULLI(100): the inclusion probability is 100 %, so every row passes the
        // per-row coin flip. The result is deterministic: all 1000 rows are returned.
        cmd.CommandText = "SELECT COUNT(*) FROM sample_data TABLESAMPLE BERNOULLI(100)";
        long bernoulli100 = Convert.ToInt64(cmd.ExecuteScalar()!);
        AssertEqual(1000L, bernoulli100, "BERNOULLI(100) must return all 1000 rows");

        // SYSTEM(100): every page is selected with 100 % probability, so all rows on
        // all pages are returned. Because SYSTEM works at page granularity, partial pages
        // do not arise at 100 % — the full table is always returned.
        cmd.CommandText = "SELECT COUNT(*) FROM sample_data TABLESAMPLE SYSTEM(100)";
        long system100 = Convert.ToInt64(cmd.ExecuteScalar()!);
        AssertEqual(1000L, system100, "SYSTEM(100) must return all 1000 rows");

        // BERNOULLI(0): the inclusion probability is 0 %, so no row ever passes the
        // per-row test. This is the degenerate lower bound and is always deterministic.
        // Note: SYSTEM(0) is intentionally not tested here because the page-level sampling
        // may still return a small number of rows when the table fits on a single page.
        cmd.CommandText = "SELECT COUNT(*) FROM sample_data TABLESAMPLE BERNOULLI(0)";
        long bernoulli0 = Convert.ToInt64(cmd.ExecuteScalar()!);
        AssertEqual(0L, bernoulli0, "BERNOULLI(0) must return 0 rows");

        // REPEATABLE(seed) pins the pseudo-random number generator to a fixed starting state.
        // Both executions below use seed 42 and 25 % sampling, so they must select exactly
        // the same rows and therefore report the same COUNT.
        cmd.CommandText = "SELECT COUNT(*) FROM sample_data TABLESAMPLE BERNOULLI(25) REPEATABLE(42)";
        long repeatCount1 = Convert.ToInt64(cmd.ExecuteScalar()!);

        cmd.CommandText = "SELECT COUNT(*) FROM sample_data TABLESAMPLE BERNOULLI(25) REPEATABLE(42)";
        long repeatCount2 = Convert.ToInt64(cmd.ExecuteScalar()!);

        AssertEqual(repeatCount1, repeatCount2,
            "REPEATABLE(42) must produce the same row count on repeated executions");

        // A 25 % sample of 1000 rows should statistically land well within (0, 1000).
        // The exact value varies by seed but must never be 0 or 1000 at this percentage.
        AssertTrue(repeatCount1 > 0 && repeatCount1 < 1000,
            "BERNOULLI(25) REPEATABLE sample should be a non-empty strict subset of 1000 rows");

        // Choosing two seeds that are far apart (1 vs 99999) makes it astronomically
        // unlikely that they produce the same pseudo-random sequence. If the counts were
        // equal it would mean REPEATABLE is being ignored (e.g., the engine always returns
        // the same sample regardless of seed), which would be a bug worth catching.
        cmd.CommandText = "SELECT COUNT(*) FROM sample_data TABLESAMPLE BERNOULLI(25) REPEATABLE(1)";
        long seed1Count = Convert.ToInt64(cmd.ExecuteScalar()!);

        cmd.CommandText = "SELECT COUNT(*) FROM sample_data TABLESAMPLE BERNOULLI(25) REPEATABLE(99999)";
        long seed2Count = Convert.ToInt64(cmd.ExecuteScalar()!);

        AssertTrue(seed1Count != seed2Count,
            "Different REPEATABLE seeds should produce different sample counts");

        // EXPLAIN must produce a valid plan for a TABLESAMPLE query
        cmd.CommandText = "EXPLAIN SELECT * FROM sample_data TABLESAMPLE BERNOULLI(10)";
        bool planGenerated = false;
        using (DbDataReader reader = cmd.ExecuteReader())
        {
            planGenerated = reader.HasRows;
        }
        AssertTrue(planGenerated, "EXPLAIN should produce a query plan for a TABLESAMPLE query");
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS sample_data CASCADE";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());
    }
}
