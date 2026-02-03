using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Constraints;

[SqlTest(SqlFeatureCategory.Constraints, "Test NOT NULL constraint", DatabaseType.MySql)]
public class NotNullConstraintTest : SqlTest
{
    public override string? SetupCommand => "CREATE TABLE notnull_test (id INT PRIMARY KEY, required_field VARCHAR(50) NOT NULL)";

    public override void Execute(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        // Try to insert NULL in NOT NULL column - expected to fail
        cmd.CommandText = "INSERT INTO notnull_test VALUES (1, NULL)";
        cmd.ExecuteNonQuery();
    }

    public override string? CleanupCommand => "DROP TABLE notnull_test";
}
