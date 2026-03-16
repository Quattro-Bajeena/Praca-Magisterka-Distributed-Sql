using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.WindowFunctions;

[SqlTest(SqlFeatureCategory.WindowFunctions, "Test PostgreSQL FILTER clause with window functions", DatabaseType.PostgreSql)]
public class PostgresWindowFilterTest : SqlTest
{
    protected override void SetupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        
        cmd.CommandText = @"CREATE TABLE orders_filter (
                            id SERIAL PRIMARY KEY,
                            customer_id INT,
                            amount DECIMAL(10,2),
                            status VARCHAR(20)
                        )";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO orders_filter (customer_id, amount, status) VALUES (1, 100, 'completed')";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "INSERT INTO orders_filter (customer_id, amount, status) VALUES (1, 200, 'pending')";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "INSERT INTO orders_filter (customer_id, amount, status) VALUES (1, 150, 'completed')";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "INSERT INTO orders_filter (customer_id, amount, status) VALUES (2, 300, 'completed')";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "INSERT INTO orders_filter (customer_id, amount, status) VALUES (2, 100, 'cancelled')";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "INSERT INTO orders_filter (customer_id, amount, status) VALUES (2, 250, 'completed')";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = @"SELECT customer_id,
                           SUM(amount) FILTER (WHERE status = 'completed') OVER (PARTITION BY customer_id) as completed_total,
                           SUM(amount) OVER (PARTITION BY customer_id) as overall_total
                           FROM orders_filter
                           ORDER BY customer_id, id";
        
        bool foundCustomer1 = false;
        bool foundCustomer2 = false;
        
        using (DbDataReader reader = cmd.ExecuteReader())
        {
            while (reader.Read())
            {
                int customerId = reader.GetInt32(0);
                decimal completedTotal = reader.GetDecimal(1);
                decimal overallTotal = reader.GetDecimal(2);
                
                if (customerId == 1 && !foundCustomer1)
                {
                    AssertEqual(250m, completedTotal, "Customer 1: completed orders total = 100 + 150");
                    AssertEqual(450m, overallTotal, "Customer 1: all orders total = 100 + 200 + 150");
                    foundCustomer1 = true;
                }
                else if (customerId == 2 && !foundCustomer2)
                {
                    AssertEqual(550m, completedTotal, "Customer 2: completed orders total = 300 + 250");
                    AssertEqual(650m, overallTotal, "Customer 2: all orders total = 300 + 100 + 250");
                    foundCustomer2 = true;
                }
            }
        }
        
        AssertTrue(foundCustomer1 && foundCustomer2, "Should find both customers");

        cmd.CommandText = @"SELECT customer_id,
                           COUNT(*) FILTER (WHERE status = 'completed') OVER (PARTITION BY customer_id) as completed_count,
                           COUNT(*) OVER (PARTITION BY customer_id) as total_count
                           FROM orders_filter
                           WHERE customer_id = 1
                           LIMIT 1";
        
        using (DbDataReader reader = cmd.ExecuteReader())
        {
            AssertTrue(reader.Read(), "Should have customer 1");
            long completedCount = reader.GetInt64(1);
            long totalCount = reader.GetInt64(2);
            AssertEqual(2L, completedCount, "Customer 1 has 2 completed orders");
            AssertEqual(3L, totalCount, "Customer 1 has 3 total orders");
        }

        cmd.CommandText = @"SELECT 
                           AVG(amount) FILTER (WHERE status = 'completed') as avg_completed,
                           AVG(amount) as avg_all
                           FROM orders_filter";
        
        using (DbDataReader reader = cmd.ExecuteReader())
        {
            AssertTrue(reader.Read(), "Should have averages");
            decimal avgCompleted = reader.GetDecimal(0);
            decimal avgAll = reader.GetDecimal(1);
            AssertTrue(avgCompleted > avgAll, "Average of completed orders should be higher");
        }
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS orders_filter CASCADE";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());
    }
}
