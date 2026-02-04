using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Constraints;

[SqlTest(SqlFeatureCategory.Constraints, "Test AUTO_INCREMENT")]
public class AutoIncrementTest : SqlTest
{
    protected override string? SetupCommandMy => "CREATE TABLE autoinc_test (id INT PRIMARY KEY AUTO_INCREMENT, name VARCHAR(50))";

    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "INSERT INTO autoinc_test (name) VALUES ('Alice')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO autoinc_test (name) VALUES ('Bob')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT id FROM autoinc_test WHERE name = 'Bob'";
        object? id = cmd.ExecuteScalar();
        AssertEqual(2, Convert.ToInt32(id!), "AUTO_INCREMENT should assign sequential IDs");
    }

    protected override string? CleanupCommandMy => "DROP TABLE autoinc_test";
}
