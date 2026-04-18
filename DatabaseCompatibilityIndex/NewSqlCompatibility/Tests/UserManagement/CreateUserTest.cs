using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.UserManagement;

[SqlTest(SqlFeatureCategory.UserManagement, "Test CREATE USER", Configuration.DatabaseType.MySql)]
public class CreateUserTest : SqlTest
{
    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "CREATE USER IF NOT EXISTS test_user IDENTIFIED BY 'test_password'";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM mysql.user WHERE user = 'test_user'";
        object? count = cmd.ExecuteScalar();
        AssertTrue(count != null && (long)count! >= 1, "User should be created in mysql.user table");
    }

    protected override void CleanupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP USER IF EXISTS test_user";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());
    }
}
