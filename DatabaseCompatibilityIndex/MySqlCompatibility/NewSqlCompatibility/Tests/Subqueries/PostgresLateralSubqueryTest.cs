using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Subqueries;

[SqlTest(SqlFeatureCategory.Subqueries, "Test PostgreSQL LATERAL subqueries", DatabaseType.PostgreSql)]
public class PostgresLateralSubqueryTest : SqlTest
{
    protected override void SetupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        
        cmd.CommandText = @"CREATE TABLE manufacturers (
                            id SERIAL PRIMARY KEY,
                            name VARCHAR(100),
                            country VARCHAR(50)
                        )";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"CREATE TABLE products_lateral (
                            id SERIAL PRIMARY KEY,
                            manufacturer_id INT,
                            name VARCHAR(100),
                            price DECIMAL(10,2)
                        )";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO manufacturers (name, country) VALUES ('TechCorp', 'USA'), ('DataCo', 'Germany'), ('CodeInc', 'Japan')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO products_lateral (manufacturer_id, name, price) VALUES (1, 'Product A', 100), (1, 'Product B', 150), (1, 'Product C', 200)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO products_lateral (manufacturer_id, name, price) VALUES (2, 'Product D', 120), (2, 'Product E', 180)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO products_lateral (manufacturer_id, name, price) VALUES (3, 'Product F', 90), (3, 'Product G', 130), (3, 'Product H', 170)";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = @"
            SELECT m.name, top_products.product_name, top_products.price
            FROM manufacturers m,
            LATERAL (
                SELECT p.name as product_name, p.price
                FROM products_lateral p
                WHERE p.manufacturer_id = m.id
                ORDER BY p.price DESC
                LIMIT 2
            ) AS top_products
            ORDER BY m.name, top_products.price DESC";
        
        int rowCount = 0;
        using (DbDataReader reader = cmd.ExecuteReader())
        {
            while (reader.Read())
            {
                rowCount++;
                string manuf = reader.GetString(0);
                AssertTrue(manuf != null && manuf.Length > 0, "Manufacturer name should not be empty");
            }
        }
        AssertTrue(rowCount >= 6, "Should return top 2 products per manufacturer");

        cmd.CommandText = @"
            SELECT m.name, 
                   (SELECT COUNT(*) FROM products_lateral p WHERE p.manufacturer_id = m.id) as product_count,
                   avg_price.avg_price
            FROM manufacturers m
            LEFT JOIN LATERAL (
                SELECT AVG(price) as avg_price
                FROM products_lateral p
                WHERE p.manufacturer_id = m.id
            ) avg_price ON true
            ORDER BY m.name";
        
        using (DbDataReader reader = cmd.ExecuteReader())
        {
            AssertTrue(reader.Read(), "Should have results from LATERAL JOIN");
            long count = reader.GetInt64(1);
            AssertTrue(count > 0, "Product count should be greater than 0");
        }

        cmd.CommandText = @"
            SELECT m.name, expensive.product_name, expensive.price
            FROM manufacturers m,
            LATERAL (
                SELECT p.name as product_name, p.price
                FROM products_lateral p
                WHERE p.manufacturer_id = m.id AND p.price > 100
                ORDER BY p.price DESC
            ) expensive
            WHERE expensive.price > 150";
        
        using (DbDataReader reader = cmd.ExecuteReader())
        {
            int expensiveCount = 0;
            while (reader.Read())
            {
                expensiveCount++;
                decimal price = reader.GetDecimal(2);
                AssertTrue(price > 150, "All products should have price > 150");
            }
            AssertTrue(expensiveCount > 0, "Should find expensive products");
        }
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS products_lateral CASCADE";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "DROP TABLE IF EXISTS manufacturers CASCADE";
        cmd.ExecuteNonQuery();
    }
}
