using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.BasicQueries;

[SqlTest(SqlFeatureCategory.BasicQueries, "DELETE operation", DatabaseType.MySql)]
public class DeleteTest : SqlTest
{
    public override void Execute(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        // Setup
        cmd.CommandText = "CREATE TABLE records (id INT PRIMARY KEY, value INT)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO records VALUES (1, 100), (2, 200), (3, 300)";
        cmd.ExecuteNonQuery();

        // Delete
        cmd.CommandText = "DELETE FROM records WHERE id = 2";
        int affectedRows = cmd.ExecuteNonQuery();
        AssertEqual(1, affectedRows, "Should delete 1 row");

        // Verify
        cmd.CommandText = "SELECT COUNT(*) FROM records";
        object? count = cmd.ExecuteScalar();
        AssertEqual(2L, (long)count!, "Should have 2 records left");

        // Cleanup
        cmd.CommandText = "DROP TABLE records";
        cmd.ExecuteNonQuery();
    }
}
