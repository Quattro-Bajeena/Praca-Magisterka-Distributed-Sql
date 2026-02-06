using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Joins;

[SqlTest(SqlFeatureCategory.Joins, "Test LEFT JOIN")]
public class LeftJoinTest : SqlTest
{
    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "CREATE TABLE departments (id INT PRIMARY KEY, name VARCHAR(50))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "CREATE TABLE employees (id INT PRIMARY KEY, dept_id INT, name VARCHAR(50))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO departments VALUES (1, 'HR'), (2, 'IT'), (3, 'Sales')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO employees VALUES (1, 1, 'John'), (2, 1, 'Jane'), (3, 2, 'Bob')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM departments d LEFT JOIN employees e ON d.id = e.dept_id WHERE d.id = 3";
        object? count = cmd.ExecuteScalar();
        AssertEqual(1L, (long)count!, "LEFT JOIN should include empty departments");

        cmd.CommandText = "DROP TABLE employees";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "DROP TABLE departments";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "CREATE TABLE departments (id INT PRIMARY KEY, name VARCHAR(50))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "CREATE TABLE employees (id INT PRIMARY KEY, dept_id INT, name VARCHAR(50))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO departments VALUES (1, 'HR'), (2, 'IT'), (3, 'Sales')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO employees VALUES (1, 1, 'John'), (2, 1, 'Jane'), (3, 2, 'Bob')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM departments d LEFT JOIN employees e ON d.id = e.dept_id WHERE d.id = 3";
        object? count = cmd.ExecuteScalar();
        AssertEqual(1L, (long)count!, "LEFT JOIN should include empty departments");

        cmd.CommandText = "DROP TABLE employees";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "DROP TABLE departments";
        cmd.ExecuteNonQuery();
    }
}
