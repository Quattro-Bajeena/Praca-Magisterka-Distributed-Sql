using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Upsert;

[SqlTest(SqlFeatureCategory.Upsert, "Test INSERT...ON DUPLICATE KEY with multiple rows", Configuration.DatabaseType.MySql)]
public class InsertOnDuplicateKeyMultipleTest : SqlTest
{
    protected override void SetupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "CREATE TABLE products (product_id INT PRIMARY KEY, name VARCHAR(100), quantity INT)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO products VALUES (1, 'Laptop', 5), (2, 'Mouse', 20)";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = @"INSERT INTO products VALUES 
                            (1, 'Laptop Pro', 3), 
                            (2, 'Wireless Mouse', 15),
                            (3, 'Keyboard', 10)
                           ON DUPLICATE KEY UPDATE 
                            name = VALUES(name), 
                            quantity = VALUES(quantity)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM products";
        object? count = cmd.ExecuteScalar();
        AssertEqual(3L, Convert.ToInt64(count!), "Should have 3 products");

        cmd.CommandText = "SELECT quantity FROM products WHERE product_id = 1";
        object? qty = cmd.ExecuteScalar();
        AssertEqual(3L, Convert.ToInt64(qty!), "Laptop quantity should be updated to 3");
    }

    protected override void CleanupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE products";
        cmd.ExecuteNonQuery();
    }
}
