using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Joins;

[SqlTest(SqlFeatureCategory.Joins, "Test multiple table JOIN")]
public class MultipleJoinTest : SqlTest
{
    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "CREATE TABLE customers (id INT PRIMARY KEY, name VARCHAR(50))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "CREATE TABLE orders (id INT PRIMARY KEY, customer_id INT, product_id INT)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "CREATE TABLE products (id INT PRIMARY KEY, name VARCHAR(50))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO customers VALUES (1, 'Alice'), (2, 'Bob')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO products VALUES (1, 'Widget'), (2, 'Gadget')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO orders VALUES (1, 1, 1), (2, 1, 2), (3, 2, 1)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM customers c JOIN orders o ON c.id = o.customer_id JOIN products p ON o.product_id = p.id";
        object? count = cmd.ExecuteScalar();
        AssertEqual(3L, (long)count!, "Multiple JOINs should work correctly");

        cmd.CommandText = "DROP TABLE orders";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "DROP TABLE products";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "DROP TABLE customers";
        cmd.ExecuteNonQuery();
    }
}
