using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.WindowFunctions;

[SqlTest(SqlFeatureCategory.WindowFunctions, "Test LAG and LEAD window functions")]
public class LagLeadTest : SqlTest
{
    protected override void SetupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "CREATE TABLE prices_window (id INT PRIMARY KEY, date_col DATE, price DECIMAL(10,2))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO prices_window VALUES (1, '2024-01-01', 100), (2, '2024-01-02', 105), (3, '2024-01-03', 110)";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "SELECT COUNT(*) FROM (SELECT LAG(price) OVER (ORDER BY date_col) as prev_price, LEAD(price) OVER (ORDER BY date_col) as next_price FROM prices_window) t";
        object? count = cmd.ExecuteScalar();
        AssertEqual(3L, Convert.ToInt64(count!), "LAG and LEAD window functions should work");
    }

    protected override void CleanupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE prices_window";
        cmd.ExecuteNonQuery();
    }

    protected override void SetupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "CREATE TABLE prices_window (id INT PRIMARY KEY, date_col DATE, price DECIMAL(10,2))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO prices_window VALUES (1, '2024-01-01', 100), (2, '2024-01-02', 105), (3, '2024-01-03', 110)";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "SELECT COUNT(*) FROM (SELECT LAG(price) OVER (ORDER BY date_col) as prev_price, LEAD(price) OVER (ORDER BY date_col) as next_price FROM prices_window) t";
        object? count = cmd.ExecuteScalar();
        AssertEqual(3L, Convert.ToInt64(count!), "LAG and LEAD window functions should work");
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE prices_window";
        cmd.ExecuteNonQuery();
    }
}
