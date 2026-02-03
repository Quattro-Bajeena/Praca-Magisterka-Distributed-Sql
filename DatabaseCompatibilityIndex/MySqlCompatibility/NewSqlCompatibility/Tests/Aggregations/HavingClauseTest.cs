using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Aggregations;

[SqlTest(SqlFeatureCategory.Aggregations, "Test HAVING clause with GROUP BY", DatabaseType.MySql)]
public class HavingClauseTest : SqlTest
{
    public override void Execute(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "CREATE TABLE orders_having (id INT PRIMARY KEY, customer_id INT, total DECIMAL(10,2))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO orders_having VALUES (1, 1, 100.0), (2, 1, 200.0), (3, 2, 50.0), (4, 3, 300.0)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM (SELECT customer_id FROM orders_having GROUP BY customer_id HAVING SUM(total) > 100) AS filtered";
        object? havingCount = cmd.ExecuteScalar();
        AssertEqual(1L, (long)havingCount!, "HAVING clause should filter grouped results");

        cmd.CommandText = "DROP TABLE orders_having";
        cmd.ExecuteNonQuery();
    }
}
