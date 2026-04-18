using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Aggregations;

// https://dev.mysql.com/doc/refman/8.4/en/counting-rows.html
// https://www.postgresql.org/docs/current/functions-aggregate.html
[SqlTest(SqlFeatureCategory.Aggregations, "Test aggregate with NULL values")]
public class AggregateNullTest : SqlTest
{
    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "CREATE TABLE nullable_values (id INT PRIMARY KEY, value INT)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO nullable_values VALUES (1, 10), (2, NULL), (3, 20), (4, NULL), (5, 30)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM nullable_values";
        object? totalCount = cmd.ExecuteScalar();
        AssertEqual(5L, (long)totalCount!, "COUNT(*) should count all rows including NULLs");

        cmd.CommandText = "SELECT COUNT(value) FROM nullable_values";
        object? nonNullCount = cmd.ExecuteScalar();
        AssertEqual(3L, (long)nonNullCount!, "COUNT(column) should count only non-NULL values");

        cmd.CommandText = "DROP TABLE nullable_values";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "CREATE TABLE nullable_values (id INT PRIMARY KEY, value INT)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO nullable_values VALUES (1, 10), (2, NULL), (3, 20), (4, NULL), (5, 30)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM nullable_values";
        object? totalCount = cmd.ExecuteScalar();
        AssertEqual(5L, (long)totalCount!, "COUNT(*) should count all rows including NULLs");

        cmd.CommandText = "SELECT COUNT(value) FROM nullable_values";
        object? nonNullCount = cmd.ExecuteScalar();
        AssertEqual(3L, (long)nonNullCount!, "COUNT(column) should count only non-NULL values");

        cmd.CommandText = "DROP TABLE nullable_values";
        cmd.ExecuteNonQuery();
    }
}
