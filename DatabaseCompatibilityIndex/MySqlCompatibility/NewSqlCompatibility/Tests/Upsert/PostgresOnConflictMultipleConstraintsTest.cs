using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Upsert;

[SqlTest(SqlFeatureCategory.Upsert, "Test PostgreSQL ON CONFLICT with multiple columns and unique constraints", DatabaseType.PostgreSql)]
public class PostgresOnConflictMultipleConstraintsTest : SqlTest
{
    protected override void SetupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = @"CREATE TABLE users_unique (
                            id SERIAL PRIMARY KEY,
                            username VARCHAR(50) UNIQUE,
                            email VARCHAR(100) UNIQUE,
                            full_name VARCHAR(100),
                            login_count INT DEFAULT 0
                        )";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO users_unique (username, email, full_name) VALUES ('alice', 'alice@example.com', 'Alice Smith')";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = @"INSERT INTO users_unique (username, email, full_name, login_count) 
                           VALUES ('bob', 'bob@example.com', 'Bob Jones', 1)
                           ON CONFLICT (username) DO UPDATE 
                           SET login_count = users_unique.login_count + 1";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT login_count FROM users_unique WHERE username = 'bob'";
        object? bobLogins = cmd.ExecuteScalar();
        AssertEqual(1L, Convert.ToInt64(bobLogins!), "New user Bob should have 1 login");

        cmd.CommandText = @"INSERT INTO users_unique (username, email, full_name, login_count) 
                           VALUES ('bob', 'bob_new@example.com', 'Bob Jones Updated', 1)
                           ON CONFLICT (username) DO UPDATE 
                           SET login_count = users_unique.login_count + 1,
                               full_name = EXCLUDED.full_name";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT login_count, full_name FROM users_unique WHERE username = 'bob'";
        using (DbDataReader reader = cmd.ExecuteReader())
        {
            AssertTrue(reader.Read(), "Should find Bob");
            AssertEqual(2L, Convert.ToInt64(reader.GetValue(0)), "Bob should have 2 logins");
            AssertEqual("Bob Jones Updated", reader.GetString(1), "Full name should be updated");
        }

        cmd.CommandText = @"INSERT INTO users_unique (username, email, full_name, login_count) 
                           VALUES ('charlie', 'alice@example.com', 'Charlie Brown', 1)
                           ON CONFLICT (email) DO UPDATE 
                           SET full_name = EXCLUDED.full_name";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT full_name FROM users_unique WHERE email = 'alice@example.com'";
        object? fullName = cmd.ExecuteScalar();
        AssertEqual("Charlie Brown", fullName?.ToString(), "Should update on email conflict");

        cmd.CommandText = @"INSERT INTO users_unique (username, email, full_name) 
                           VALUES ('david', 'david@example.com', 'David Lee')
                           ON CONFLICT ON CONSTRAINT users_unique_username_key DO NOTHING";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM users_unique";
        object? count = cmd.ExecuteScalar();
        AssertTrue(Convert.ToInt64(count!) >= 2, "Should have at least 2 users");
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS users_unique CASCADE";
        cmd.ExecuteNonQuery();
    }
}
