using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Indexes;

[SqlTest(SqlFeatureCategory.Indexes, "Test PostgreSQL expression indexes", DatabaseType.PostgreSql)]
public class PostgresExpressionIndexTest : SqlTest
{
    protected override void SetupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        
        cmd.CommandText = @"CREATE TABLE users_expr (
                            id SERIAL PRIMARY KEY,
                            email VARCHAR(100),
                            username VARCHAR(50),
                            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                        )";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "CREATE INDEX idx_lower_email ON users_expr (LOWER(email))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "CREATE INDEX idx_upper_username ON users_expr (UPPER(username))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO users_expr (email, username) VALUES ('Alice@Example.COM', 'alice')";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "INSERT INTO users_expr (email, username) VALUES ('bob@EXAMPLE.com', 'BOB')";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "INSERT INTO users_expr (email, username) VALUES ('Charlie@test.COM', 'Charlie')";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "SELECT COUNT(*) FROM users_expr WHERE LOWER(email) = 'alice@example.com'";
        object? count = cmd.ExecuteScalar();
        AssertEqual(1L, Convert.ToInt64(count!), "Should find user with case-insensitive email search");

        cmd.CommandText = "SELECT COUNT(*) FROM users_expr WHERE UPPER(username) = 'BOB'";
        count = cmd.ExecuteScalar();
        AssertEqual(1L, Convert.ToInt64(count!), "Should find user with case-insensitive username search");

        cmd.CommandText = "EXPLAIN SELECT * FROM users_expr WHERE LOWER(email) = 'charlie@test.com'";
        bool usesExprIndex = false;
        using (DbDataReader reader = cmd.ExecuteReader())
        {
            while (reader.Read())
            {
                string? plan = reader.GetValue(0)?.ToString();
                if (plan != null && (plan.Contains("idx_lower_email") || plan.Contains("Index")))
                {
                    usesExprIndex = true;
                    break;
                }
            }
        }
        AssertTrue(usesExprIndex, "Query should use expression index");

        cmd.CommandText = "SELECT username FROM users_expr WHERE LOWER(email) LIKE '%@example.com'";
        int userCount = 0;
        using (DbDataReader reader = cmd.ExecuteReader())
        {
            while (reader.Read())
            {
                userCount++;
            }
        }
        AssertEqual(2, userCount, "Should find 2 users with @example.com email");

        cmd.CommandText = "INSERT INTO users_expr (email, username) VALUES ('TEST@EXAMPLE.COM', 'test')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM users_expr WHERE LOWER(email) = 'test@example.com'";
        count = cmd.ExecuteScalar();
        AssertEqual(1L, Convert.ToInt64(count!), "Should find newly inserted user");
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS users_expr CASCADE";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());
    }
}
