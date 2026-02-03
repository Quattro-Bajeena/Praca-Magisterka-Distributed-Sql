using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.WindowFunctions;

[SqlTest(SqlFeatureCategory.WindowFunctions, "Test ROW_NUMBER window function", DatabaseType.MySql)]
public class RowNumberTest : SqlTest
{
    public override void Setup(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "CREATE TABLE sales_window (id INT PRIMARY KEY, region VARCHAR(20), amount DECIMAL(10,2))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO sales_window VALUES (1, 'North', 100), (2, 'North', 150), (3, 'South', 120), (4, 'South', 140)";
        cmd.ExecuteNonQuery();
    }

    public override void Execute(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "SELECT COUNT(*) FROM (SELECT ROW_NUMBER() OVER (PARTITION BY region ORDER BY amount) as rn FROM sales_window) t";
        object? count = cmd.ExecuteScalar();
        AssertEqual(1L, (long)count!, "ROW_NUMBER window function should work");
    }

    public override void Cleanup(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE sales_window";
        cmd.ExecuteNonQuery();
    }
}

[SqlTest(SqlFeatureCategory.WindowFunctions, "Test RANK and DENSE_RANK window functions", DatabaseType.MySql)]
public class RankDenseRankTest : SqlTest
{
    public override void Setup(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "CREATE TABLE scores_window (id INT PRIMARY KEY, student VARCHAR(20), score INT)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO scores_window VALUES (1, 'Alice', 95), (2, 'Bob', 90), (3, 'Charlie', 95), (4, 'David', 85)";
        cmd.ExecuteNonQuery();
    }

    public override void Execute(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "SELECT COUNT(*) FROM (SELECT RANK() OVER (ORDER BY score DESC) as rank FROM scores_window) t";
        object? count = cmd.ExecuteScalar();
        AssertEqual(1L, (long)count!, "RANK window function should work");
    }

    public override void Cleanup(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE scores_window";
        cmd.ExecuteNonQuery();
    }
}

[SqlTest(SqlFeatureCategory.WindowFunctions, "Test LAG and LEAD window functions", DatabaseType.MySql)]
public class LagLeadTest : SqlTest
{
    public override void Setup(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "CREATE TABLE prices_window (id INT PRIMARY KEY, date_col DATE, price DECIMAL(10,2))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO prices_window VALUES (1, '2024-01-01', 100), (2, '2024-01-02', 105), (3, '2024-01-03', 110)";
        cmd.ExecuteNonQuery();
    }

    public override void Execute(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "SELECT COUNT(*) FROM (SELECT LAG(price) OVER (ORDER BY date_col) as prev_price, LEAD(price) OVER (ORDER BY date_col) as next_price FROM prices_window) t";
        object? count = cmd.ExecuteScalar();
        AssertEqual(1L, (long)count!, "LAG and LEAD window functions should work");
    }

    public override void Cleanup(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE prices_window";
        cmd.ExecuteNonQuery();
    }
}

[SqlTest(SqlFeatureCategory.WindowFunctions, "Test aggregate window functions (SUM OVER)", DatabaseType.MySql)]
public class AggregateWindowTest : SqlTest
{
    public override void Setup(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "CREATE TABLE revenue_window (id INT PRIMARY KEY, month INT, amount DECIMAL(10,2))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO revenue_window VALUES (1, 1, 1000), (2, 2, 1500), (3, 3, 2000), (4, 4, 1800)";
        cmd.ExecuteNonQuery();
    }

    public override void Execute(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "SELECT COUNT(*) FROM (SELECT SUM(amount) OVER (ORDER BY month ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW) as running_total FROM revenue_window) t";
        object? count = cmd.ExecuteScalar();
        AssertEqual(1L, (long)count!, "Aggregate window functions should work");
    }

    public override void Cleanup(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE revenue_window";
        cmd.ExecuteNonQuery();
    }
}

[SqlTest(SqlFeatureCategory.WindowFunctions, "Test NTILE window function", DatabaseType.MySql)]
public class NtileTest : SqlTest
{
    public override void Setup(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "CREATE TABLE performance_window (id INT PRIMARY KEY, employee VARCHAR(20), rating INT)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO performance_window VALUES (1, 'Alice', 90), (2, 'Bob', 85), (3, 'Charlie', 95), (4, 'David', 80)";
        cmd.ExecuteNonQuery();
    }

    public override void Execute(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "SELECT COUNT(*) FROM (SELECT NTILE(2) OVER (ORDER BY rating) as quartile FROM performance_window) t";
        object? count = cmd.ExecuteScalar();
        AssertEqual(1L, (long)count!, "NTILE window function should work");
    }

    public override void Cleanup(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE performance_window";
        cmd.ExecuteNonQuery();
    }
}
