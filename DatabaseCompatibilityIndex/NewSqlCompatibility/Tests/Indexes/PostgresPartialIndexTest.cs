using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Indexes;

[SqlTest(SqlFeatureCategory.Indexes, "Test PostgreSQL partial indexes", DatabaseType.PostgreSql)]
public class PostgresPartialIndexTest : SqlTest
{
    protected override void SetupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        
        cmd.CommandText = @"CREATE TABLE orders_partial (
                            id SERIAL PRIMARY KEY,
                            customer_id INT,
                            status VARCHAR(20),
                            amount DECIMAL(10,2),
                            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                        )";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "CREATE INDEX idx_pending_orders ON orders_partial (customer_id) WHERE status = 'pending'";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO orders_partial (customer_id, status, amount) VALUES (1, 'pending', 100)";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "INSERT INTO orders_partial (customer_id, status, amount) VALUES (1, 'completed', 200)";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "INSERT INTO orders_partial (customer_id, status, amount) VALUES (2, 'pending', 150)";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "INSERT INTO orders_partial (customer_id, status, amount) VALUES (2, 'cancelled', 300)";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "SELECT COUNT(*) FROM orders_partial WHERE customer_id = 1 AND status = 'pending'";
        object? count = cmd.ExecuteScalar();
        AssertEqual(1L, Convert.ToInt64(count!), "Should find 1 pending order for customer 1");

        cmd.CommandText = @"EXPLAIN SELECT * FROM orders_partial 
                           WHERE customer_id = 1 AND status = 'pending'";
        bool usesPartialIndex = false;
        using (DbDataReader reader = cmd.ExecuteReader())
        {
            while (reader.Read())
            {
                string? plan = reader.GetValue(0)?.ToString();
                if (plan != null && plan.Contains("idx_pending_orders"))
                {
                    usesPartialIndex = true;
                    break;
                }
            }
        }
        AssertTrue(usesPartialIndex, "Query should use partial index");

        cmd.CommandText = "SELECT COUNT(*) FROM orders_partial WHERE status = 'pending'";
        count = cmd.ExecuteScalar();
        AssertEqual(2L, Convert.ToInt64(count!), "Should have 2 pending orders total");

        cmd.CommandText = "INSERT INTO orders_partial (customer_id, status, amount) VALUES (1, 'pending', 175)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM orders_partial WHERE customer_id = 1 AND status = 'pending'";
        count = cmd.ExecuteScalar();
        AssertEqual(2L, Convert.ToInt64(count!), "Customer 1 should now have 2 pending orders");
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS orders_partial CASCADE";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());
    }
}
