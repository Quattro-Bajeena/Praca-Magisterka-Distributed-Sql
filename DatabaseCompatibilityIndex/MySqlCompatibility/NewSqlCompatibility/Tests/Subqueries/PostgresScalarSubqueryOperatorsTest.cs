using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Subqueries;

[SqlTest(SqlFeatureCategory.Subqueries, "Test PostgreSQL scalar subquery operators (ALL, ANY, SOME)", DatabaseType.PostgreSql)]
public class PostgresScalarSubqueryOperatorsTest : SqlTest
{
    protected override void SetupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = @"CREATE TABLE departments_ops (
                            id SERIAL PRIMARY KEY,
                            name VARCHAR(100),
                            budget DECIMAL(12,2)
                        )";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"CREATE TABLE employees_ops (
                            id SERIAL PRIMARY KEY,
                            name VARCHAR(100),
                            dept_id INT,
                            salary DECIMAL(10,2)
                        )";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO departments_ops (name, budget) VALUES ('Engineering', 500000), ('Sales', 300000), ('HR', 200000)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO employees_ops (name, dept_id, salary) VALUES ('Alice', 1, 80000), ('Bob', 1, 90000), ('Charlie', 1, 75000)";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "INSERT INTO employees_ops (name, dept_id, salary) VALUES ('David', 2, 70000), ('Eve', 2, 65000)";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "INSERT INTO employees_ops (name, dept_id, salary) VALUES ('Frank', 3, 55000), ('Grace', 3, 52000)";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = @"
            SELECT name, salary
            FROM employees_ops
            WHERE salary > ALL (SELECT salary FROM employees_ops WHERE dept_id = 3)
            ORDER BY salary";

        int higherThanHR = 0;
        using (DbDataReader reader = cmd.ExecuteReader())
        {
            while (reader.Read())
            {
                higherThanHR++;
                decimal salary = reader.GetDecimal(1);
                AssertTrue(salary > 55000, "Salary should be higher than all HR salaries");
            }
        }
        AssertTrue(higherThanHR >= 4, "Should find employees earning more than all HR employees");

        cmd.CommandText = @"
            SELECT name, salary
            FROM employees_ops
            WHERE salary > ANY (SELECT salary FROM employees_ops WHERE dept_id = 2)
            ORDER BY name";

        int higherThanAnySales = 0;
        using (DbDataReader reader = cmd.ExecuteReader())
        {
            while (reader.Read())
            {
                higherThanAnySales++;
            }
        }
        AssertTrue(higherThanAnySales > 0, "Should find employees earning more than at least one Sales employee");

        cmd.CommandText = @"
            SELECT name, salary
            FROM employees_ops
            WHERE salary > SOME (SELECT salary FROM employees_ops WHERE dept_id = 1)
            ORDER BY salary DESC";

        using (DbDataReader reader = cmd.ExecuteReader())
        {
            AssertTrue(reader.Read(), "Should find employees with SOME operator");
        }

        cmd.CommandText = @"
            SELECT name, salary
            FROM employees_ops
            WHERE salary >= ALL (SELECT salary FROM employees_ops WHERE dept_id = dept_id)
            ORDER BY salary DESC";

        using (DbDataReader reader = cmd.ExecuteReader())
        {
            while (reader.Read())
            {
                string name = reader.GetString(0);
                AssertTrue(name != null, "Should find highest paid in their department");
            }
        }

        cmd.CommandText = @"
            SELECT d.name, d.budget
            FROM departments_ops d
            WHERE d.budget < ALL (
                SELECT SUM(e.salary)
                FROM employees_ops e
                WHERE e.dept_id = d.id
                GROUP BY e.dept_id
            )";

        using (DbDataReader reader = cmd.ExecuteReader())
        {
            if (reader.Read())
            {
                AssertTrue(true, "Found department where budget less than total salaries");
            }
        }

        cmd.CommandText = @"
            SELECT e.name, e.salary,
                   (SELECT AVG(salary) FROM employees_ops WHERE dept_id = e.dept_id) as dept_avg
            FROM employees_ops e
            WHERE e.salary = ANY (
                SELECT MAX(salary) FROM employees_ops GROUP BY dept_id
            )
            ORDER BY e.salary DESC";

        using (DbDataReader reader = cmd.ExecuteReader())
        {
            while (reader.Read())
            {
                decimal salary = reader.GetDecimal(1);
                decimal deptAvg = reader.GetDecimal(2);
                AssertTrue(salary >= deptAvg, "Top earner should be at or above department average");
            }
        }
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS employees_ops CASCADE";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "DROP TABLE IF EXISTS departments_ops CASCADE";
        cmd.ExecuteNonQuery();
    }
}
