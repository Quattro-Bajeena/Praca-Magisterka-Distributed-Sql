using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.WindowFunctions;

[SqlTest(SqlFeatureCategory.WindowFunctions, "Test RANK and DENSE_RANK window functions")]
public class RankDenseRankTest : SqlTest
{
    protected override void SetupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "CREATE TABLE scores_window (id INT PRIMARY KEY, student VARCHAR(20), score INT)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO scores_window VALUES (1, 'Alice', 95), (2, 'Bob', 90), (3, 'Charlie', 95), (4, 'David', 85)";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "SELECT COUNT(*) FROM (SELECT RANK() OVER (ORDER BY score DESC) as score_rank FROM scores_window) t";
        object? count = cmd.ExecuteScalar();
        AssertEqual(4L, Convert.ToInt64(count!), "RANK window function should work");
    }

    protected override void CleanupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE scores_window";
        cmd.ExecuteNonQuery();
    }

    protected override void SetupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "CREATE TABLE scores_window (id INT PRIMARY KEY, student VARCHAR(20), score INT)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO scores_window VALUES (1, 'Alice', 95), (2, 'Bob', 90), (3, 'Charlie', 95), (4, 'David', 85)";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "SELECT COUNT(*) FROM (SELECT RANK() OVER (ORDER BY score DESC) as score_rank FROM scores_window) t";
        object? count = cmd.ExecuteScalar();
        AssertEqual(4L, Convert.ToInt64(count!), "RANK window function should work");
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE scores_window";
        cmd.ExecuteNonQuery();
    }
}
