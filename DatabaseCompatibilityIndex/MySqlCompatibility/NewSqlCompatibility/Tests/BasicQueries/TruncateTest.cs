using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.BasicQueries;

[SqlTest(SqlFeatureCategory.BasicQueries, "TRUNCATE table operation", DatabaseType.MySql)]
public class TruncateTest : SqlTest
{
    public override void Execute(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        // Setup
        cmd.CommandText = "CREATE TABLE temp_data (id INT PRIMARY KEY, data VARCHAR(50))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO temp_data VALUES (1, 'a'), (2, 'b'), (3, 'c')";
        cmd.ExecuteNonQuery();

        // Truncate
        cmd.CommandText = "TRUNCATE TABLE temp_data";
        cmd.ExecuteNonQuery();

        // Verify
        cmd.CommandText = "SELECT COUNT(*) FROM temp_data";
        object? count = cmd.ExecuteScalar();
        AssertEqual(0L, (long)count!, "Table should be empty after truncate");

        // Cleanup
        cmd.CommandText = "DROP TABLE temp_data";
        cmd.ExecuteNonQuery();
    }
}
