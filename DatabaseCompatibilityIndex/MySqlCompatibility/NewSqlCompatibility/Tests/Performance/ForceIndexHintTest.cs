using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Performance;

[SqlTest(SqlFeatureCategory.PerformanceHints, "Test FORCE INDEX hint", DatabaseType.MySql)]
public class ForceIndexHintTest : SqlTest
{
    protected override void SetupMy(DbConnection connection)
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

    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "SELECT * FROM products FORCE INDEX (idx_category) WHERE category = 'Electronics'";
        int count = 0;
        using (DbDataReader reader = cmd.ExecuteReader())
        {
            while (reader.Read())
            {
                count++;
            }
        }
        AssertEqual(2, count, "Should find 2 Electronics products with FORCE INDEX");

        cmd.CommandText = "SELECT COUNT(*) FROM products FORCE INDEX (idx_price) WHERE price > 100";
        object? result = cmd.ExecuteScalar();
        AssertEqual(2L, Convert.ToInt64(result!), "Should find 2 products with price > 100");
    }

    protected override void CleanupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE products";
        cmd.ExecuteNonQuery();
    }
}
