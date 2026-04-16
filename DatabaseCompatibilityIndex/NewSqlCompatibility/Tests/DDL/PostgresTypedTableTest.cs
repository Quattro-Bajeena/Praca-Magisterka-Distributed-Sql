using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.TableDefinition;

[SqlTest(SqlFeatureCategory.DDL, "Test typed table creation using OF type_name", DatabaseType.PostgreSql)]
public class PostgresTypedTableTest : SqlTest
{
    protected override void SetupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = @"CREATE TYPE employee_type AS (
            employee_id INT,
            full_name   TEXT,
            salary      NUMERIC(10, 2)
        )";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "CREATE TABLE employees OF employee_type";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO employees VALUES (1, 'Alice', 75000.00)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO employees VALUES (2, 'Bob', 82000.50)";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "SELECT COUNT(*) FROM employees";
        long count = Convert.ToInt64(cmd.ExecuteScalar()!);
        AssertEqual(2L, count, "Typed table should contain 2 rows");

        cmd.CommandText = "SELECT full_name FROM employees WHERE employee_id = 1";
        object? name = cmd.ExecuteScalar();
        AssertEqual("Alice", name?.ToString(), "First employee should be Alice");

        // reloftype != 0 confirms the table is a typed table
        cmd.CommandText = "SELECT reloftype != 0 FROM pg_class WHERE oid = 'employees'::regclass";
        object? isTyped = cmd.ExecuteScalar();
        AssertEqual(true, Convert.ToBoolean(isTyped!), "pg_class.reloftype should be non-zero for a typed table");
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "DROP TABLE IF EXISTS employees CASCADE";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());

        cmd.CommandText = "DROP TYPE IF EXISTS employee_type CASCADE";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());
    }
}
