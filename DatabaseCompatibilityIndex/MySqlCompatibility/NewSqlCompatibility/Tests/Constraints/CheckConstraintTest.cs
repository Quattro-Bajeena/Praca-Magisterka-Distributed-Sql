using MySqlConnector;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Constraints;

[SqlTest(SqlFeatureCategory.Constraints, "Test CHECK constraint (if supported)")]
public class CheckConstraintTest : SqlTest
{
    protected override string? SetupCommandMy => "CREATE TABLE check_test (id INT PRIMARY KEY, age INT CHECK (age >= 0 AND age <= 150))";

    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "INSERT INTO check_test VALUES (1, 25)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO check_test VALUES (2, -5)";
        AssertThrows<MySqlException>(() => cmd.ExecuteNonQuery(), "Should throw exception Check constraint 'check_test_chk_1' is violated");
    }

    protected override string? CleanupCommandMy => "DROP TABLE check_test";
}
