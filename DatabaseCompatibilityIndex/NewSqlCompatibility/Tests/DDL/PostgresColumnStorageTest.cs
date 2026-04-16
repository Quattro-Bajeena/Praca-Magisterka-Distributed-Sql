using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.TableDefinition;

// https://www.postgresql.org/docs/current/storage-toast.html
[SqlTest(SqlFeatureCategory.DDL, "Test STORAGE clause on columns is recorded in pg_attribute", DatabaseType.PostgreSql)]
public class PostgresColumnStorageTest : SqlTest
{
    protected override void SetupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = @"CREATE TABLE storage_test (
            id           SERIAL PRIMARY KEY,
            plain_col    INT        STORAGE PLAIN,
            external_col TEXT       STORAGE EXTERNAL,
            extended_col TEXT       STORAGE EXTENDED,
            main_col     TEXT       STORAGE MAIN
        )";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO storage_test (plain_col, external_col, extended_col, main_col) VALUES (42, 'ext', 'ext2', 'main')";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "SELECT COUNT(*) FROM storage_test";
        long count = Convert.ToInt64(cmd.ExecuteScalar()!);
        AssertEqual(1L, count, "storage_test should contain 1 row");

        // Read all four storage strategy values from the catalog at once
        cmd.CommandText = @"SELECT attname, attstorage
                            FROM pg_attribute
                            WHERE attrelid = 'storage_test'::regclass
                              AND attname IN ('plain_col', 'external_col', 'extended_col', 'main_col')
                            ORDER BY attname";

        var storageMap = new Dictionary<string, char>();
        using (DbDataReader reader = cmd.ExecuteReader())
        {
            while (reader.Read())
                storageMap[reader.GetString(0)] = reader.GetChar(1);
        }

        AssertEqual('p', storageMap["plain_col"], "plain_col should have STORAGE PLAIN ('p')");
        AssertEqual('e', storageMap["external_col"], "external_col should have STORAGE EXTERNAL ('e')");
        AssertEqual('x', storageMap["extended_col"], "extended_col should have STORAGE EXTENDED ('x')");
        AssertEqual('m', storageMap["main_col"], "main_col should have STORAGE MAIN ('m')");
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS storage_test CASCADE";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());
    }
}
