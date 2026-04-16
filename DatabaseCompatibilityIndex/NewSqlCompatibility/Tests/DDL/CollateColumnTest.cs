using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.TableDefinition;

[SqlTest(SqlFeatureCategory.DDL, "Test COLLATE on columns affects string comparison and ordering")]
public class CollateColumnTest : SqlTest
{
    protected override void SetupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        // ci_col: case-insensitive collation; bin_col: binary (case-sensitive) collation
        cmd.CommandText = @"CREATE TABLE collate_test (
            id      INT AUTO_INCREMENT PRIMARY KEY,
            ci_col  VARCHAR(100) COLLATE utf8mb4_unicode_ci,
            bin_col VARCHAR(100) COLLATE utf8mb4_bin
        )";
        cmd.ExecuteNonQuery();

        string[] values = ["('Apple', 'Apple')", "('banana', 'banana')", "('Cherry', 'Cherry')"];
        foreach (string v in values)
        {
            cmd.CommandText = $"INSERT INTO collate_test (ci_col, bin_col) VALUES {v}";
            cmd.ExecuteNonQuery();
        }
    }

    protected override void SetupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = @"CREATE TABLE collate_test (
            id       SERIAL PRIMARY KEY,
            c_col    TEXT COLLATE ""C"",
            def_col  TEXT
        )";
        cmd.ExecuteNonQuery();

        string[] values = ["('Apple', 'Apple')", "('banana', 'banana')", "('Cherry', 'Cherry')"];
        foreach (string v in values)
        {
            cmd.CommandText = $"INSERT INTO collate_test (c_col, def_col) VALUES {v}";
            cmd.ExecuteNonQuery();
        }
    }

    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        // Case-insensitive collation: 'apple' matches 'Apple'
        cmd.CommandText = "SELECT COUNT(*) FROM collate_test WHERE ci_col = 'apple'";
        long ciCount = Convert.ToInt64(cmd.ExecuteScalar()!);
        AssertEqual(1L, ciCount, "Case-insensitive collation should match 'apple' = 'Apple'");

        // Binary collation: 'apple' does NOT match 'Apple'
        cmd.CommandText = "SELECT COUNT(*) FROM collate_test WHERE bin_col = 'apple'";
        long binCount = Convert.ToInt64(cmd.ExecuteScalar()!);
        AssertEqual(0L, binCount, "Binary collation should not match 'apple' = 'Apple'");

        // Binary collation: exact-case match succeeds
        cmd.CommandText = "SELECT COUNT(*) FROM collate_test WHERE bin_col = 'Apple'";
        long binExact = Convert.ToInt64(cmd.ExecuteScalar()!);
        AssertEqual(1L, binExact, "Binary collation should match exact case 'Apple'");

        // ORDER BY with binary collation: uppercase letters sort before lowercase in ASCII order
        cmd.CommandText = "SELECT bin_col FROM collate_test ORDER BY bin_col LIMIT 1";
        object? firstBin = cmd.ExecuteScalar();
        AssertEqual("Apple", firstBin?.ToString(), "Binary ORDER BY should put 'Apple' first (uppercase < lowercase in ASCII)");
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        // COLLATE "C" is byte-order; exact case match succeeds
        cmd.CommandText = "SELECT COUNT(*) FROM collate_test WHERE c_col = 'Apple'";
        long exactCount = Convert.ToInt64(cmd.ExecuteScalar()!);
        AssertEqual(1L, exactCount, "COLLATE \"C\" column should match exact case 'Apple'");

        // COLLATE "C" does not do case-folding: 'apple' != 'Apple'
        cmd.CommandText = "SELECT COUNT(*) FROM collate_test WHERE c_col = 'apple'";
        long caseCount = Convert.ToInt64(cmd.ExecuteScalar()!);
        AssertEqual(0L, caseCount, "COLLATE \"C\" column should not match 'apple' against 'Apple'");

        // In "C" collation uppercase sorts before lowercase
        cmd.CommandText = "SELECT c_col FROM collate_test ORDER BY c_col LIMIT 1";
        object? firstC = cmd.ExecuteScalar();
        AssertEqual("Apple", firstC?.ToString(), "COLLATE \"C\" ORDER BY should put 'Apple' first");

        // Verify collation registered in pg_attribute
        cmd.CommandText = @"SELECT c.collname
                            FROM pg_attribute a
                            JOIN pg_collation c ON c.oid = a.attcollation
                            WHERE a.attrelid = 'collate_test'::regclass
                              AND a.attname = 'c_col'";
        object? collName = cmd.ExecuteScalar();
        AssertEqual("C", collName?.ToString(), "pg_attribute should show collation 'C' for c_col");
    }

    protected override void CleanupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS collate_test";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS collate_test CASCADE";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());
    }
}
