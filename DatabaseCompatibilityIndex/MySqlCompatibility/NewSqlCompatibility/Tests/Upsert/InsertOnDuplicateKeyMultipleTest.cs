using NSCI.Testing;using NSCI.Configuration;
using System.Data.Common;

namespace NSCI.Tests.Upsert;

[SqlTest(SqlFeatureCategory.Upsert, "Test INSERT...ON DUPLICATE KEY with multiple rows", DatabaseType.MySql)]
public class InsertOnDuplicateKeyMultipleTest : SqlTest
{
    public override void Setup(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "CREATE TABLE products (product_id INT PRIMARY KEY, name VARCHAR(100), quantity INT)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO products VALUES (1, 'Laptop', 5), (2, 'Mouse', 20)";
        cmd.ExecuteNonQuery();
    }

    public override void Execute(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        // Upsert multiple rows
        cmd.CommandText = @"INSERT INTO products VALUES 
                            (1, 'Laptop Pro', 3), 
                            (2, 'Wireless Mouse', 15),
                            (3, 'Keyboard', 10)
                           ON DUPLICATE KEY UPDATE 
                            name = VALUES(name), 
                            quantity = VALUES(quantity)";
        cmd.ExecuteNonQuery();

        // Verify all rows
        cmd.CommandText = "SELECT COUNT(*) FROM products";
        object? count = cmd.ExecuteScalar();
        AssertEqual(3L, (long)count!, "Should have 3 products");

        cmd.CommandText = "SELECT quantity FROM products WHERE product_id = 1";
        object? qty = cmd.ExecuteScalar();
        AssertEqual(3L, (long)qty!, "Laptop quantity should be updated to 3");
    }

    public override void Cleanup(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE products";
        cmd.ExecuteNonQuery();
    }
}
