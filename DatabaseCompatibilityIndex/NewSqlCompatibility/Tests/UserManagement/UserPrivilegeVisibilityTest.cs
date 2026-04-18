using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.UserManagement;

[SqlTest(SqlFeatureCategory.UserManagement, "Test user privilege visibility", Configuration.DatabaseType.MySql)]
public class UserPrivilegeVisibilityTest : SqlTest
{
    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "SHOW GRANTS FOR CURRENT_USER()";
        int grantCount = 0;
        using (DbDataReader reader = cmd.ExecuteReader())
        {
            while (reader.Read())
                grantCount++;
        }
        AssertTrue(grantCount >= 1, "SHOW GRANTS should return at least one grant for current user");

        cmd.CommandText = "SELECT USER(), DATABASE()";
        using (DbDataReader reader = cmd.ExecuteReader())
        {
            AssertTrue(reader.Read(), "Should be able to get current user and database");
            string user = reader.GetString(0);
            AssertTrue(user.Length > 0, "Current user should not be empty");
        }
    }
}
