using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.TableDefinition;

[SqlTest(SqlFeatureCategory.DDL, "Test CREATE TABLE LIKE copies structure from an existing table")]
public class TableLikeTest : SqlTest
{
    protected override void SetupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = @"CREATE TABLE template_my (
            id         INT AUTO_INCREMENT PRIMARY KEY,
            name       VARCHAR(100) NOT NULL,
            score      INT          NOT NULL DEFAULT 0,
            created_at DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP
        )";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO template_my (name, score) VALUES ('original', 99)";
        cmd.ExecuteNonQuery();
    }

    protected override void SetupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = @"CREATE TABLE template_pg (
            id    SERIAL PRIMARY KEY,
            name  TEXT NOT NULL,
            score INT  NOT NULL CHECK (score >= 0)
        )";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO template_pg (name, score) VALUES ('original', 99)";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "CREATE TABLE copy_my LIKE template_my";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO copy_my (name, score) VALUES ('copy', 42)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM copy_my";
        long copyCount = Convert.ToInt64(cmd.ExecuteScalar()!);
        AssertEqual(1L, copyCount, "Copied table should contain 1 inserted row");

        // Original table untouched
        cmd.CommandText = "SELECT COUNT(*) FROM template_my";
        long origCount = Convert.ToInt64(cmd.ExecuteScalar()!);
        AssertEqual(1L, origCount, "Template table should still contain only 1 row");

        // Verify column count via information_schema
        cmd.CommandText = @"SELECT COUNT(*) FROM information_schema.columns
                            WHERE table_schema = DATABASE()
                              AND table_name = 'copy_my'";
        long colCount = Convert.ToInt64(cmd.ExecuteScalar()!);
        AssertEqual(4L, colCount, "copy_my should have the same 4 columns as the template");
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        // INCLUDING ALL copies constraints, defaults, indexes, etc.
        cmd.CommandText = "CREATE TABLE copy_pg (LIKE template_pg INCLUDING ALL)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO copy_pg (name, score) VALUES ('copy', 42)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM copy_pg";
        long copyCount = Convert.ToInt64(cmd.ExecuteScalar()!);
        AssertEqual(1L, copyCount, "Copied table should contain 1 inserted row");

        cmd.CommandText = "SELECT COUNT(*) FROM template_pg";
        long origCount = Convert.ToInt64(cmd.ExecuteScalar()!);
        AssertEqual(1L, origCount, "Template table should still contain only 1 row");

        // CHECK constraint (score >= 0) should have been copied
        AssertThrows<Exception>(
            () =>
            {
                using DbCommand badCmd = connection.CreateCommand();
                badCmd.CommandText = "INSERT INTO copy_pg (name, score) VALUES ('bad', -1)";
                badCmd.ExecuteNonQuery();
            },
            "Including CHECK constraint should reject negative score in copy_pg");

        // Verify column count via pg_attribute
        cmd.CommandText = @"SELECT COUNT(*) FROM pg_attribute
                            WHERE attrelid = 'copy_pg'::regclass
                              AND attnum > 0
                              AND NOT attisdropped";
        long colCount = Convert.ToInt64(cmd.ExecuteScalar()!);
        AssertEqual(3L, colCount, "copy_pg should have the same 3 columns as the template");
    }

    protected override void CleanupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "DROP TABLE IF EXISTS copy_my";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());

        cmd.CommandText = "DROP TABLE IF EXISTS template_my";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "DROP TABLE IF EXISTS copy_pg CASCADE";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());

        cmd.CommandText = "DROP TABLE IF EXISTS template_pg CASCADE";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());
    }
}
