using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.WindowFunctions;

[SqlTest(SqlFeatureCategory.WindowFunctions, "Test NTILE window function", DatabaseType.MySql)]
public class NtileTest : SqlTest
{
    protected override void SetupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "CREATE TABLE performance_window (id INT PRIMARY KEY, employee VARCHAR(20), rating INT)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO performance_window VALUES (1, 'Alice', 90), (2, 'Bob', 85), (3, 'Charlie', 95), (4, 'David', 80)";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "SELECT COUNT(*) FROM (SELECT NTILE(2) OVER (ORDER BY rating) as quartile FROM performance_window) t";
        object? count = cmd.ExecuteScalar();
        AssertEqual(4L, Convert.ToInt64(count!), "NTILE window function should work");
    }

    protected override void CleanupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE performance_window";
        cmd.ExecuteNonQuery();
    }
}
