using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Constraints;

[SqlTest(SqlFeatureCategory.Constraints, "Test AUTO_INCREMENT", DatabaseType.MySql)]
public class AutoIncrementTest : SqlTest
{
    public override string? SetupCommand => "CREATE TABLE autoinc_test (id INT PRIMARY KEY AUTO_INCREMENT, name VARCHAR(50))";

    public override void Execute(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "INSERT INTO autoinc_test (name) VALUES ('Alice')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO autoinc_test (name) VALUES ('Bob')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT id FROM autoinc_test WHERE name = 'Bob'";
        object? id = cmd.ExecuteScalar();
        AssertEqual(2L, (long)id!, "AUTO_INCREMENT should assign sequential IDs");
    }

    public override string? CleanupCommand => "DROP TABLE autoinc_test";
}
