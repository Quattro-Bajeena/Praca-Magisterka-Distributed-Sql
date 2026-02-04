using MySqlConnector;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Constraints;

[SqlTest(SqlFeatureCategory.Constraints, "Test UNIQUE constraint")]
public class UniqueConstraintTest : SqlTest
{
    protected override string? SetupCommandMy => "CREATE TABLE unique_test (id INT PRIMARY KEY, email VARCHAR(100) UNIQUE)";

    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "INSERT INTO unique_test VALUES (1, 'user@example.com')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO unique_test VALUES (2, 'user@example.com')";
        AssertThrows<MySqlException>(() => cmd.ExecuteNonQuery(), "Should throw exception for unique constraint violation");
    }

    protected override string? CleanupCommandMy => "DROP TABLE unique_test";
}
