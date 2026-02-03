using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Constraints;

[SqlTest(SqlFeatureCategory.Constraints, "Test UNIQUE constraint", DatabaseType.MySql)]
public class UniqueConstraintTest : SqlTest
{
    public override string? SetupCommand => "CREATE TABLE unique_test (id INT PRIMARY KEY, email VARCHAR(100) UNIQUE)";

    public override void Execute(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "INSERT INTO unique_test VALUES (1, 'user@example.com')";
        cmd.ExecuteNonQuery();

        // Try to insert duplicate unique value - expected to fail
        cmd.CommandText = "INSERT INTO unique_test VALUES (2, 'user@example.com')";
        cmd.ExecuteNonQuery();
    }

    public override string? CleanupCommand => "DROP TABLE unique_test";
}
