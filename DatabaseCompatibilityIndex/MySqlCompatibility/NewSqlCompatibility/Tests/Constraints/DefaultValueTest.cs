using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Constraints;

[SqlTest(SqlFeatureCategory.Constraints, "Test DEFAULT value", DatabaseType.MySql)]
public class DefaultValueTest : SqlTest
{
    public override string? SetupCommand => "CREATE TABLE default_test (id INT PRIMARY KEY, status VARCHAR(20) DEFAULT 'active')";

    public override void Execute(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "INSERT INTO default_test (id) VALUES (1)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT status FROM default_test WHERE id = 1";
        object? status = cmd.ExecuteScalar();
        AssertEqual("active", status?.ToString(), "Default value should be applied");
    }

    public override string? CleanupCommand => "DROP TABLE default_test";
}
