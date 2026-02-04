using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Constraints;

[SqlTest(SqlFeatureCategory.Constraints, "Test DEFAULT value")]
public class DefaultValueTest : SqlTest
{
    protected override string? SetupCommandMy => "CREATE TABLE default_test (id INT PRIMARY KEY, status VARCHAR(20) DEFAULT 'active')";

    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "INSERT INTO default_test (id) VALUES (1)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT status FROM default_test WHERE id = 1";
        object? status = cmd.ExecuteScalar();
        AssertEqual("active", status?.ToString(), "Default value should be applied");
    }

    protected override string? CleanupCommandMy => "DROP TABLE default_test";
}
