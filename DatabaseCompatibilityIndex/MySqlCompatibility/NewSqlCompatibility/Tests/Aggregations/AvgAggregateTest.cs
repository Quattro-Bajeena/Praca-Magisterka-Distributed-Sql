using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Aggregations;

[SqlTest(SqlFeatureCategory.Aggregations, "Test AVG aggregate function")]
public class AvgAggregateTest : SqlTest
{
    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "CREATE TABLE scores (id INT PRIMARY KEY, score INT)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO scores VALUES (1, 100), (2, 80), (3, 90)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT AVG(score) FROM scores";
        object? avg = cmd.ExecuteScalar();
        decimal avgValue = Convert.ToDecimal(avg);
        AssertTrue(Math.Abs(avgValue - 90m) < 1m, "AVG should be approximately 90");

        cmd.CommandText = "DROP TABLE scores";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "CREATE TABLE scores (id INT PRIMARY KEY, score INT)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO scores VALUES (1, 100), (2, 80), (3, 90)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT AVG(score) FROM scores";
        object? avg = cmd.ExecuteScalar();
        decimal avgValue = Convert.ToDecimal(avg);
        AssertTrue(Math.Abs(avgValue - 90m) < 1m, "AVG should be approximately 90");

        cmd.CommandText = "DROP TABLE scores";
        cmd.ExecuteNonQuery();
    }
}
