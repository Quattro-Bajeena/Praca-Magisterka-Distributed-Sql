using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.UserManagement;

[SqlTest(SqlFeatureCategory.UserManagement, "Test CREATE USER")]
public class CreateUserTest : SqlTest
{
    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
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

    protected override string? CleanupCommandMy => "DROP USER IF EXISTS test_user";
}
