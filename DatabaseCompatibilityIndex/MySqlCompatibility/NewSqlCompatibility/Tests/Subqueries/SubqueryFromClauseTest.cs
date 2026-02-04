using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Subqueries;

[SqlTest(SqlFeatureCategory.Subqueries, "Test subquery in FROM clause")]
public class SubqueryFromClauseTest : SqlTest
{
    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "CREATE TABLE sales_data (id INT PRIMARY KEY, month INT, amount DECIMAL(10,2))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO sales_data VALUES (1, 1, 1000), (2, 1, 1500), (3, 2, 2000), (4, 2, 2500)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM (SELECT month, SUM(amount) as total FROM sales_data GROUP BY month) AS monthly_totals";
        object? count = cmd.ExecuteScalar();
        AssertEqual(2L, Convert.ToInt64(count!), "Subquery in FROM clause should work");
    }

    protected override string? CleanupCommandMy => "DROP TABLE sales_data";
}
