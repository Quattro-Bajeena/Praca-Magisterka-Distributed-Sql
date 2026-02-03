using NSCI.Testing;using NSCI.Configuration;
using System.Data.Common;

namespace NSCI.Tests.Performance;

[SqlTest(SqlFeatureCategory.Indexes, "Test FORCE INDEX hint", DatabaseType.MySql)]
public class ForceIndexHintTest : SqlTest
{
    public override void Setup(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = @"CREATE TABLE products (
                            id INT PRIMARY KEY,
                            category VARCHAR(50),
                            name VARCHAR(100),
                            price DECIMAL(10, 2),
                            INDEX idx_category (category),
                            INDEX idx_price (price)
                        )";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"INSERT INTO products VALUES 
                            (1, 'Electronics', 'Laptop', 999.99),
                            (2, 'Electronics', 'Phone', 699.99),
                            (3, 'Books', 'Novel', 19.99),
                            (4, 'Books', 'Textbook', 89.99)";
        cmd.ExecuteNonQuery();
    }

    public override void Execute(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "SELECT * FROM products FORCE INDEX (idx_category) WHERE category = 'Electronics'";
        using DbDataReader reader = cmd.ExecuteReader();
        int count = 0;
        while (reader.Read())
        {
            count++;
        }
        AssertEqual(2, count, "Should find 2 Electronics products with FORCE INDEX");

        cmd.CommandText = "SELECT COUNT(*) FROM products FORCE INDEX (idx_price) WHERE price > 100";
        object? result = cmd.ExecuteScalar();
        AssertEqual(2L, (long)result!, "Should find 2 products with price > 100");
    }

    public override void Cleanup(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE products";
        cmd.ExecuteNonQuery();
    }
}
