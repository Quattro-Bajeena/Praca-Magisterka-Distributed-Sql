using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Indexes;

[SqlTest(SqlFeatureCategory.Indexes, "Test PostgreSQL INCLUDE columns in indexes", DatabaseType.PostgreSql)]
public class PostgresIncludeIndexTest : SqlTest
{
    protected override void SetupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = @"CREATE TABLE products_include (
                            id SERIAL PRIMARY KEY,
                            sku VARCHAR(50),
                            name VARCHAR(200),
                            description TEXT,
                            price DECIMAL(10,2)
                        )";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "CREATE INDEX idx_sku_include ON products_include (sku) INCLUDE (name, price)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO products_include (sku, name, description, price) VALUES ('PROD001', 'Laptop', 'High-end laptop', 999.99)";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "INSERT INTO products_include (sku, name, description, price) VALUES ('PROD002', 'Mouse', 'Wireless mouse', 29.99)";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "INSERT INTO products_include (sku, name, description, price) VALUES ('PROD003', 'Keyboard', 'Mechanical keyboard', 79.99)";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "SELECT name, price FROM products_include WHERE sku = 'PROD001'";
        using (DbDataReader reader = cmd.ExecuteReader())
        {
            AssertTrue(reader.Read(), "Should find product");
            string name = reader.GetString(0);
            decimal price = reader.GetDecimal(1);
            AssertEqual("Laptop", name, "Should be Laptop");
            AssertEqual(999.99m, price, "Price should be 999.99");
        }

        cmd.CommandText = "EXPLAIN (FORMAT TEXT) SELECT name, price FROM products_include WHERE sku = 'PROD002'";
        bool indexOnlyScan = false;
        using (DbDataReader reader = cmd.ExecuteReader())
        {
            while (reader.Read())
            {
                string? plan = reader.GetValue(0)?.ToString();
                if (plan != null && (plan.Contains("Index Only Scan") || plan.Contains("idx_sku_include")))
                {
                    indexOnlyScan = true;
                    break;
                }
            }
        }
        AssertTrue(indexOnlyScan, "Query should use index-only scan with INCLUDE columns");

        cmd.CommandText = "SELECT COUNT(*) FROM products_include WHERE sku IN ('PROD001', 'PROD002')";
        object? count = cmd.ExecuteScalar();
        AssertEqual(2L, Convert.ToInt64(count!), "Should find 2 products");

        cmd.CommandText = "UPDATE products_include SET price = 899.99 WHERE sku = 'PROD001'";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT price FROM products_include WHERE sku = 'PROD001'";
        object? newPrice = cmd.ExecuteScalar();
        AssertEqual(899.99m, Convert.ToDecimal(newPrice!), "Price should be updated");
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS products_include CASCADE";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());
    }
}
