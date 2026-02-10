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
                            department VARCHAR(50),
                            salary DECIMAL(10,2)
                        )";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO employees_check (name, department, salary) VALUES ('Alice', 'Engineering', 75000)";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "INSERT INTO employees_check (name, department, salary) VALUES ('Bob', 'Engineering', 80000)";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "INSERT INTO employees_check (name, department, salary) VALUES ('Charlie', 'Sales', 60000)";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = @"CREATE VIEW engineering_view AS
                           SELECT id, name, department, salary
                           FROM employees_check
                           WHERE department = 'Engineering'
                           WITH CHECK OPTION";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM engineering_view";
        object? count = cmd.ExecuteScalar();
        AssertEqual(2L, Convert.ToInt64(count!), "Engineering view should show 2 employees");

        cmd.CommandText = "INSERT INTO engineering_view (name, department, salary) VALUES ('David', 'Engineering', 85000)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM engineering_view";
        count = cmd.ExecuteScalar();
        AssertEqual(3L, Convert.ToInt64(count!), "Should have 3 engineering employees");

        bool checkOptionViolated = false;
        try
        {
            cmd.CommandText = "INSERT INTO engineering_view (name, department, salary) VALUES ('Eve', 'Sales', 70000)";
            cmd.ExecuteNonQuery();
        }
        catch
        {
            checkOptionViolated = true;
        }
        AssertTrue(checkOptionViolated, "WITH CHECK OPTION should prevent inserting non-Engineering department");

        cmd.CommandText = "UPDATE engineering_view SET salary = 90000 WHERE name = 'Alice'";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT salary FROM employees_check WHERE name = 'Alice'";
        object? salary = cmd.ExecuteScalar();
        AssertEqual(90000m, Convert.ToDecimal(salary!), "Update through view should work");

        bool updateViolation = false;
        try
        {
            cmd.CommandText = "UPDATE engineering_view SET department = 'HR' WHERE name = 'Alice'";
            cmd.ExecuteNonQuery();
        }
        catch
        {
            updateViolation = true;
        }
        AssertTrue(updateViolation, "WITH CHECK OPTION should prevent updating department to non-Engineering");

        cmd.CommandText = "SELECT COUNT(*) FROM employees_check";
        count = cmd.ExecuteScalar();
        AssertEqual(4L, Convert.ToInt64(count!), "Base table should have 4 total employees");
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
