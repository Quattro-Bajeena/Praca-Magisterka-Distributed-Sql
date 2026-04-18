using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.CustomTypes;

[SqlTest(SqlFeatureCategory.CustomTypes, "Test CREATE TYPE composite type used as a column type", DatabaseType.PostgreSql)]
public class PostgresCompositeTypeTest : SqlTest
{
    protected override void SetupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = @"CREATE TYPE address AS (
            street   TEXT,
            city     TEXT,
            zip_code VARCHAR(10)
        )";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"CREATE TABLE persons (
            id   SERIAL PRIMARY KEY,
            name TEXT NOT NULL,
            home address
        )";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO persons (name, home) VALUES ('Alice', ROW('Main St 1', 'Warsaw', '00-001'))";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "SELECT COUNT(*) FROM persons";
        long count = Convert.ToInt64(cmd.ExecuteScalar()!);
        AssertEqual(1L, count, "persons table should contain 1 row");

        cmd.CommandText = "SELECT (home).city FROM persons WHERE id = 1";
        object? city = cmd.ExecuteScalar();
        AssertEqual("Warsaw", city?.ToString(), "Alice's city should be Warsaw");
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "DROP TABLE IF EXISTS persons CASCADE";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());

        cmd.CommandText = "DROP TYPE IF EXISTS address CASCADE";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());
    }
}
