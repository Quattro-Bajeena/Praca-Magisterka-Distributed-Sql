using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.WindowFunctions;

[SqlTest(SqlFeatureCategory.WindowFunctions, "Test PostgreSQL NTH_VALUE and value access window functions", DatabaseType.PostgreSql)]
public class PostgresNthValueTest : SqlTest
{
    protected override void SetupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        
        cmd.CommandText = @"CREATE TABLE product_prices (
                            id SERIAL PRIMARY KEY,
                            product VARCHAR(100),
                            month INT,
                            price DECIMAL(10,2)
                        )";
        cmd.ExecuteNonQuery();

        string[] products = { "Laptop", "Mouse", "Keyboard" };
        foreach (string product in products)
        {
            for (int month = 1; month <= 6; month++)
            {
                decimal price = 100 + (month * 10);
                cmd.CommandText = $"INSERT INTO product_prices (product, month, price) VALUES ('{product}', {month}, {price})";
                cmd.ExecuteNonQuery();
            }
        }
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = @"SELECT product, month, price,
                           NTH_VALUE(price, 1) OVER (PARTITION BY product ORDER BY month) as first_price,
                           NTH_VALUE(price, 3) OVER (PARTITION BY product ORDER BY month ROWS BETWEEN UNBOUNDED PRECEDING AND UNBOUNDED FOLLOWING) as third_price,
                           NTH_VALUE(price, 6) OVER (PARTITION BY product ORDER BY month ROWS BETWEEN UNBOUNDED PRECEDING AND UNBOUNDED FOLLOWING) as sixth_price
                           FROM product_prices
                           WHERE product = 'Laptop'
                           ORDER BY month";
        
        using (DbDataReader reader = cmd.ExecuteReader())
        {
            while (reader.Read())
            {
                int month = reader.GetInt32(1);
                decimal firstPrice = reader.GetDecimal(3);
                AssertEqual(110m, firstPrice, "First month price should always be 110");
                
                if (!reader.IsDBNull(4))
                {
                    decimal thirdPrice = reader.GetDecimal(4);
                    AssertEqual(130m, thirdPrice, "Third month price should be 130");
                }
                
                if (!reader.IsDBNull(5))
                {
                    decimal sixthPrice = reader.GetDecimal(5);
                    AssertEqual(160m, sixthPrice, "Sixth month price should be 160");
                }
            }
        }

        cmd.CommandText = @"SELECT product, month, price,
                           FIRST_VALUE(price) OVER (PARTITION BY product ORDER BY month) as first_val,
                           LAST_VALUE(price) OVER (PARTITION BY product ORDER BY month ROWS BETWEEN UNBOUNDED PRECEDING AND UNBOUNDED FOLLOWING) as last_val,
                           price - FIRST_VALUE(price) OVER (PARTITION BY product ORDER BY month) as price_change
                           FROM product_prices
                           WHERE product = 'Mouse' AND month IN (1, 6)
                           ORDER BY month";
        
        using (DbDataReader reader = cmd.ExecuteReader())
        {
            AssertTrue(reader.Read(), "Should have month 1");
            AssertEqual(1, reader.GetInt32(1), "First row is month 1");
            decimal firstVal1 = reader.GetDecimal(3);
            decimal lastVal1 = reader.GetDecimal(4);
            decimal change1 = reader.GetDecimal(5);
            AssertEqual(0m, change1, "Month 1: no change from first month");
            AssertEqual(160m, lastVal1, "Last value should be month 6 price");

            AssertTrue(reader.Read(), "Should have month 6");
            AssertEqual(6, reader.GetInt32(1), "Second row is month 6");
            decimal firstVal6 = reader.GetDecimal(3);
            decimal change6 = reader.GetDecimal(5);
            AssertEqual(110m, firstVal6, "First value should still be month 1 price");
            AssertEqual(50m, change6, "Month 6: price increased by 50");
        }

        cmd.CommandText = @"SELECT product,
                           COUNT(DISTINCT month) as month_count,
                           NTH_VALUE(price, 2) OVER (PARTITION BY product ORDER BY month ROWS BETWEEN UNBOUNDED PRECEDING AND UNBOUNDED FOLLOWING) as second_month_price
                           FROM product_prices
                           GROUP BY product, price, month
                           HAVING COUNT(*) > 0
                           ORDER BY product
                           LIMIT 3";
        
        int productCount = 0;
        using (DbDataReader reader = cmd.ExecuteReader())
        {
            while (reader.Read())
            {
                productCount++;
                if (!reader.IsDBNull(2))
                {
                    decimal secondPrice = reader.GetDecimal(2);
                    AssertEqual(120m, secondPrice, "Second month price should be 120 for all products");
                }
            }
        }
        AssertTrue(productCount >= 3, "Should have at least 3 products");
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS product_prices CASCADE";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());
    }
}
