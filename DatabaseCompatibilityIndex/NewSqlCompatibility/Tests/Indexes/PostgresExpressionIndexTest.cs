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
                            email VARCHAR(100)
                        )";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "CREATE INDEX idx_lower_email ON users_expr (LOWER(email))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO users_expr (email) VALUES ('Alice@Example.COM')";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "INSERT INTO users_expr (email) VALUES ('bob@EXAMPLE.com')";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "SELECT COUNT(*) FROM users_expr WHERE LOWER(email) = 'alice@example.com'";
        object? count = cmd.ExecuteScalar();
        AssertEqual(1L, Convert.ToInt64(count!), "Should find user with case-insensitive email search");

        cmd.CommandText = "SELECT COUNT(*) FROM users_expr WHERE LOWER(email) LIKE '%@example.com'";
        count = cmd.ExecuteScalar();
        AssertEqual(2L, Convert.ToInt64(count!), "Should find 2 users with @example.com email");
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS users_expr CASCADE";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());
    }
}
