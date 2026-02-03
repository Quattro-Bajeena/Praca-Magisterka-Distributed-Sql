using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Constraints;

[SqlTest(SqlFeatureCategory.Constraints, "Test PRIMARY KEY constraint", DatabaseType.MySql)]
public class PrimaryKeyTest : SqlTest
{
    public override string? SetupCommand => "CREATE TABLE pk_test (id INT PRIMARY KEY, name VARCHAR(50))";

    public override void Execute(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "INSERT INTO pk_test VALUES (1, 'Alice')";
        cmd.ExecuteNonQuery();

        // Try to insert duplicate primary key - expected to fail
        cmd.CommandText = "INSERT INTO pk_test VALUES (1, 'Bob')";
        cmd.ExecuteNonQuery();
    }

    public override string? CleanupCommand => "DROP TABLE pk_test";
}
