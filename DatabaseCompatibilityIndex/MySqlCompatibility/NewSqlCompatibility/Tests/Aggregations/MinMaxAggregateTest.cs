using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Aggregations;

[SqlTest(SqlFeatureCategory.Aggregations, "Test MIN and MAX aggregate functions")]
public class MinMaxAggregateTest : SqlTest
{
    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "CREATE TABLE values_table (id INT PRIMARY KEY, val INT)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO values_table VALUES (1, 10), (2, 50), (3, 30), (4, 100), (5, 20)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT MIN(val) FROM values_table";
        object? min = cmd.ExecuteScalar();
        AssertEqual(10L, Convert.ToInt64(min!), "MIN should be 10");

        cmd.CommandText = "SELECT MAX(val) FROM values_table";
        object? max = cmd.ExecuteScalar();
        AssertEqual(100L, Convert.ToInt64(max!), "MAX should be 100");

        cmd.CommandText = "DROP TABLE values_table";
        cmd.ExecuteNonQuery();
    }
}
