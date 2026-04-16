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

        cmd.CommandText = @"CREATE DOMAIN email_address AS TEXT
                            CHECK (VALUE ~ '^[^@]+@[^@]+\.[^@]+$')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"CREATE TABLE contacts (
            id      SERIAL        PRIMARY KEY,
            user_id positive_int  NOT NULL,
            email   email_address NOT NULL,
            name    TEXT          NOT NULL
        )";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO contacts (user_id, email, name) VALUES (1, 'alice@example.com', 'Alice')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO contacts (user_id, email, name) VALUES (2, 'bob@test.org', 'Bob')";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "SELECT COUNT(*) FROM contacts";
        long count = Convert.ToInt64(cmd.ExecuteScalar()!);
        AssertEqual(2L, count, "contacts should contain 2 valid rows");

        cmd.CommandText = "SELECT name FROM contacts WHERE user_id = 1";
        object? name = cmd.ExecuteScalar();
        AssertEqual("Alice", name?.ToString(), "First contact should be Alice");

        // positive_int domain: zero violates VALUE > 0
        AssertThrows<Exception>(
            () =>
            {
                using DbCommand badCmd = connection.CreateCommand();
                badCmd.CommandText = "INSERT INTO contacts (user_id, email, name) VALUES (0, 'x@y.com', 'Zero')";
                badCmd.ExecuteNonQuery();
            },
            "user_id = 0 should violate the positive_int domain constraint");

        // positive_int domain: negative value violates VALUE > 0
        AssertThrows<Exception>(
            () =>
            {
                using DbCommand badCmd = connection.CreateCommand();
                badCmd.CommandText = "INSERT INTO contacts (user_id, email, name) VALUES (-5, 'x@y.com', 'Neg')";
                badCmd.ExecuteNonQuery();
            },
            "user_id = -5 should violate the positive_int domain constraint");

        // email_address domain: value without '@' fails the regex
        AssertThrows<Exception>(
            () =>
            {
                using DbCommand badCmd = connection.CreateCommand();
                badCmd.CommandText = "INSERT INTO contacts (user_id, email, name) VALUES (3, 'not-an-email', 'Bad')";
                badCmd.ExecuteNonQuery();
            },
            "email without '@' should violate the email_address domain constraint");

        // Verify both domains are registered in pg_type with typtype = 'd' (domain)
        cmd.CommandText = @"SELECT COUNT(*) FROM pg_type
                            WHERE typname IN ('positive_int', 'email_address')
                              AND typtype = 'd'";
        long domainCount = Convert.ToInt64(cmd.ExecuteScalar()!);
        AssertEqual(2L, domainCount, "Both domains should appear in pg_type with typtype 'd'");

        // Verify positive_int is based on INT4 (oid 23)
        cmd.CommandText = "SELECT typbasetype = 23 FROM pg_type WHERE typname = 'positive_int'";
        object? basedOnInt = cmd.ExecuteScalar();
        AssertEqual(true, Convert.ToBoolean(basedOnInt!), "positive_int domain should be based on INT (oid 23)");
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "DROP TABLE IF EXISTS contacts CASCADE";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());

        cmd.CommandText = "DROP DOMAIN IF EXISTS email_address CASCADE";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());

        cmd.CommandText = "DROP DOMAIN IF EXISTS positive_int CASCADE";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());
    }
}
