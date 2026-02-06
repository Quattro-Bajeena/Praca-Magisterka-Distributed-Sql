using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Aggregations;

[SqlTest(SqlFeatureCategory.Aggregations, "Test GROUP BY clause")]
public class GroupByTest : SqlTest
{
    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "CREATE TABLE transactions_group (id INT PRIMARY KEY, category VARCHAR(20), amount DECIMAL(10,2))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO transactions_group VALUES (1, 'Food', 50.0), (2, 'Food', 30.0), (3, 'Gas', 40.0), (4, 'Gas', 45.0), (5, 'Entertainment', 60.0)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM (SELECT category, SUM(amount) FROM transactions_group GROUP BY category) AS grouped";
        object? groupCount = cmd.ExecuteScalar();
        AssertEqual(3L, Convert.ToInt64(groupCount!), "GROUP BY should work with subquery");

        cmd.CommandText = "DROP TABLE transactions_group";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "CREATE TABLE transactions_group (id INT PRIMARY KEY, category VARCHAR(20), amount DECIMAL(10,2))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO transactions_group VALUES (1, 'Food', 50.0), (2, 'Food', 30.0), (3, 'Gas', 40.0), (4, 'Gas', 45.0), (5, 'Entertainment', 60.0)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM (SELECT category, SUM(amount) FROM transactions_group GROUP BY category) AS grouped";
        object? groupCount = cmd.ExecuteScalar();
        AssertEqual(3L, Convert.ToInt64(groupCount!), "GROUP BY should work with subquery");

        cmd.CommandText = "DROP TABLE transactions_group";
        cmd.ExecuteNonQuery();
    }
}
