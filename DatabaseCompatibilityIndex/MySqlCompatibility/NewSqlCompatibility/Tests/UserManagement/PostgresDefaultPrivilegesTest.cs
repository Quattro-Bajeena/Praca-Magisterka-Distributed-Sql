using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.UserManagement;

[SqlTest(SqlFeatureCategory.UserManagement, "Test PostgreSQL default privileges and schema permissions", DatabaseType.PostgreSql)]
public class PostgresDefaultPrivilegesTest : SqlTest
{
    protected override void SetupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        
        cmd.CommandText = "CREATE SCHEMA app_schema_pg";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());

        cmd.CommandText = "CREATE ROLE app_user_pg WITH LOGIN PASSWORD 'app_pass'";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());

        cmd.CommandText = "CREATE ROLE app_reader_pg WITH LOGIN PASSWORD 'reader_pass'";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "GRANT USAGE ON SCHEMA app_schema_pg TO app_user_pg";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "GRANT CREATE ON SCHEMA app_schema_pg TO app_user_pg";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "ALTER DEFAULT PRIVILEGES IN SCHEMA app_schema_pg GRANT SELECT ON TABLES TO app_reader_pg";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "ALTER DEFAULT PRIVILEGES IN SCHEMA app_schema_pg GRANT ALL ON TABLES TO app_user_pg";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"SELECT COUNT(*) FROM pg_default_acl 
                           WHERE defaclnamespace = (SELECT oid FROM pg_namespace WHERE nspname = 'app_schema_pg')";
        object? count = cmd.ExecuteScalar();
        AssertTrue(count != null && Convert.ToInt64(count!) >= 1, "Should have default privileges configured");

        cmd.CommandText = "ALTER DEFAULT PRIVILEGES IN SCHEMA app_schema_pg GRANT USAGE ON SEQUENCES TO app_user_pg";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "CREATE TABLE app_schema_pg.test_table (id SERIAL PRIMARY KEY, name VARCHAR(100))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO app_schema_pg.test_table (name) VALUES ('test record')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"SELECT COUNT(*) FROM information_schema.table_privileges 
                           WHERE grantee = 'app_reader_pg' AND table_schema = 'app_schema_pg' AND table_name = 'test_table'";
        count = cmd.ExecuteScalar();
        AssertTrue(count != null && Convert.ToInt64(count!) >= 1, "app_reader_pg should have SELECT privilege on new table via default privileges");

        cmd.CommandText = @"SELECT COUNT(*) FROM information_schema.table_privileges 
                           WHERE grantee = 'app_user_pg' AND table_schema = 'app_schema_pg' AND table_name = 'test_table'";
        count = cmd.ExecuteScalar();
        AssertTrue(count != null && Convert.ToInt64(count!) >= 1, "app_user_pg should have privileges on new table via default privileges");

        cmd.CommandText = "CREATE TABLE app_schema_pg.another_table (id INT, value TEXT)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"SELECT COUNT(*) FROM information_schema.table_privileges 
                           WHERE grantee = 'app_reader_pg' AND table_schema = 'app_schema_pg' AND table_name = 'another_table'";
        count = cmd.ExecuteScalar();
        AssertTrue(count != null && Convert.ToInt64(count!) >= 1, "Default privileges should apply to second table too");

        cmd.CommandText = "GRANT USAGE ON SCHEMA app_schema_pg TO app_reader_pg";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM app_schema_pg.test_table";
        object? tableCount = cmd.ExecuteScalar();
        AssertEqual(1L, Convert.ToInt64(tableCount!), "Should be able to access schema tables");
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        
        cmd.CommandText = "DROP SCHEMA IF EXISTS app_schema_pg CASCADE";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());

        cmd.CommandText = "DROP ROLE IF EXISTS app_user_pg";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());

        cmd.CommandText = "DROP ROLE IF EXISTS app_reader_pg";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());
    }
}
