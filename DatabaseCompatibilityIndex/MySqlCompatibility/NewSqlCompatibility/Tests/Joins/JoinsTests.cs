using NSCI.Testing;using NSCI.Configuration;
using System.Data.Common;

namespace NSCI.Tests.Joins;

[SqlTest(SqlFeatureCategory.Joins, "Test INNER JOIN", DatabaseType.MySql)]
public class InnerJoinTest : SqlTest
{
    public override void Execute(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        // Create tables
        cmd.CommandText = "CREATE TABLE authors (id INT PRIMARY KEY, name VARCHAR(50))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "CREATE TABLE books (id INT PRIMARY KEY, author_id INT, title VARCHAR(100))";
        cmd.ExecuteNonQuery();

        // Insert data
        cmd.CommandText = "INSERT INTO authors VALUES (1, 'Author A'), (2, 'Author B')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO books VALUES (1, 1, 'Book 1'), (2, 1, 'Book 2'), (3, 2, 'Book 3')";
        cmd.ExecuteNonQuery();

        // Execute INNER JOIN
        cmd.CommandText = "SELECT COUNT(*) FROM authors a INNER JOIN books b ON a.id = b.author_id WHERE a.id = 1";
        object? count = cmd.ExecuteScalar();
        AssertEqual(2L, (long)count!, "INNER JOIN should return 2 books for Author A");

        // Cleanup
        cmd.CommandText = "DROP TABLE books";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "DROP TABLE authors";
        cmd.ExecuteNonQuery();
    }
}

[SqlTest(SqlFeatureCategory.Joins, "Test LEFT JOIN", DatabaseType.MySql)]
public class LeftJoinTest : SqlTest
{
    public override void Execute(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        // Create tables
        cmd.CommandText = "CREATE TABLE departments (id INT PRIMARY KEY, name VARCHAR(50))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "CREATE TABLE employees (id INT PRIMARY KEY, dept_id INT, name VARCHAR(50))";
        cmd.ExecuteNonQuery();

        // Insert data
        cmd.CommandText = "INSERT INTO departments VALUES (1, 'HR'), (2, 'IT'), (3, 'Sales')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO employees VALUES (1, 1, 'John'), (2, 1, 'Jane'), (3, 2, 'Bob')";
        cmd.ExecuteNonQuery();

        // Execute LEFT JOIN
        cmd.CommandText = "SELECT COUNT(*) FROM departments d LEFT JOIN employees e ON d.id = e.dept_id WHERE d.id = 3";
        object? count = cmd.ExecuteScalar();
        AssertEqual(1L, (long)count!, "LEFT JOIN should include empty departments");

        // Cleanup
        cmd.CommandText = "DROP TABLE employees";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "DROP TABLE departments";
        cmd.ExecuteNonQuery();
    }
}

[SqlTest(SqlFeatureCategory.Joins, "Test RIGHT JOIN (if supported)", DatabaseType.MySql)]
public class RightJoinTest : SqlTest
{
    public override void Setup(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "CREATE TABLE left_t (id INT PRIMARY KEY, name VARCHAR(50))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "CREATE TABLE right_t (id INT PRIMARY KEY, left_id INT, value INT)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO left_t VALUES (1, 'A'), (2, 'B')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO right_t VALUES (1, 1, 100), (2, 1, 200), (3, 3, 300)";
        cmd.ExecuteNonQuery();
    }

    public override void Execute(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        // Execute RIGHT JOIN
        cmd.CommandText = "SELECT COUNT(*) FROM left_t l RIGHT JOIN right_t r ON l.id = r.left_id";
        object? count = cmd.ExecuteScalar();
        AssertEqual(3L, (long)count!, "RIGHT JOIN should work correctly");
    }

    public override void Cleanup(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE right_t";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "DROP TABLE left_t";
        cmd.ExecuteNonQuery();
    }
}

[SqlTest(SqlFeatureCategory.Joins, "Test CROSS JOIN", DatabaseType.MySql)]
public class CrossJoinTest : SqlTest
{
    public override void Execute(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        // Create tables
        cmd.CommandText = "CREATE TABLE colors (id INT PRIMARY KEY, color VARCHAR(20))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "CREATE TABLE sizes (id INT PRIMARY KEY, size VARCHAR(20))";
        cmd.ExecuteNonQuery();

        // Insert data
        cmd.CommandText = "INSERT INTO colors VALUES (1, 'Red'), (2, 'Blue'), (3, 'Green')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO sizes VALUES (1, 'S'), (2, 'M'), (3, 'L')";
        cmd.ExecuteNonQuery();

        // Execute CROSS JOIN
        cmd.CommandText = "SELECT COUNT(*) FROM colors CROSS JOIN sizes";
        object? count = cmd.ExecuteScalar();
        AssertEqual(9L, (long)count!, "CROSS JOIN should return 3x3=9 combinations");

        // Cleanup
        cmd.CommandText = "DROP TABLE sizes";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "DROP TABLE colors";
        cmd.ExecuteNonQuery();
    }
}

[SqlTest(SqlFeatureCategory.Joins, "Test SELF JOIN", DatabaseType.MySql)]
public class SelfJoinTest : SqlTest
{
    public override void Execute(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        // Create table
        cmd.CommandText = "CREATE TABLE employees_self (id INT PRIMARY KEY, name VARCHAR(50), manager_id INT)";
        cmd.ExecuteNonQuery();

        // Insert data
        cmd.CommandText = "INSERT INTO employees_self VALUES (1, 'Boss', NULL), (2, 'John', 1), (3, 'Jane', 1), (4, 'Bob', 2)";
        cmd.ExecuteNonQuery();

        // Execute SELF JOIN
        cmd.CommandText = "SELECT COUNT(*) FROM employees_self e1 JOIN employees_self e2 ON e1.id = e2.manager_id";
        object? count = cmd.ExecuteScalar();
        AssertEqual(3L, (long)count!, "SELF JOIN should find all manager-employee relationships");

        // Cleanup
        cmd.CommandText = "DROP TABLE employees_self";
        cmd.ExecuteNonQuery();
    }
}

[SqlTest(SqlFeatureCategory.Joins, "Test multiple table JOIN", DatabaseType.MySql)]
public class MultipleJoinTest : SqlTest
{
    public override void Execute(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        // Create tables
        cmd.CommandText = "CREATE TABLE customers (id INT PRIMARY KEY, name VARCHAR(50))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "CREATE TABLE orders (id INT PRIMARY KEY, customer_id INT, product_id INT)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "CREATE TABLE products (id INT PRIMARY KEY, name VARCHAR(50))";
        cmd.ExecuteNonQuery();

        // Insert data
        cmd.CommandText = "INSERT INTO customers VALUES (1, 'Alice'), (2, 'Bob')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO products VALUES (1, 'Widget'), (2, 'Gadget')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO orders VALUES (1, 1, 1), (2, 1, 2), (3, 2, 1)";
        cmd.ExecuteNonQuery();

        // Execute multiple JOINs
        cmd.CommandText = "SELECT COUNT(*) FROM customers c JOIN orders o ON c.id = o.customer_id JOIN products p ON o.product_id = p.id";
        object? count = cmd.ExecuteScalar();
        AssertEqual(3L, (long)count!, "Multiple JOINs should work correctly");

        // Cleanup
        cmd.CommandText = "DROP TABLE orders";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "DROP TABLE products";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "DROP TABLE customers";
        cmd.ExecuteNonQuery();
    }
}
