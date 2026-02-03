using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Constraints;

[SqlTest(SqlFeatureCategory.Constraints, "Test CHECK constraint (if supported)", DatabaseType.MySql)]
public class CheckConstraintTest : SqlTest
{
    public override string? SetupCommand => "CREATE TABLE check_test (id INT PRIMARY KEY, age INT CHECK (age >= 0 AND age <= 150))";

    public override void Execute(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "INSERT INTO check_test VALUES (1, 25)";
        cmd.ExecuteNonQuery();

        // Try to insert invalid value - expected to fail
        cmd.CommandText = "INSERT INTO check_test VALUES (2, -5)";
        cmd.ExecuteNonQuery();
    }

    public override string? CleanupCommand => "DROP TABLE check_test";
}
