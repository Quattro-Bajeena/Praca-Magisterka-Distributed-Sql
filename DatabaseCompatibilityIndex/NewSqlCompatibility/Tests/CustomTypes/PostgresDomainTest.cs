using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.CustomTypes;

[SqlTest(SqlFeatureCategory.CustomTypes, "Test CREATE DOMAIN enforces constraints on columns that use it", DatabaseType.PostgreSql)]
public class PostgresDomainTest : SqlTest
{
    protected override void SetupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "CREATE DOMAIN positive_int AS INT CHECK (VALUE > 0)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"CREATE TABLE contacts (
            id      SERIAL        PRIMARY KEY,
            user_id positive_int  NOT NULL,
            name    TEXT          NOT NULL
        )";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO contacts (user_id, name) VALUES (1, 'Alice')";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "SELECT COUNT(*) FROM contacts";
        long count = Convert.ToInt64(cmd.ExecuteScalar()!);
        AssertEqual(1L, count, "contacts should contain 1 row");

        AssertThrows<Exception>(
            () =>
            {
                using DbCommand badCmd = connection.CreateCommand();
                badCmd.CommandText = "INSERT INTO contacts (user_id, name) VALUES (0, 'Zero')";
                badCmd.ExecuteNonQuery();
            },
            "user_id = 0 should violate the positive_int domain constraint");
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "DROP TABLE IF EXISTS contacts CASCADE";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());

        cmd.CommandText = "DROP DOMAIN IF EXISTS positive_int CASCADE";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());
    }
}
