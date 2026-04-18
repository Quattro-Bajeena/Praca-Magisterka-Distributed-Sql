using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Views;

[SqlTest(SqlFeatureCategory.Views, "Test PostgreSQL recursive views", DatabaseType.PostgreSql)]
public class PostgresRecursiveViewTest : SqlTest
{
    protected override void SetupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = @"CREATE TABLE employees_hierarchy (
                            id INT PRIMARY KEY,
                            name VARCHAR(100),
                            manager_id INT
                        )";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO employees_hierarchy VALUES (1, 'CEO', NULL)";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "INSERT INTO employees_hierarchy VALUES (2, 'CTO', 1)";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "INSERT INTO employees_hierarchy VALUES (3, 'Dev Lead', 2)";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "INSERT INTO employees_hierarchy VALUES (4, 'Developer', 3)";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = @"CREATE VIEW employee_tree AS
                           WITH RECURSIVE tree AS (
                               SELECT id, name, manager_id, 0 as level
                               FROM employees_hierarchy
                               WHERE manager_id IS NULL
                               UNION ALL
                               SELECT e.id, e.name, e.manager_id, t.level + 1
                               FROM employees_hierarchy e
                               JOIN tree t ON e.manager_id = t.id
                           )
                           SELECT id, name, manager_id, level FROM tree";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM employee_tree";
        object? count = cmd.ExecuteScalar();
        AssertEqual(4L, Convert.ToInt64(count!), "Recursive view should show all 4 employees");

        cmd.CommandText = "SELECT MAX(level) FROM employee_tree";
        object? maxLevel = cmd.ExecuteScalar();
        AssertEqual(3, Convert.ToInt32(maxLevel!), "Maximum hierarchy level should be 3");
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP VIEW IF EXISTS employee_tree CASCADE";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());
        cmd.CommandText = "DROP TABLE IF EXISTS employees_hierarchy CASCADE";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());
    }
}
