using NSCI.Testing;using NSCI.Configuration;
using System.Data.Common;

namespace NSCI.Tests.Performance;

[SqlTest(SqlFeatureCategory.Indexes, "Test IGNORE INDEX hint", DatabaseType.MySql)]
public class IgnoreIndexHintTest : SqlTest
{
    public override void Setup(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = @"CREATE TABLE orders (
                            id INT PRIMARY KEY,
                            customer_id INT,
                            amount DECIMAL(10, 2),
                            order_date DATE,
                            INDEX idx_customer (customer_id),
                            INDEX idx_date (order_date)
                        )";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"INSERT INTO orders VALUES 
                            (1, 100, 250.00, '2024-01-15'),
                            (2, 101, 500.00, '2024-01-20'),
                            (3, 100, 175.00, '2024-02-10'),
                            (4, 102, 350.00, '2024-02-15')";
        cmd.ExecuteNonQuery();
    }

    public override void Execute(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "SELECT COUNT(*) FROM orders IGNORE INDEX (idx_date) WHERE order_date = '2024-01-15'";
        object? count = cmd.ExecuteScalar();
        AssertEqual(1L, (long)count!, "IGNORE INDEX should still return correct results");

        cmd.CommandText = "SELECT * FROM orders IGNORE INDEX (idx_customer, idx_date) WHERE customer_id = 100 AND amount > 100";
        using DbDataReader reader = cmd.ExecuteReader();
        int rowCount = 0;
        while (reader.Read())
        {
            rowCount++;
        }
        AssertEqual(2, rowCount, "Should find 2 matching orders ignoring indexes");
    }

    public override void Cleanup(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE orders";
        cmd.ExecuteNonQuery();
    }
}
