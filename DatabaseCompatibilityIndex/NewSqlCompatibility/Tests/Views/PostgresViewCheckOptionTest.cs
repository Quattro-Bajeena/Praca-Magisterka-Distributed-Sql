using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Views;

[SqlTest(SqlFeatureCategory.Views, "Test PostgreSQL WITH CHECK OPTION on views", DatabaseType.PostgreSql)]
public class PostgresViewCheckOptionTest : SqlTest
{
    protected override void SetupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = @"CREATE TABLE employees_check (
                            id SERIAL PRIMARY KEY,
                            name VARCHAR(100),
                            department VARCHAR(50)
                        )";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO employees_check (name, department) VALUES ('Alice', 'Engineering')";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "INSERT INTO employees_check (name, department) VALUES ('Bob', 'Sales')";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = @"CREATE VIEW engineering_view AS
                           SELECT id, name, department
                           FROM employees_check
                           WHERE department = 'Engineering'
                           WITH CHECK OPTION";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM engineering_view";
        object? count = cmd.ExecuteScalar();
        AssertEqual(1L, Convert.ToInt64(count!), "Engineering view should show 1 employee");

        // Insert through view — valid row should succeed
        cmd.CommandText = "INSERT INTO engineering_view (name, department) VALUES ('David', 'Engineering')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM engineering_view";
        count = cmd.ExecuteScalar();
        AssertEqual(2L, Convert.ToInt64(count!), "Should now have 2 engineering employees");

        // Insert row that violates the view's WHERE clause — should fail
        AssertThrows<Exception>(
            () =>
            {
                using DbCommand badCmd = connection.CreateCommand();
                badCmd.CommandText = "INSERT INTO engineering_view (name, department) VALUES ('Eve', 'Sales')";
                badCmd.ExecuteNonQuery();
            },
            "WITH CHECK OPTION should prevent inserting non-Engineering department");
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP VIEW IF EXISTS engineering_view CASCADE";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());
        cmd.CommandText = "DROP TABLE IF EXISTS employees_check CASCADE";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());
    }
}
