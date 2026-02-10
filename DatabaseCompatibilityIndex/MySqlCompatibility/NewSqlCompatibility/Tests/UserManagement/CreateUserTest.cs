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

        cmd.CommandText = "CREATE USER IF NOT EXISTS test_user2@'localhost' IDENTIFIED BY 'pass123'";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM mysql.user WHERE user = 'test_user2'";
        count = cmd.ExecuteScalar();
        AssertTrue(count != null && (long)count! >= 1, "User with host should be created");

        cmd.CommandText = "CREATE USER IF NOT EXISTS test_user3 IDENTIFIED WITH mysql_native_password BY 'secure_pass'";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM mysql.user WHERE user LIKE 'test_user%'";
        count = cmd.ExecuteScalar();
        AssertTrue(count != null && (long)count! >= 3, "Should have created 3 test users");
    }

    protected override void CleanupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP USER IF EXISTS test_user";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());
        cmd.CommandText = "DROP USER IF EXISTS test_user2@'localhost'";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());
        cmd.CommandText = "DROP USER IF EXISTS test_user3";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());
    }
}
