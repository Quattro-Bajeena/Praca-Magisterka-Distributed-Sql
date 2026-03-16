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
                            amount DECIMAL(10,2),
                            sale_date DATE
                        )";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO sales_data (product, amount, sale_date) VALUES ('Laptop', 1000, '2024-01-15')";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "INSERT INTO sales_data (product, amount, sale_date) VALUES ('Mouse', 25, '2024-01-16')";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "INSERT INTO sales_data (product, amount, sale_date) VALUES ('Keyboard', 75, '2024-01-17')";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "INSERT INTO sales_data (product, amount, sale_date) VALUES ('Laptop', 1200, '2024-01-18')";
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
        object? laptopCount = cmd.ExecuteScalar();
        AssertEqual(2L, Convert.ToInt64(laptopCount!), "Materialized view should show 2 laptop sales");

        cmd.CommandText = "SELECT total_amount FROM sales_summary WHERE product = 'Laptop'";
        object? laptopTotal = cmd.ExecuteScalar();
        AssertEqual(2200m, Convert.ToDecimal(laptopTotal!), "Materialized view should show total of 2200");

        cmd.CommandText = "INSERT INTO sales_data (product, amount, sale_date) VALUES ('Laptop', 1500, '2024-01-19')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT sales_count FROM sales_summary WHERE product = 'Laptop'";
        laptopCount = cmd.ExecuteScalar();
        AssertEqual(2L, Convert.ToInt64(laptopCount!), "Materialized view should still show 2 (not refreshed)");

        cmd.CommandText = "REFRESH MATERIALIZED VIEW sales_summary";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT sales_count FROM sales_summary WHERE product = 'Laptop'";
        laptopCount = cmd.ExecuteScalar();
        AssertEqual(3L, Convert.ToInt64(laptopCount!), "After refresh, materialized view should show 3 laptop sales");

        cmd.CommandText = "SELECT total_amount FROM sales_summary WHERE product = 'Laptop'";
        laptopTotal = cmd.ExecuteScalar();
        AssertEqual(3700m, Convert.ToDecimal(laptopTotal!), "After refresh, total should be 3700");

        cmd.CommandText = "SELECT COUNT(*) FROM sales_summary";
        object? productCount = cmd.ExecuteScalar();
        AssertEqual(3L, Convert.ToInt64(productCount!), "Should have 3 products in materialized view");
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
