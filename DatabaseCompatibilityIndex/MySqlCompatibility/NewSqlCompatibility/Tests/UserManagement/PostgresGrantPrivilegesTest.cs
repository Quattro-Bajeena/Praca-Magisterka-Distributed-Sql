using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.UserManagement;

[SqlTest(SqlFeatureCategory.UserManagement, "Test PostgreSQL GRANT privileges on tables and schemas", DatabaseType.PostgreSql)]
public class PostgresGrantPrivilegesTest : SqlTest
{
    protected override void SetupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        
        cmd.CommandText = "CREATE ROLE test_grant_user_pg WITH LOGIN PASSWORD 'test_pass'";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());

        cmd.CommandText = "CREATE TABLE grant_test_pg (id INT PRIMARY KEY, name VARCHAR(50), value INT)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO grant_test_pg VALUES (1, 'test', 100)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "CREATE SCHEMA test_schema_pg";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());

        cmd.CommandText = "CREATE TABLE test_schema_pg.schema_table (id INT, data VARCHAR(100))";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());
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

        cmd.CommandText = "GRANT ALL PRIVILEGES ON grant_test_pg TO test_grant_user_pg";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "GRANT USAGE ON SCHEMA test_schema_pg TO test_grant_user_pg";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "GRANT SELECT ON ALL TABLES IN SCHEMA test_schema_pg TO test_grant_user_pg";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"SELECT COUNT(*) FROM information_schema.table_privileges 
                           WHERE grantee = 'test_grant_user_pg' AND table_schema = 'test_schema_pg'";
        count = cmd.ExecuteScalar();
        AssertTrue(count != null && Convert.ToInt64(count!) >= 1, "User should have privileges on schema tables");

        cmd.CommandText = "GRANT DELETE ON grant_test_pg TO test_grant_user_pg WITH GRANT OPTION";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM grant_test_pg";
        object? tableCount = cmd.ExecuteScalar();
        AssertEqual(1L, (long)tableCount!, "Table should have 1 row");
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        
        cmd.CommandText = "REVOKE ALL PRIVILEGES ON grant_test_pg FROM test_grant_user_pg";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());

        cmd.CommandText = "DROP TABLE IF EXISTS grant_test_pg CASCADE";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());

        cmd.CommandText = "DROP SCHEMA IF EXISTS test_schema_pg CASCADE";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());

        cmd.CommandText = "DROP ROLE IF EXISTS test_grant_user_pg";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());
    }
}
