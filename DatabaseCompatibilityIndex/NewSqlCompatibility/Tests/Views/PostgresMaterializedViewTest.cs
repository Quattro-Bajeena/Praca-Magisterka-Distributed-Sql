using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Views;

[SqlTest(SqlFeatureCategory.Views, "Test PostgreSQL materialized views", DatabaseType.PostgreSql)]
public class PostgresMaterializedViewTest : SqlTest
{
    protected override void SetupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = @"CREATE TABLE sales_data (
                            id SERIAL PRIMARY KEY,
                            product VARCHAR(100),
                            amount DECIMAL(10,2)
                        )";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO sales_data (product, amount) VALUES ('Laptop', 1000)";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "INSERT INTO sales_data (product, amount) VALUES ('Laptop', 1200)";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "INSERT INTO sales_data (product, amount) VALUES ('Mouse', 25)";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = @"CREATE MATERIALIZED VIEW sales_summary AS
                           SELECT product, COUNT(*) as sales_count, SUM(amount) as total_amount
                           FROM sales_data
                           GROUP BY product";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT sales_count FROM sales_summary WHERE product = 'Laptop'";
        object? count = cmd.ExecuteScalar();
        AssertEqual(2L, Convert.ToInt64(count!), "Materialized view should show 2 laptop sales");

        // Add a row and verify view is stale (not auto-refreshed)
        cmd.CommandText = "INSERT INTO sales_data (product, amount) VALUES ('Laptop', 1500)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT sales_count FROM sales_summary WHERE product = 'Laptop'";
        count = cmd.ExecuteScalar();
        AssertEqual(2L, Convert.ToInt64(count!), "Materialized view should still show 2 (not refreshed yet)");

        // After refresh the count should update
        cmd.CommandText = "REFRESH MATERIALIZED VIEW sales_summary";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT sales_count FROM sales_summary WHERE product = 'Laptop'";
        count = cmd.ExecuteScalar();
        AssertEqual(3L, Convert.ToInt64(count!), "After refresh, materialized view should show 3 laptop sales");
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP MATERIALIZED VIEW IF EXISTS sales_summary CASCADE";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());
        cmd.CommandText = "DROP TABLE IF EXISTS sales_data CASCADE";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());
    }
}
