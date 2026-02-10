using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.BasicQueries;

[SqlTest(SqlFeatureCategory.BasicQueries, "TRUNCATE table operation")]
public class TruncateTest : SqlTest
{
    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "CREATE TABLE temp_data (id INT PRIMARY KEY, data VARCHAR(50))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO temp_data VALUES (1, 'a'), (2, 'b'), (3, 'c')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "TRUNCATE TABLE temp_data";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM temp_data";
        object? count = cmd.ExecuteScalar();
        AssertEqual(0L, (long)count!, "Table should be empty after truncate");

        cmd.CommandText = "DROP TABLE temp_data";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "CREATE TABLE temp_data (id INT PRIMARY KEY, data VARCHAR(50))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO temp_data VALUES (1, 'a'), (2, 'b'), (3, 'c')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "TRUNCATE TABLE temp_data";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM temp_data";
        object? count = cmd.ExecuteScalar();
        AssertEqual(0L, (long)count!, "Table should be empty after truncate");

        cmd.CommandText = "DROP TABLE temp_data";
        cmd.ExecuteNonQuery();
    }
}
