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

        cmd.CommandText = "CREATE ROLE managers_pg";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());

        cmd.CommandText = "CREATE ROLE employee_pg WITH LOGIN PASSWORD 'emp_pass'";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());

        cmd.CommandText = "CREATE TABLE projects_pg (id INT PRIMARY KEY, name VARCHAR(100), status VARCHAR(50))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO projects_pg VALUES (1, 'Project Alpha', 'active')";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "GRANT SELECT ON projects_pg TO developers_pg";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "GRANT INSERT, UPDATE ON projects_pg TO managers_pg";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"SELECT COUNT(*) FROM information_schema.table_privileges 
                           WHERE grantee = 'developers_pg' AND table_name = 'projects_pg'";
        object? count = cmd.ExecuteScalar();
        AssertTrue(count != null && Convert.ToInt64(count!) >= 1, "developers_pg should have table privileges");

        cmd.CommandText = "GRANT developers_pg TO employee_pg";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM pg_auth_members WHERE roleid = (SELECT oid FROM pg_roles WHERE rolname = 'developers_pg') AND member = (SELECT oid FROM pg_roles WHERE rolname = 'employee_pg')";
        count = cmd.ExecuteScalar();
        AssertEqual(1L, Convert.ToInt64(count!), "employee_pg should be member of developers_pg");

        cmd.CommandText = "GRANT managers_pg TO employee_pg WITH ADMIN OPTION";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT admin_option FROM pg_auth_members WHERE roleid = (SELECT oid FROM pg_roles WHERE rolname = 'managers_pg') AND member = (SELECT oid FROM pg_roles WHERE rolname = 'employee_pg')";
        object? adminOption = cmd.ExecuteScalar();
        AssertTrue(adminOption != null && Convert.ToBoolean(adminOption), "employee_pg should have ADMIN OPTION for managers_pg");

        cmd.CommandText = "REVOKE developers_pg FROM employee_pg";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM pg_auth_members WHERE roleid = (SELECT oid FROM pg_roles WHERE rolname = 'developers_pg') AND member = (SELECT oid FROM pg_roles WHERE rolname = 'employee_pg')";
        count = cmd.ExecuteScalar();
        AssertEqual(0L, Convert.ToInt64(count!), "employee_pg should no longer be member of developers_pg");

        cmd.CommandText = "CREATE ROLE team_lead_pg WITH LOGIN PASSWORD 'lead_pass' IN ROLE developers_pg";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM pg_auth_members WHERE roleid = (SELECT oid FROM pg_roles WHERE rolname = 'developers_pg') AND member = (SELECT oid FROM pg_roles WHERE rolname = 'team_lead_pg')";
        count = cmd.ExecuteScalar();
        AssertEqual(1L, Convert.ToInt64(count!), "team_lead_pg should be member of developers_pg via IN ROLE");

        cmd.CommandText = "SELECT COUNT(*) FROM projects_pg";
        object? projectCount = cmd.ExecuteScalar();
        AssertEqual(1L, (long)projectCount!, "Projects table should be accessible");
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "DROP TABLE IF EXISTS projects_pg CASCADE";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());

        cmd.CommandText = "REVOKE ALL PRIVILEGES ON ALL TABLES IN SCHEMA public FROM developers_pg, managers_pg";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());

        cmd.CommandText = "DROP ROLE IF EXISTS team_lead_pg";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());

        cmd.CommandText = "DROP ROLE IF EXISTS employee_pg";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());

        cmd.CommandText = "DROP ROLE IF EXISTS managers_pg";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());

        cmd.CommandText = "DROP ROLE IF EXISTS developers_pg";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());
    }
}
