using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.UserManagement;

[SqlTest(SqlFeatureCategory.UserManagement, "Test REVOKE privileges")]
public class RevokePrivilegesTest : SqlTest
{
    protected override void SetupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "CREATE TABLE revoke_test (id INT, data VARCHAR(100))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "GRANT SELECT, INSERT, UPDATE ON revoke_test TO revoke_user@'localhost'";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());
    }

    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "REVOKE INSERT ON revoke_test FROM revoke_user@'localhost'";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());

        cmd.CommandText = "REVOKE ALL PRIVILEGES ON revoke_test FROM revoke_user@'localhost'";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());

        cmd.CommandText = "SELECT COUNT(*) FROM revoke_test";
        object? count = cmd.ExecuteScalar();
        AssertEqual(0L, (long)count!, "Table should be empty");
    }

    protected override void CleanupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS revoke_test";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());

        cmd.CommandText = "DROP USER IF EXISTS revoke_user@'localhost'";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());
    }
}
