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
            {
                grantCount++;
                string grant = reader.GetString(0);
                AssertTrue(grant.Length > 0, "Grant statement should not be empty");
            }
        }
        AssertTrue(grantCount >= 1, "SHOW GRANTS should return at least one grant for current user");

        cmd.CommandText = "SELECT COUNT(*) FROM information_schema.tables";
        object? tableCount = cmd.ExecuteScalar();
        AssertTrue(tableCount != null && (long)tableCount! > 0, "Should be able to query information_schema.tables");

        cmd.CommandText = "SELECT COUNT(*) FROM information_schema.user_privileges WHERE GRANTEE LIKE CONCAT('%', USER(), '%')";
        object? privCount = cmd.ExecuteScalar();
        AssertTrue(privCount != null, "Should be able to query user_privileges");

        cmd.CommandText = "SELECT USER(), DATABASE()";
        using (DbDataReader reader = cmd.ExecuteReader())
        {
            AssertTrue(reader.Read(), "Should be able to get current user and database");
            string user = reader.GetString(0);
            AssertTrue(user.Length > 0, "Current user should not be empty");
        }

        cmd.CommandText = "SELECT COUNT(*) FROM mysql.user WHERE user = SUBSTRING_INDEX(USER(), '@', 1)";
        object? userExists = cmd.ExecuteScalar();
        AssertTrue(userExists != null && (long)userExists! >= 1, "Current user should exist in mysql.user table");
    }
}
