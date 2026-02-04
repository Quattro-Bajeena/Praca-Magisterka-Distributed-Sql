using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.BasicQueries;

[SqlTest(SqlFeatureCategory.BasicQueries, "INSERT and SELECT data")]
public class InsertSelectTest : SqlTest
{
    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "CREATE TABLE users (id INT PRIMARY KEY AUTO_INCREMENT, username VARCHAR(50) NOT NULL)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO users (username) VALUES ('alice'), ('bob'), ('charlie')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM users";
        object? count = cmd.ExecuteScalar();
        AssertEqual(3L, (long)count!, "Should have 3 users");

        cmd.CommandText = "DROP TABLE users";
        cmd.ExecuteNonQuery();
    }
}
