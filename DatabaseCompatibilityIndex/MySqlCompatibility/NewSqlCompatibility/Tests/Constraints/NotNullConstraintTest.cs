using MySqlConnector;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Constraints;

[SqlTest(SqlFeatureCategory.Constraints, "Test NOT NULL constraint")]
public class NotNullConstraintTest : SqlTest
{
    protected override string? SetupCommandMy => "CREATE TABLE notnull_test (id INT PRIMARY KEY, required_field VARCHAR(50) NOT NULL)";

    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "INSERT INTO notnull_test VALUES (1, NULL)";
        AssertThrows<MySqlException>(() => cmd.ExecuteNonQuery(), "Should throw exception for NOT NULL constraint violation");
    }

    protected override string? CleanupCommandMy => "DROP TABLE notnull_test";
}
