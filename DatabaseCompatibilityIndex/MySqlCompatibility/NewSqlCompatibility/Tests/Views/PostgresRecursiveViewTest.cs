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
                            manager_id INT,
                            title VARCHAR(100)
                        )";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO employees_hierarchy VALUES (1, 'CEO', NULL, 'Chief Executive Officer')";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "INSERT INTO employees_hierarchy VALUES (2, 'CTO', 1, 'Chief Technology Officer')";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "INSERT INTO employees_hierarchy VALUES (3, 'CFO', 1, 'Chief Financial Officer')";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "INSERT INTO employees_hierarchy VALUES (4, 'Dev Lead', 2, 'Development Lead')";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "INSERT INTO employees_hierarchy VALUES (5, 'Senior Dev', 4, 'Senior Developer')";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "INSERT INTO employees_hierarchy VALUES (6, 'Junior Dev', 4, 'Junior Developer')";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "INSERT INTO employees_hierarchy VALUES (7, 'Accountant', 3, 'Senior Accountant')";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = @"CREATE VIEW employee_tree AS
                           WITH RECURSIVE tree AS (
                               SELECT id, name, manager_id, title, 0 as level, ARRAY[id] as path
                               FROM employees_hierarchy
                               WHERE manager_id IS NULL
                               UNION ALL
                               SELECT e.id, e.name, e.manager_id, e.title, t.level + 1, t.path || e.id
                               FROM employees_hierarchy e
                               JOIN tree t ON e.manager_id = t.id
                           )
                           SELECT id, name, manager_id, title, level, path FROM tree";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM employee_tree";
        object? count = cmd.ExecuteScalar();
        AssertEqual(7L, Convert.ToInt64(count!), "Recursive view should show all 7 employees");

        cmd.CommandText = "SELECT MAX(level) FROM employee_tree";
        object? maxLevel = cmd.ExecuteScalar();
        AssertEqual(3, Convert.ToInt32(maxLevel!), "Maximum hierarchy level should be 3");

        cmd.CommandText = "SELECT COUNT(*) FROM employee_tree WHERE level = 0";
        count = cmd.ExecuteScalar();
        AssertEqual(1L, Convert.ToInt64(count!), "Should have 1 CEO at level 0");

        cmd.CommandText = "SELECT COUNT(*) FROM employee_tree WHERE level = 1";
        count = cmd.ExecuteScalar();
        AssertEqual(2L, Convert.ToInt64(count!), "Should have 2 executives at level 1");

        cmd.CommandText = "SELECT COUNT(*) FROM employee_tree WHERE level = 2";
        count = cmd.ExecuteScalar();
        AssertTrue(Convert.ToInt64(count!) >= 2, "Should have at least 2 employees at level 2");

        cmd.CommandText = "SELECT name FROM employee_tree WHERE level = 3 ORDER BY name";
        using (DbDataReader reader = cmd.ExecuteReader())
        {
            AssertTrue(reader.Read(), "Should have employees at level 3");
            string name = reader.GetString(0);
            AssertTrue(name == "Junior Dev" || name == "Senior Dev", "Level 3 should be developers");
        }

        cmd.CommandText = "SELECT array_length(path, 1) as path_length FROM employee_tree WHERE name = 'Junior Dev'";
        object? pathLength = cmd.ExecuteScalar();
        AssertEqual(4, Convert.ToInt32(pathLength!), "Junior Dev should have path length of 4 (CEO->CTO->Dev Lead->Junior Dev)");

        cmd.CommandText = "INSERT INTO employees_hierarchy VALUES (8, 'Intern', 6, 'Development Intern')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT MAX(level) FROM employee_tree";
        maxLevel = cmd.ExecuteScalar();
        AssertEqual(4, Convert.ToInt32(maxLevel!), "After adding intern, max level should be 4");

        cmd.CommandText = "SELECT COUNT(*) FROM employee_tree WHERE level = 4";
        count = cmd.ExecuteScalar();
        AssertEqual(1L, Convert.ToInt64(count!), "Should have 1 intern at level 4");
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
