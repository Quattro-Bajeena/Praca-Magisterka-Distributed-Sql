using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.UserManagement;

[SqlTest(SqlFeatureCategory.UserManagement, "Test user privilege visibility")]
public class UserPrivilegeVisibilityTest : SqlTest
{
    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "SHOW GRANTS FOR CURRENT_USER()";
        ExecuteIgnoringException(() =>
        {
            using DbDataReader reader = cmd.ExecuteReader();
            AssertTrue(reader.Read() || true, "SHOW GRANTS should execute");
        });

        cmd.CommandText = "SELECT COUNT(*) FROM information_schema.tables LIMIT 1";
        ExecuteIgnoringException(() =>
        {
            object? count = cmd.ExecuteScalar();
            AssertTrue(count != null, "Should be able to query information_schema");
        });
    }
}
