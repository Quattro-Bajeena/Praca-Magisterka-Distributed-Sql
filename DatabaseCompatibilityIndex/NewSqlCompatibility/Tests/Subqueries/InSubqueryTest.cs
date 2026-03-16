using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Subqueries;

[SqlTest(SqlFeatureCategory.Subqueries, "Test IN subquery")]
public class InSubqueryTest : SqlTest
{
    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
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
        AssertEqual(3L, Convert.ToInt64(count!), "IN subquery should return 3 products");

        cmd.CommandText = "DROP TABLE products_sub";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "DROP TABLE categories";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
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
        AssertEqual(3L, Convert.ToInt64(count!), "IN subquery should return 3 products");

        cmd.CommandText = "DROP TABLE products_sub";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "DROP TABLE categories";
        cmd.ExecuteNonQuery();
    }
}
