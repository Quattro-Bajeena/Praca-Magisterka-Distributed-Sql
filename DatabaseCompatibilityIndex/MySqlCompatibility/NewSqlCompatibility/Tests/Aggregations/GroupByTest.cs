using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Aggregations;

[SqlTest(SqlFeatureCategory.Aggregations, "Test GROUP BY clause", DatabaseType.MySql)]
public class GroupByTest : SqlTest
{
    public override void Execute(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "CREATE TABLE transactions (id INT PRIMARY KEY, category VARCHAR(20), amount DECIMAL(10,2))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO transactions VALUES (1, 'Food', 50.0), (2, 'Food', 30.0), (3, 'Gas', 40.0), (4, 'Gas', 45.0), (5, 'Entertainment', 60.0)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM (SELECT category, SUM(amount) FROM transactions GROUP BY category) AS grouped";
        object? groupCount = cmd.ExecuteScalar();
        AssertEqual(1L, (long)groupCount!, "GROUP BY should work with subquery");

        cmd.CommandText = "DROP TABLE transactions";
        cmd.ExecuteNonQuery();
    }
}
