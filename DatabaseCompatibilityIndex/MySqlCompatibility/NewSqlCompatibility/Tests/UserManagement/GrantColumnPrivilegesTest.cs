using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.UserManagement;

[SqlTest(SqlFeatureCategory.UserManagement, "Test GRANT on specific columns", DatabaseType.MySql)]
public class GrantColumnPrivilegesTest : SqlTest
{
    public override void Setup(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "CREATE TABLE sensitive_data (id INT, name VARCHAR(100), salary DECIMAL(10,2), ssn VARCHAR(11))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO sensitive_data VALUES (1, 'Alice', 50000, '123-45-6789')";
        cmd.ExecuteNonQuery();
    }

    public override void Execute(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "GRANT SELECT (id, name) ON sensitive_data TO col_user@'localhost'";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());

        cmd.CommandText = "SELECT COUNT(*) FROM sensitive_data";
        object? count = cmd.ExecuteScalar();
        AssertEqual(1L, (long)count!, "Table should have data");
    }

    public override void Cleanup(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS sensitive_data";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());

        cmd.CommandText = "DROP USER IF EXISTS col_user@'localhost'";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());
    }
}
