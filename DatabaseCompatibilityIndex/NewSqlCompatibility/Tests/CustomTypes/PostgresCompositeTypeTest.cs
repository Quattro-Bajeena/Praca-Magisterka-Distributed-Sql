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

        cmd.CommandText = "INSERT INTO persons (name, home) VALUES ('Bob', ROW('Oak Ave 7', 'Krakow', '30-002'))";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "SELECT COUNT(*) FROM persons";
        long count = Convert.ToInt64(cmd.ExecuteScalar()!);
        AssertEqual(2L, count, "persons table should contain 2 rows");

        // Field access via dot notation on composite column
        cmd.CommandText = "SELECT (home).city FROM persons WHERE id = 1";
        object? city = cmd.ExecuteScalar();
        AssertEqual("Warsaw", city?.ToString(), "Alice's city should be Warsaw");

        cmd.CommandText = "SELECT (home).zip_code FROM persons WHERE id = 2";
        object? zip = cmd.ExecuteScalar();
        AssertEqual("30-002", zip?.ToString(), "Bob's zip_code should be 30-002");

        // Filter using composite field
        cmd.CommandText = "SELECT name FROM persons WHERE (home).city = 'Krakow'";
        object? name = cmd.ExecuteScalar();
        AssertEqual("Bob", name?.ToString(), "Query by composite field city should return Bob");

        // Verify the type is registered in pg_type with typtype = 'c' (composite)
        cmd.CommandText = "SELECT typtype FROM pg_type WHERE typname = 'address'";
        object? typtype = cmd.ExecuteScalar();
        AssertEqual("c", typtype?.ToString(), "pg_type should show typtype 'c' for a composite type");

        // Verify the composite type has 3 attributes
        cmd.CommandText = @"SELECT COUNT(*) FROM pg_attribute
                            WHERE attrelid = (SELECT typrelid FROM pg_type WHERE typname = 'address')
                              AND attnum > 0";
        long attrCount = Convert.ToInt64(cmd.ExecuteScalar()!);
        AssertEqual(3L, attrCount, "Composite type 'address' should have 3 fields");
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
