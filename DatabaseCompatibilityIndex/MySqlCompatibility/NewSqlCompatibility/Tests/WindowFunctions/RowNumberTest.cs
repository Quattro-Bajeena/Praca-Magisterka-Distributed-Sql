using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.WindowFunctions;

[SqlTest(SqlFeatureCategory.WindowFunctions, "Test ROW_NUMBER window function")]
public class RowNumberTest : SqlTest
{
    protected override void SetupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "CREATE TABLE sales_window (id INT PRIMARY KEY, region VARCHAR(20), amount DECIMAL(10,2))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO sales_window VALUES (1, 'North', 100), (2, 'North', 150), (3, 'South', 120), (4, 'South', 140)";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "SELECT COUNT(*) FROM (SELECT ROW_NUMBER() OVER (PARTITION BY region ORDER BY amount) as rn FROM sales_window) t";
        object? count = cmd.ExecuteScalar();
        AssertEqual(4L, Convert.ToInt64(count!), "ROW_NUMBER window function should work");
    }

    protected override void CleanupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE sales_window";
        cmd.ExecuteNonQuery();
    }

    protected override void SetupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "CREATE TABLE sales_window (id INT PRIMARY KEY, region VARCHAR(20), amount DECIMAL(10,2))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO sales_window VALUES (1, 'North', 100), (2, 'North', 150), (3, 'South', 120), (4, 'South', 140)";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "SELECT COUNT(*) FROM (SELECT ROW_NUMBER() OVER (PARTITION BY region ORDER BY amount) as rn FROM sales_window) t";
        object? count = cmd.ExecuteScalar();
        AssertEqual(4L, Convert.ToInt64(count!), "ROW_NUMBER window function should work");
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE sales_window";
        cmd.ExecuteNonQuery();
    }
}
