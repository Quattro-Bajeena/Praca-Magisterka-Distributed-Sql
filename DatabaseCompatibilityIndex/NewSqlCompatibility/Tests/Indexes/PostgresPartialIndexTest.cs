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
                            amount DECIMAL(10,2)
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
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "SELECT COUNT(*) FROM orders_partial WHERE status = 'pending'";
        object? count = cmd.ExecuteScalar();
        AssertEqual(2L, Convert.ToInt64(count!), "Should have 2 pending orders total");

        cmd.CommandText = "SELECT COUNT(*) FROM orders_partial WHERE customer_id = 1 AND status = 'pending'";
        count = cmd.ExecuteScalar();
        AssertEqual(1L, Convert.ToInt64(count!), "Should find 1 pending order for customer 1");
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS orders_partial CASCADE";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());
    }
}
