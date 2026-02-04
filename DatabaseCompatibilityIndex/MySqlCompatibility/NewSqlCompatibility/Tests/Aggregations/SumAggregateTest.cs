using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Aggregations;

[SqlTest(SqlFeatureCategory.Aggregations, "Test SUM aggregate function")]
public class SumAggregateTest : SqlTest
{
    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "CREATE TABLE sales (id INT PRIMARY KEY, amount DECIMAL(10,2))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO sales VALUES (1, 100.50), (2, 200.75), (3, 50.25)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT SUM(amount) FROM sales";
        object? sum = cmd.ExecuteScalar();
        decimal sumValue = Convert.ToDecimal(sum);
        AssertTrue(Math.Abs(sumValue - 351.50m) < 0.01m, "SUM should be 351.50");

        cmd.CommandText = "DROP TABLE sales";
        cmd.ExecuteNonQuery();
    }
}
