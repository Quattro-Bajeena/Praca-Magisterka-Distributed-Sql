using NSCI.Testing;using NSCI.Configuration;
using System.Data.Common;

namespace NSCI.Tests.Performance;

[SqlTest(SqlFeatureCategory.Indexes, "Test index hints in JOIN queries", DatabaseType.MySql)]
public class IndexHintsInJoinTest : SqlTest
{
    public override void Setup(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = @"CREATE TABLE customers (
                            id INT PRIMARY KEY,
                            name VARCHAR(100),
                            country VARCHAR(50),
                            INDEX idx_country (country)
                        )";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"CREATE TABLE transactions (
                            id INT PRIMARY KEY,
                            customer_id INT,
                            amount DECIMAL(10, 2),
                            INDEX idx_customer (customer_id)
                        )";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO customers VALUES (1, 'Alice', 'USA'), (2, 'Bob', 'UK'), (3, 'Charlie', 'USA')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO transactions VALUES (1, 1, 100.00), (2, 2, 250.00), (3, 1, 150.00), (4, 3, 300.00)";
        cmd.ExecuteNonQuery();
    }

    public override void Execute(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = @"SELECT c.name, t.amount 
                           FROM customers c USE INDEX (idx_country) 
                           JOIN transactions t USE INDEX (idx_customer) 
                           ON c.id = t.customer_id 
                           WHERE c.country = 'USA'";
        using DbDataReader reader = cmd.ExecuteReader();
        int count = 0;
        while (reader.Read())
        {
            count++;
        }
        AssertEqual(3, count, "Should find 3 transactions for USA customers with index hints");

        cmd.CommandText = @"SELECT SUM(t.amount) 
                           FROM customers c FORCE INDEX (idx_country) 
                           JOIN transactions t FORCE INDEX (idx_customer) 
                           ON c.id = t.customer_id 
                           WHERE c.country = 'USA'";
        object? total = cmd.ExecuteScalar();
        AssertTrue(total != null && (decimal)total! > 0, "Should calculate total correctly with forced indexes");
    }

    public override void Cleanup(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE transactions";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "DROP TABLE customers";
        cmd.ExecuteNonQuery();
    }
}
