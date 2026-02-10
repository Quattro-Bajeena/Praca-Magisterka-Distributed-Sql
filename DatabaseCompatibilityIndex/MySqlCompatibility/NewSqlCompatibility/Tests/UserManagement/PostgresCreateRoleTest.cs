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

        cmd.CommandText = "SELECT COUNT(*) FROM pg_user WHERE usename = 'test_user_pg'";
        count = cmd.ExecuteScalar();
        AssertEqual(1L, Convert.ToInt64(count!), "User should be created in pg_user");

        cmd.CommandText = "SELECT rolcanlogin FROM pg_roles WHERE rolname = 'test_user_pg'";
        canLogin = cmd.ExecuteScalar();
        AssertTrue(canLogin != null && Convert.ToBoolean(canLogin), "USER should have LOGIN privilege by default");

        cmd.CommandText = "CREATE ROLE test_admin_pg WITH LOGIN PASSWORD 'admin_pass' SUPERUSER CREATEDB CREATEROLE";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT rolsuper, rolcreatedb, rolcreaterole FROM pg_roles WHERE rolname = 'test_admin_pg'";
        using (DbDataReader reader = cmd.ExecuteReader())
        {
            AssertTrue(reader.Read(), "Should find admin role");
            bool isSuperuser = reader.GetBoolean(0);
            bool canCreateDb = reader.GetBoolean(1);
            bool canCreateRole = reader.GetBoolean(2);
            AssertTrue(isSuperuser, "Admin should have SUPERUSER attribute");
            AssertTrue(canCreateDb, "Admin should have CREATEDB attribute");
            AssertTrue(canCreateRole, "Admin should have CREATEROLE attribute");
        }

        cmd.CommandText = "CREATE ROLE test_readonly_pg WITH LOGIN PASSWORD 'readonly_pass' NOINHERIT";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT rolinherit FROM pg_roles WHERE rolname = 'test_readonly_pg'";
        object? inherit = cmd.ExecuteScalar();
        AssertTrue(inherit != null && !Convert.ToBoolean(inherit), "Role with NOINHERIT should not inherit privileges");

        cmd.CommandText = "SELECT COUNT(*) FROM pg_roles WHERE rolname LIKE 'test_%_pg'";
        count = cmd.ExecuteScalar();
        AssertEqual(4L, Convert.ToInt64(count!), "Should have created 4 test roles/users");
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP ROLE IF EXISTS test_role_pg";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());
        cmd.CommandText = "DROP USER IF EXISTS test_user_pg";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());
        cmd.CommandText = "DROP ROLE IF EXISTS test_admin_pg";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());
        cmd.CommandText = "DROP ROLE IF EXISTS test_readonly_pg";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());
    }
}
