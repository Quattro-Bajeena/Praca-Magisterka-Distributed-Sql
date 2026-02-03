using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.BasicQueries;

[SqlTest(SqlFeatureCategory.BasicQueries, "INSERT and SELECT data", DatabaseType.MySql)]
public class InsertSelectTest : SqlTest
{
    public override void Execute(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        // Create table
        cmd.CommandText = "CREATE TABLE users (id INT PRIMARY KEY AUTO_INCREMENT, username VARCHAR(50) NOT NULL)";
        cmd.ExecuteNonQuery();

        // Insert data
        cmd.CommandText = "INSERT INTO users (username) VALUES ('alice'), ('bob'), ('charlie')";
        cmd.ExecuteNonQuery();

        // Verify count
        cmd.CommandText = "SELECT COUNT(*) FROM users";
        object? count = cmd.ExecuteScalar();
        AssertEqual(3L, (long)count!, "Should have 3 users");

        // Cleanup
        cmd.CommandText = "DROP TABLE users";
        cmd.ExecuteNonQuery();
    }
}
