using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.BasicQueries;

[SqlTest(SqlFeatureCategory.BasicQueries, "UPDATE operation")]
public class UpdateTest : SqlTest
{
    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "CREATE TABLE products (id INT PRIMARY KEY, name VARCHAR(50), price DECIMAL(10,2))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO products VALUES (1, 'Laptop', 999.99), (2, 'Mouse', 25.50)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "UPDATE products SET price = 29.99 WHERE id = 2";
        int affectedRows = cmd.ExecuteNonQuery();
        AssertEqual(1, affectedRows, "Should update 1 row");

        cmd.CommandText = "SELECT price FROM products WHERE id = 2";
        object? price = cmd.ExecuteScalar();
        AssertEqual(29.99m, Convert.ToDecimal(price), "Price should be updated");

        cmd.CommandText = "DROP TABLE products";
        cmd.ExecuteNonQuery();
    }
}
