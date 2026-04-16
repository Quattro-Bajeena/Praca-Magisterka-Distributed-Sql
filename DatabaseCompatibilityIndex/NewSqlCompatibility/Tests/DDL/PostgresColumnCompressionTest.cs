using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.TableDefinition;

[SqlTest(SqlFeatureCategory.DDL, "Test COMPRESSION attribute on TEXT columns is recorded in pg_attribute", DatabaseType.PostgreSql)]
public class PostgresColumnCompressionTest : SqlTest
{
    protected override void SetupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = @"CREATE TABLE compression_test (
            id         SERIAL PRIMARY KEY,
            data_pglz  TEXT COMPRESSION pglz
        )";
        cmd.ExecuteNonQuery();

        // Insert a long string to exercise actual storage
        string longValue = new string('x', 5000);
        cmd.CommandText = $"INSERT INTO compression_test (data_pglz) VALUES ('{longValue}')";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "SELECT LENGTH(data_pglz) FROM compression_test WHERE id = 1";
        long len = Convert.ToInt64(cmd.ExecuteScalar()!);
        AssertEqual(5000L, len, "Stored value length should be 5000 characters");

        // 'p' = pglz compression
        cmd.CommandText = @"SELECT attcompression
                            FROM pg_attribute
                            WHERE attrelid = 'compression_test'::regclass
                              AND attname = 'data_pglz'";
        object? compressionType = cmd.ExecuteScalar();
        AssertEqual("p", compressionType?.ToString(), "data_pglz column should use pglz compression ('p')");
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS compression_test CASCADE";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());
    }
}
