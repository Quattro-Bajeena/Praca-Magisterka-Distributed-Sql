using NSCI.Testing;using NSCI.Configuration;
using System.Data.Common;

namespace NSCI.Tests.Subqueries;

[SqlTest(SqlFeatureCategory.Subqueries, "Test scalar subquery", DatabaseType.MySql)]
public class ScalarSubqueryTest : SqlTest
{
    public override void Execute(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "CREATE TABLE employees_sub (id INT PRIMARY KEY, name VARCHAR(50), salary DECIMAL(10,2))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO employees_sub VALUES (1, 'Alice', 50000), (2, 'Bob', 60000), (3, 'Charlie', 55000)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT salary FROM employees_sub WHERE salary > (SELECT AVG(salary) FROM employees_sub)";
        using DbDataReader reader = cmd.ExecuteReader();
        int count = 0;
        while (reader.Read())
            count++;
        AssertEqual(1, count, "Scalar subquery should return 1 row");

        cmd.CommandText = "DROP TABLE employees_sub";
        cmd.ExecuteNonQuery();
    }
}

[SqlTest(SqlFeatureCategory.Subqueries, "Test IN subquery", DatabaseType.MySql)]
public class InSubqueryTest : SqlTest
{
    public override void Execute(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "CREATE TABLE categories (id INT PRIMARY KEY, name VARCHAR(50))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "CREATE TABLE products_sub (id INT PRIMARY KEY, category_id INT, name VARCHAR(50))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO categories VALUES (1, 'Electronics'), (2, 'Books')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO products_sub VALUES (1, 1, 'Laptop'), (2, 1, 'Phone'), (3, 2, 'Novel'), (4, 3, 'Unknown')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM products_sub WHERE category_id IN (SELECT id FROM categories)";
        object? count = cmd.ExecuteScalar();
        AssertEqual(3L, (long)count!, "IN subquery should return 3 products");

        cmd.CommandText = "DROP TABLE products_sub";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "DROP TABLE categories";
        cmd.ExecuteNonQuery();
    }
}

[SqlTest(SqlFeatureCategory.Subqueries, "Test NOT IN subquery", DatabaseType.MySql)]
public class NotInSubqueryTest : SqlTest
{
    public override void Execute(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "CREATE TABLE allowed_ids (id INT PRIMARY KEY)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "CREATE TABLE all_records (id INT PRIMARY KEY, value INT)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO allowed_ids VALUES (1), (2), (3)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO all_records VALUES (1, 100), (2, 200), (3, 300), (4, 400), (5, 500)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM all_records WHERE id NOT IN (SELECT id FROM allowed_ids)";
        object? count = cmd.ExecuteScalar();
        AssertEqual(2L, (long)count!, "NOT IN subquery should return 2 records");

        cmd.CommandText = "DROP TABLE all_records";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "DROP TABLE allowed_ids";
        cmd.ExecuteNonQuery();
    }
}

[SqlTest(SqlFeatureCategory.Subqueries, "Test EXISTS subquery", DatabaseType.MySql)]
public class ExistsSubqueryTest : SqlTest
{
    public override void Execute(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "CREATE TABLE customers_sub (id INT PRIMARY KEY, name VARCHAR(50))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "CREATE TABLE orders_sub (id INT PRIMARY KEY, customer_id INT, total DECIMAL(10,2))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO customers_sub VALUES (1, 'Alice'), (2, 'Bob'), (3, 'Charlie')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO orders_sub VALUES (1, 1, 100.0), (2, 1, 200.0), (3, 3, 150.0)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM customers_sub c WHERE EXISTS (SELECT 1 FROM orders_sub o WHERE o.customer_id = c.id)";
        object? count = cmd.ExecuteScalar();
        AssertEqual(2L, (long)count!, "EXISTS subquery should find 2 customers with orders");

        cmd.CommandText = "DROP TABLE orders_sub";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "DROP TABLE customers_sub";
        cmd.ExecuteNonQuery();
    }
}

[SqlTest(SqlFeatureCategory.Subqueries, "Test NOT EXISTS subquery", DatabaseType.MySql)]
public class NotExistsSubqueryTest : SqlTest
{
    public override void Execute(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "CREATE TABLE parent_records (id INT PRIMARY KEY, name VARCHAR(50))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "CREATE TABLE child_records (id INT PRIMARY KEY, parent_id INT)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO parent_records VALUES (1, 'P1'), (2, 'P2'), (3, 'P3')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO child_records VALUES (1, 1), (2, 2)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM parent_records p WHERE NOT EXISTS (SELECT 1 FROM child_records c WHERE c.parent_id = p.id)";
        object? count = cmd.ExecuteScalar();
        AssertEqual(1L, (long)count!, "NOT EXISTS subquery should find 1 parent without children");

        cmd.CommandText = "DROP TABLE child_records";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "DROP TABLE parent_records";
        cmd.ExecuteNonQuery();
    }
}

[SqlTest(SqlFeatureCategory.Subqueries, "Test correlated subquery", DatabaseType.MySql)]
public class CorrelatedSubqueryTest : SqlTest
{
    public override void Execute(DbConnection connection)
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
        AssertEqual(2L, (long)count!, "Correlated subquery should find 2 above-average salary employees");

        cmd.CommandText = "DROP TABLE employees_cor";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "DROP TABLE departments_cor";
        cmd.ExecuteNonQuery();
    }
}

[SqlTest(SqlFeatureCategory.Subqueries, "Test subquery in FROM clause", DatabaseType.MySql)]
public class SubqueryFromClauseTest : SqlTest
{
    public override void Execute(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "CREATE TABLE sales_data (id INT PRIMARY KEY, month INT, amount DECIMAL(10,2))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO sales_data VALUES (1, 1, 1000), (2, 1, 1500), (3, 2, 2000), (4, 2, 2500)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM (SELECT month, SUM(amount) as total FROM sales_data GROUP BY month) AS monthly_totals";
        object? count = cmd.ExecuteScalar();
        AssertEqual(1L, (long)count!, "Subquery in FROM clause should work");

        cmd.CommandText = "DROP TABLE sales_data";
        cmd.ExecuteNonQuery();
    }
}
