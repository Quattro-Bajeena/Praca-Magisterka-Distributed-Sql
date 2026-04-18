using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.UserManagement;

[SqlTest(SqlFeatureCategory.UserManagement, "Test PostgreSQL GRANT privileges on tables", DatabaseType.PostgreSql)]
public class PostgresGrantPrivilegesTest : SqlTest
{
    protected override void SetupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "CREATE ROLE test_grant_user_pg WITH LOGIN PASSWORD 'test_pass'";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());

        cmd.CommandText = "CREATE TABLE grant_test_pg (id INT PRIMARY KEY, name VARCHAR(50))";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "GRANT SELECT ON grant_test_pg TO test_grant_user_pg";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"SELECT COUNT(*) FROM information_schema.table_privileges
                           WHERE grantee = 'test_grant_user_pg' AND table_name = 'grant_test_pg' AND privilege_type = 'SELECT'";
        object? count = cmd.ExecuteScalar();
        AssertTrue(count != null && Convert.ToInt64(count!) >= 1, "User should have SELECT privilege");

        cmd.CommandText = "GRANT INSERT, UPDATE ON grant_test_pg TO test_grant_user_pg";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"SELECT COUNT(*) FROM information_schema.table_privileges
                           WHERE grantee = 'test_grant_user_pg' AND table_name = 'grant_test_pg'";
        count = cmd.ExecuteScalar();
        AssertTrue(count != null && Convert.ToInt64(count!) >= 3, "User should have at least 3 privileges");
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "REVOKE ALL PRIVILEGES ON grant_test_pg FROM test_grant_user_pg";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());

        cmd.CommandText = "DROP TABLE IF EXISTS grant_test_pg CASCADE";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());

        cmd.CommandText = "DROP ROLE IF EXISTS test_grant_user_pg";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());
    }
}
