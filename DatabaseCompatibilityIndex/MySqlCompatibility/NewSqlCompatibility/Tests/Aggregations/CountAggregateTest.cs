using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Aggregations;

[SqlTest(SqlFeatureCategory.Aggregations, "Test COUNT aggregate function")]
public class CountAggregateTest : SqlTest
{
    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "CREATE TABLE items (id INT PRIMARY KEY, category VARCHAR(20), price DECIMAL(10,2))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO items VALUES (1, 'A', 10.0), (2, 'A', 20.0), (3, 'B', 15.0), (4, 'B', 25.0)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM items";
        object? total = cmd.ExecuteScalar();
        AssertEqual(4L, (long)total!, "COUNT should return 4");

        cmd.CommandText = "SELECT COUNT(*) FROM items WHERE category = 'A'";
        object? countA = cmd.ExecuteScalar();
        AssertEqual(2L, (long)countA!, "COUNT with WHERE should return 2");

        cmd.CommandText = "DROP TABLE items";
        cmd.ExecuteNonQuery();
    }
}
