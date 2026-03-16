using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Performance;

// https://www.postgresql.org/docs/current/explicit-joins.html
[SqlTest(SqlFeatureCategory.PerformanceHints, "Test PostgreSQL join order and planning settings", DatabaseType.PostgreSql)]
public class PostgresJoinOrderPlanningTest : SqlTest
{
    protected override void SetupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = @"CREATE TABLE customers_plan (
                            id INT PRIMARY KEY,
                            name VARCHAR(100),
                            country VARCHAR(50)
                        )";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"CREATE TABLE orders_plan (
                            id INT PRIMARY KEY,
                            customer_id INT,
                            amount DECIMAL(10,2)
                        )";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"CREATE TABLE products_plan (
                            id INT PRIMARY KEY,
                            order_id INT,
                            product_name VARCHAR(100)
                        )";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "CREATE INDEX idx_customer_country ON customers_plan(country)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "CREATE INDEX idx_order_customer ON orders_plan(customer_id)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "CREATE INDEX idx_product_order ON products_plan(order_id)";
        cmd.ExecuteNonQuery();

        for (int i = 1; i <= 20; i++)
        {
            cmd.CommandText = $"INSERT INTO customers_plan (id, name, country) VALUES ({i}, 'Customer{i}', 'Country{i % 3}')";
            cmd.ExecuteNonQuery();

            cmd.CommandText = $"INSERT INTO orders_plan (id, customer_id, amount) VALUES ({i}, {(i % 20) + 1}, {i * 100.50})";
            cmd.ExecuteNonQuery();

            cmd.CommandText = $"INSERT INTO products_plan (id, order_id, product_name) VALUES ({i}, {(i % 20) + 1}, 'Product{i}')";
            cmd.ExecuteNonQuery();
        }
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "SHOW join_collapse_limit";
        object? limit = cmd.ExecuteScalar();
        AssertTrue(limit != null, "Should be able to read join_collapse_limit");

        cmd.CommandText = "SET LOCAL join_collapse_limit = 8";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SHOW from_collapse_limit";
        limit = cmd.ExecuteScalar();
        AssertTrue(limit != null, "Should be able to read from_collapse_limit");

        cmd.CommandText = "SET LOCAL from_collapse_limit = 8";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"SELECT c.name, o.amount, p.product_name
                           FROM customers_plan c
                           JOIN orders_plan o ON c.id = o.customer_id
                           JOIN products_plan p ON o.id = p.order_id
                           WHERE c.country = 'Country1'";
        using (DbDataReader reader = cmd.ExecuteReader())
        {
            int count = 0;
            while (reader.Read())
            {
                count++;
            }
            AssertTrue(count > 0, "Multi-table join should return results");
        }

        cmd.CommandText = "SET LOCAL enable_hashjoin = ON";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SET LOCAL enable_mergejoin = ON";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SET LOCAL enable_nestloop = ON";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"SELECT COUNT(*) 
                           FROM customers_plan c
                           JOIN orders_plan o ON c.id = o.customer_id";
        object? count2 = cmd.ExecuteScalar();
        AssertTrue(count2 != null, "Join with all join types enabled should work");

        cmd.CommandText = "SET LOCAL enable_hashjoin = OFF";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"SELECT COUNT(*) 
                           FROM customers_plan c
                           JOIN orders_plan o ON c.id = o.customer_id";
        count2 = cmd.ExecuteScalar();
        AssertTrue(count2 != null, "Join should work even with hashjoin disabled");

        cmd.CommandText = "SHOW geqo";
        object? geqo = cmd.ExecuteScalar();
        AssertTrue(geqo != null, "Should be able to read geqo (genetic query optimizer) setting");
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS products_plan CASCADE";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "DROP TABLE IF EXISTS orders_plan CASCADE";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "DROP TABLE IF EXISTS customers_plan CASCADE";
        cmd.ExecuteNonQuery();
    }
}
