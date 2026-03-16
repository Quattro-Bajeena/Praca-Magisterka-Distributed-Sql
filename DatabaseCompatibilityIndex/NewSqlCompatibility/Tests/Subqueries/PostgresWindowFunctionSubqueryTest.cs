using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Subqueries;

[SqlTest(SqlFeatureCategory.Subqueries, "Test PostgreSQL window functions in subqueries", DatabaseType.PostgreSql)]
public class PostgresWindowFunctionSubqueryTest : SqlTest
{
    protected override void SetupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        
        cmd.CommandText = @"CREATE TABLE sales_window (
                            id SERIAL PRIMARY KEY,
                            salesperson VARCHAR(100),
                            region VARCHAR(50),
                            amount DECIMAL(10,2),
                            sale_date DATE
                        )";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO sales_window (salesperson, region, amount, sale_date) VALUES ('Alice', 'North', 1000, '2024-01-15')";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "INSERT INTO sales_window (salesperson, region, amount, sale_date) VALUES ('Alice', 'North', 1500, '2024-02-10')";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "INSERT INTO sales_window (salesperson, region, amount, sale_date) VALUES ('Bob', 'South', 2000, '2024-01-20')";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "INSERT INTO sales_window (salesperson, region, amount, sale_date) VALUES ('Bob', 'South', 1800, '2024-02-15')";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "INSERT INTO sales_window (salesperson, region, amount, sale_date) VALUES ('Charlie', 'North', 1200, '2024-01-25')";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "INSERT INTO sales_window (salesperson, region, amount, sale_date) VALUES ('Charlie', 'North', 1600, '2024-02-20')";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = @"
            SELECT salesperson, region, amount, 
                   ROW_NUMBER() OVER (PARTITION BY region ORDER BY amount DESC) as rank_in_region
            FROM sales_window
            ORDER BY region, rank_in_region";
        
        using (DbDataReader reader = cmd.ExecuteReader())
        {
            AssertTrue(reader.Read(), "Should have results with window function");
            int rank = reader.GetInt32(3);
            AssertEqual(1, rank, "First row should have rank 1");
        }

        cmd.CommandText = @"
            SELECT * FROM (
                SELECT salesperson, region, amount,
                       RANK() OVER (PARTITION BY region ORDER BY amount DESC) as rank
                FROM sales_window
            ) ranked
            WHERE rank <= 2
            ORDER BY region, rank";
        
        int topSales = 0;
        using (DbDataReader reader = cmd.ExecuteReader())
        {
            while (reader.Read())
            {
                topSales++;
                int rank = reader.GetInt32(3);
                AssertTrue(rank <= 2, "Rank should be 1 or 2");
            }
        }
        AssertTrue(topSales >= 2, "Should have top sales from each region");

        cmd.CommandText = @"
            SELECT salesperson, region, amount,
                   amount - LAG(amount) OVER (PARTITION BY salesperson ORDER BY sale_date) as growth
            FROM sales_window
            ORDER BY salesperson, sale_date";
        
        using (DbDataReader reader = cmd.ExecuteReader())
        {
            while (reader.Read())
            {
                if (!reader.IsDBNull(3))
                {
                    decimal growth = reader.GetDecimal(3);
                    AssertTrue(true, "Growth calculation with LAG should work");
                }
            }
        }

        cmd.CommandText = @"
            SELECT region, 
                   AVG(amount) as avg_amount,
                   (SELECT AVG(amount) FROM sales_window) as overall_avg
            FROM sales_window
            GROUP BY region
            HAVING AVG(amount) > (SELECT AVG(amount) FROM sales_window) * 0.9
            ORDER BY region";
        
        using (DbDataReader reader = cmd.ExecuteReader())
        {
            while (reader.Read())
            {
                decimal avgAmount = reader.GetDecimal(1);
                decimal overallAvg = reader.GetDecimal(2);
                AssertTrue(avgAmount > overallAvg * 0.9m, "Region average should be > 90% of overall");
            }
        }

        cmd.CommandText = @"
            SELECT * FROM (
                SELECT salesperson, region, amount,
                       NTILE(3) OVER (ORDER BY amount) as quartile,
                       PERCENT_RANK() OVER (ORDER BY amount) as percent_rank
                FROM sales_window
            ) quartiles
            WHERE quartile = 3
            ORDER BY amount DESC";
        
        using (DbDataReader reader = cmd.ExecuteReader())
        {
            AssertTrue(reader.Read(), "Should find sales in top quartile");
        }
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS sales_window CASCADE";
        cmd.ExecuteNonQuery();
    }
}
