using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Locking;

[SqlTest(SqlFeatureCategory.Locking, "FOR KEY SHARE and FOR NO KEY UPDATE", DatabaseType.PostgreSql)]
public class PostgresOnlyAdvancedLockingTest : SqlTest
{
    protected override void SetupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "CREATE TABLE products (product_id INT PRIMARY KEY, name VARCHAR(100), price DECIMAL(10,2), category_id INT)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO products VALUES (1, 'Laptop', 999.99, 1), (2, 'Mouse', 29.99, 1)";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd1 = connection.CreateCommand();
        using DbCommand cmd2 = connectionSecond.CreateCommand();

        cmd1.CommandText = "BEGIN";
        cmd1.ExecuteNonQuery();

        cmd1.CommandText = "SELECT price FROM products WHERE product_id = 1 FOR NO KEY UPDATE";
        object? price = cmd1.ExecuteScalar();
        AssertEqual(999.99m, Convert.ToDecimal(price!), "Should read with FOR NO KEY UPDATE");

        cmd2.CommandText = "BEGIN";
        cmd2.ExecuteNonQuery();

        cmd2.CommandText = "SELECT name FROM products WHERE product_id = 1 FOR KEY SHARE";
        object? name = cmd2.ExecuteScalar();
        AssertEqual("Laptop", name?.ToString(), "FOR KEY SHARE should not block on FOR NO KEY UPDATE");

        cmd2.CommandText = "COMMIT";
        cmd2.ExecuteNonQuery();

        cmd1.CommandText = "UPDATE products SET price = 899.99 WHERE product_id = 1";
        cmd1.ExecuteNonQuery();

        cmd1.CommandText = "COMMIT";
        cmd1.ExecuteNonQuery();

        cmd1.CommandText = "SELECT price FROM products WHERE product_id = 1";
        object? newPrice = cmd1.ExecuteScalar();
        AssertEqual(899.99m, Convert.ToDecimal(newPrice!), "Price should be updated");
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS products";
        cmd.ExecuteNonQuery();
    }
}
