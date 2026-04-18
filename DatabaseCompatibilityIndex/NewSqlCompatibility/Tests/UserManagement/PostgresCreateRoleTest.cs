using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.UserManagement;

[SqlTest(SqlFeatureCategory.UserManagement, "Test PostgreSQL CREATE ROLE and CREATE USER", DatabaseType.PostgreSql)]
public class PostgresCreateRoleTest : SqlTest
{
    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "CREATE ROLE test_role_pg";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM pg_roles WHERE rolname = 'test_role_pg'";
        object? count = cmd.ExecuteScalar();
        AssertEqual(1L, Convert.ToInt64(count!), "Role should be created in pg_roles");

        cmd.CommandText = "SELECT rolcanlogin FROM pg_roles WHERE rolname = 'test_role_pg'";
        object? canLogin = cmd.ExecuteScalar();
        AssertTrue(canLogin != null && !Convert.ToBoolean(canLogin), "ROLE without LOGIN should not be able to login");

        cmd.CommandText = "CREATE USER test_user_pg WITH PASSWORD 'test_password'";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT rolcanlogin FROM pg_roles WHERE rolname = 'test_user_pg'";
        canLogin = cmd.ExecuteScalar();
        AssertTrue(canLogin != null && Convert.ToBoolean(canLogin), "USER should have LOGIN privilege by default");
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP ROLE IF EXISTS test_role_pg";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());
        cmd.CommandText = "DROP USER IF EXISTS test_user_pg";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());
    }
}
