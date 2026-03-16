using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Subqueries;

[SqlTest(SqlFeatureCategory.Subqueries, "Test scalar subquery")]
public class ScalarSubqueryTest : SqlTest
{
    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "CREATE TABLE employees_sub (id INT PRIMARY KEY, name VARCHAR(50), salary DECIMAL(10,2))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO employees_sub VALUES (1, 'Alice', 50000), (2, 'Bob', 60000), (3, 'Charlie', 55000)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT salary FROM employees_sub WHERE salary > (SELECT AVG(salary) FROM employees_sub)";
        using (DbDataReader reader = cmd.ExecuteReader())
        {
            int count = 0;
            while (reader.Read())
                count++;
            AssertEqual(1, count, "Scalar subquery should return 1 row");
        }

        cmd.CommandText = "DROP TABLE employees_sub";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "CREATE TABLE employees_sub (id INT PRIMARY KEY, name VARCHAR(50), salary DECIMAL(10,2))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO employees_sub VALUES (1, 'Alice', 50000), (2, 'Bob', 60000), (3, 'Charlie', 55000)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT salary FROM employees_sub WHERE salary > (SELECT AVG(salary) FROM employees_sub)";
        using (DbDataReader reader = cmd.ExecuteReader())
        {
            int count = 0;
            while (reader.Read())
                count++;
            AssertEqual(1, count, "Scalar subquery should return 1 row");
        }

        cmd.CommandText = "DROP TABLE employees_sub";
        cmd.ExecuteNonQuery();
    }
}
