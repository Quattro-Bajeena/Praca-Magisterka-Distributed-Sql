using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.UserManagement;

[SqlTest(SqlFeatureCategory.UserManagement, "Test PostgreSQL role membership and inheritance", DatabaseType.PostgreSql)]
public class PostgresRoleMembershipTest : SqlTest
{
    protected override void SetupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "CREATE ROLE developers_pg";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());

        cmd.CommandText = "CREATE ROLE employee_pg WITH LOGIN PASSWORD 'emp_pass'";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "GRANT developers_pg TO employee_pg";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM pg_auth_members WHERE roleid = (SELECT oid FROM pg_roles WHERE rolname = 'developers_pg') AND member = (SELECT oid FROM pg_roles WHERE rolname = 'employee_pg')";
        object? count = cmd.ExecuteScalar();
        AssertEqual(1L, Convert.ToInt64(count!), "employee_pg should be member of developers_pg");

        cmd.CommandText = "REVOKE developers_pg FROM employee_pg";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM pg_auth_members WHERE roleid = (SELECT oid FROM pg_roles WHERE rolname = 'developers_pg') AND member = (SELECT oid FROM pg_roles WHERE rolname = 'employee_pg')";
        count = cmd.ExecuteScalar();
        AssertEqual(0L, Convert.ToInt64(count!), "employee_pg should no longer be member of developers_pg");
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "DROP ROLE IF EXISTS employee_pg";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());

        cmd.CommandText = "DROP ROLE IF EXISTS developers_pg";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());
    }
}
