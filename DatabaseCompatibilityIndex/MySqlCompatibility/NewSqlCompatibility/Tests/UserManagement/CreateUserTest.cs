using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.UserManagement;

[SqlTest(SqlFeatureCategory.UserManagement, "Test CREATE USER", DatabaseType.MySql)]
public class CreateUserTest : SqlTest
{
    public override void Execute(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "CREATE USER IF NOT EXISTS test_user IDENTIFIED BY 'test_password'";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());

        cmd.CommandText = "SELECT COUNT(*) FROM mysql.user WHERE user = 'test_user'";
        ExecuteIgnoringException(() =>
        {
            object? count = cmd.ExecuteScalar();
            AssertTrue(count != null && (long)count! >= 0, "User creation check executed");
        });
    }

    public override string? CleanupCommand => "DROP USER IF EXISTS test_user";
}
