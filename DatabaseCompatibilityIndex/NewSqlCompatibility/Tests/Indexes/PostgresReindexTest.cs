using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Indexes;

[SqlTest(SqlFeatureCategory.Indexes, "Test REINDEX rebuilds index structures while preserving query correctness", DatabaseType.PostgreSql)]
public class PostgresReindexTest : SqlTest
{
    protected override void SetupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = @"CREATE TABLE reindex_test (
            id   SERIAL PRIMARY KEY,
            name TEXT NOT NULL,
            val  INT  NOT NULL
        )";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "CREATE INDEX idx_reindex_name ON reindex_test (name)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "CREATE INDEX idx_reindex_val ON reindex_test (val)";
        cmd.ExecuteNonQuery();

        // generate_series populates the table in a single statement
        cmd.CommandText = "INSERT INTO reindex_test (name, val) SELECT 'name_' || g, g FROM generate_series(1, 200) g";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        // Baseline: verify data is intact before any reindex operation
        cmd.CommandText = "SELECT COUNT(*) FROM reindex_test";
        long baselineCount = Convert.ToInt64(cmd.ExecuteScalar()!);
        AssertEqual(200L, baselineCount, "Table should have 200 rows before REINDEX");

        // REINDEX INDEX rebuilds a single named index without touching the table data
        cmd.CommandText = "REINDEX INDEX idx_reindex_name";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT val FROM reindex_test WHERE name = 'name_42'";
        long valAfterIndexReindex = Convert.ToInt64(cmd.ExecuteScalar()!);
        AssertEqual(42L, valAfterIndexReindex, "Index lookup should return the correct row after REINDEX INDEX");

        // REINDEX TABLE rebuilds every index on the table, including the primary key
        cmd.CommandText = "REINDEX TABLE reindex_test";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM reindex_test";
        long afterTableReindex = Convert.ToInt64(cmd.ExecuteScalar()!);
        AssertEqual(200L, afterTableReindex, "All 200 rows should be accessible after REINDEX TABLE");

        cmd.CommandText = "SELECT val FROM reindex_test WHERE name = 'name_100'";
        long valAfterTableReindex = Convert.ToInt64(cmd.ExecuteScalar()!);
        AssertEqual(100L, valAfterTableReindex, "Index lookup should return the correct row after REINDEX TABLE");

        // REINDEX INDEX CONCURRENTLY rebuilds without locking out concurrent reads/writes;
        // it cannot be executed inside a transaction block
        cmd.CommandText = "REINDEX INDEX CONCURRENTLY idx_reindex_val";
        cmd.ExecuteNonQuery();

        // Range query exercises the rebuilt idx_reindex_val index
        cmd.CommandText = "SELECT COUNT(*) FROM reindex_test WHERE val BETWEEN 50 AND 60";
        long rangeCount = Convert.ToInt64(cmd.ExecuteScalar()!);
        AssertEqual(11L, rangeCount, "Range query should return 11 rows (50..60 inclusive) after CONCURRENT REINDEX");

        // Both indexes must still be registered in the catalog after all reindex operations
        cmd.CommandText = @"SELECT COUNT(*) FROM pg_indexes
                            WHERE tablename = 'reindex_test'
                              AND indexname IN ('idx_reindex_name', 'idx_reindex_val')";
        long indexCount = Convert.ToInt64(cmd.ExecuteScalar()!);
        AssertEqual(2L, indexCount, "Both named indexes should remain in pg_indexes after all REINDEX operations");
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS reindex_test CASCADE";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());
    }
}
