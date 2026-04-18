using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.UserManagement;

[SqlTest(SqlFeatureCategory.UserManagement, "Test PostgreSQL column-level privileges", DatabaseType.PostgreSql)]
public class PostgresColumnPrivilegesTest : SqlTest
{
    protected override void SetupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "CREATE ROLE test_col_user_pg WITH LOGIN PASSWORD 'col_pass'";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());

        cmd.CommandText = @"CREATE TABLE sensitive_data_pg (
                            id SERIAL PRIMARY KEY,
                            name VARCHAR(100),
                            salary DECIMAL(10,2)
                        )";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "GRANT SELECT (id, name) ON sensitive_data_pg TO test_col_user_pg";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"SELECT COUNT(*) FROM information_schema.column_privileges
                           WHERE grantee = 'test_col_user_pg' AND table_name = 'sensitive_data_pg' AND privilege_type = 'SELECT'";
        object? count = cmd.ExecuteScalar();
        AssertEqual(2L, Convert.ToInt64(count!), "User should have SELECT on 2 columns (id, name)");

        cmd.CommandText = @"SELECT COUNT(*) FROM information_schema.column_privileges
                           WHERE grantee = 'test_col_user_pg' AND table_name = 'sensitive_data_pg' AND column_name = 'salary'";
        count = cmd.ExecuteScalar();
        AssertEqual(0L, Convert.ToInt64(count!), "User should NOT have access to salary column");
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "REVOKE ALL PRIVILEGES ON sensitive_data_pg FROM test_col_user_pg";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());

        cmd.CommandText = "DROP TABLE IF EXISTS sensitive_data_pg CASCADE";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());

        cmd.CommandText = "DROP ROLE IF EXISTS test_col_user_pg";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());
    }
}
