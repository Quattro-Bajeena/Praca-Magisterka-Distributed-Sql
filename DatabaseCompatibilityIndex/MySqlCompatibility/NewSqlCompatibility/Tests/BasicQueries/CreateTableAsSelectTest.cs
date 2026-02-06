using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.BasicQueries;

[SqlTest(SqlFeatureCategory.BasicQueries, "Test CREATE TABLE AS SELECT ")]
public class CreateTableAsSelectTest : SqlTest
{
    protected override void SetupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "CREATE TABLE source_table (id INT PRIMARY KEY, name VARCHAR(50), value INT)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO source_table VALUES (1, 'Alice', 100), (2, 'Bob', 200), (3, 'Charlie', 300)";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "CREATE TABLE destination_table AS SELECT * FROM source_table WHERE value > 100";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM destination_table";
        object? count = cmd.ExecuteScalar();
        AssertEqual(2L, Convert.ToInt64(count!), "New table should have 2 rows");

        cmd.CommandText = "SELECT SUM(value) FROM destination_table";
        object? sum = cmd.ExecuteScalar();
        AssertEqual(500L, Convert.ToInt64(sum!), "Sum should be 500 (200 + 300)");
    }

    protected override void CleanupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS destination_table";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "DROP TABLE IF EXISTS source_table";
        cmd.ExecuteNonQuery();
    }
}
