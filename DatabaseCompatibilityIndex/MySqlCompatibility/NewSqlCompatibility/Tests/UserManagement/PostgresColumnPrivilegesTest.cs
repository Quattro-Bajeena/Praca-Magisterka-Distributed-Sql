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
                            email VARCHAR(100),
                            salary DECIMAL(10,2),
                            ssn VARCHAR(11)
                        )";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO sensitive_data_pg (name, email, salary, ssn) VALUES ('Alice', 'alice@example.com', 50000, '123-45-6789')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO sensitive_data_pg (name, email, salary, ssn) VALUES ('Bob', 'bob@example.com', 60000, '987-65-4321')";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "GRANT SELECT (id, name, email) ON sensitive_data_pg TO test_col_user_pg";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"SELECT COUNT(*) FROM information_schema.column_privileges 
                           WHERE grantee = 'test_col_user_pg' AND table_name = 'sensitive_data_pg' AND privilege_type = 'SELECT'";
        object? count = cmd.ExecuteScalar();
        AssertEqual(3L, Convert.ToInt64(count!), "User should have SELECT on 3 columns (id, name, email)");

        cmd.CommandText = "GRANT UPDATE (email) ON sensitive_data_pg TO test_col_user_pg";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"SELECT COUNT(*) FROM information_schema.column_privileges 
                           WHERE grantee = 'test_col_user_pg' AND table_name = 'sensitive_data_pg' AND column_name = 'email'";
        count = cmd.ExecuteScalar();
        AssertTrue(Convert.ToInt64(count!) >= 2, "Email column should have both SELECT and UPDATE privileges");

        cmd.CommandText = @"SELECT COUNT(*) FROM information_schema.column_privileges 
                           WHERE grantee = 'test_col_user_pg' AND table_name = 'sensitive_data_pg' AND column_name IN ('salary', 'ssn')";
        count = cmd.ExecuteScalar();
        AssertEqual(0L, Convert.ToInt64(count!), "User should NOT have access to salary or ssn columns");

        cmd.CommandText = "REVOKE SELECT (name) ON sensitive_data_pg FROM test_col_user_pg";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"SELECT COUNT(*) FROM information_schema.column_privileges 
                           WHERE grantee = 'test_col_user_pg' AND table_name = 'sensitive_data_pg' AND column_name = 'name' AND privilege_type = 'SELECT'";
        count = cmd.ExecuteScalar();
        AssertEqual(0L, Convert.ToInt64(count!), "Name column SELECT should be revoked");

        cmd.CommandText = "SELECT COUNT(*) FROM sensitive_data_pg";
        object? tableCount = cmd.ExecuteScalar();
        AssertEqual(2L, (long)tableCount!, "Table should have 2 rows");

        cmd.CommandText = @"GRANT INSERT (name, email) ON sensitive_data_pg TO test_col_user_pg";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"SELECT COUNT(*) FROM information_schema.column_privileges 
                           WHERE grantee = 'test_col_user_pg' AND table_name = 'sensitive_data_pg'";
        count = cmd.ExecuteScalar();
        AssertTrue(Convert.ToInt64(count!) >= 4, "User should have multiple column privileges");
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
