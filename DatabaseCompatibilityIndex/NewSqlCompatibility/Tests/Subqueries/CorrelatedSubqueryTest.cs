using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Subqueries;

[SqlTest(SqlFeatureCategory.Subqueries, "Test correlated subquery")]
public class CorrelatedSubqueryTest : SqlTest
{
    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "CREATE TABLE departments_cor (id INT PRIMARY KEY, name VARCHAR(50))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "CREATE TABLE employees_cor (id INT PRIMARY KEY, dept_id INT, salary DECIMAL(10,2))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO departments_cor VALUES (1, 'HR'), (2, 'IT')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO employees_cor VALUES (1, 1, 50000), (2, 1, 55000), (3, 2, 70000), (4, 2, 75000)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM employees_cor e WHERE salary > (SELECT AVG(salary) FROM employees_cor WHERE dept_id = e.dept_id)";
        object? count = cmd.ExecuteScalar();
        AssertEqual(2L, Convert.ToInt64(count!), "Correlated subquery should find 2 above-average salary employees");

        cmd.CommandText = "DROP TABLE employees_cor";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "DROP TABLE departments_cor";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "CREATE TABLE departments_cor (id INT PRIMARY KEY, name VARCHAR(50))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "CREATE TABLE employees_cor (id INT PRIMARY KEY, dept_id INT, salary DECIMAL(10,2))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO departments_cor VALUES (1, 'HR'), (2, 'IT')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO employees_cor VALUES (1, 1, 50000), (2, 1, 55000), (3, 2, 70000), (4, 2, 75000)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM employees_cor e WHERE salary > (SELECT AVG(salary) FROM employees_cor WHERE dept_id = e.dept_id)";
        object? count = cmd.ExecuteScalar();
        AssertEqual(2L, Convert.ToInt64(count!), "Correlated subquery should find 2 above-average salary employees");

        cmd.CommandText = "DROP TABLE employees_cor";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "DROP TABLE departments_cor";
        cmd.ExecuteNonQuery();
    }
}
