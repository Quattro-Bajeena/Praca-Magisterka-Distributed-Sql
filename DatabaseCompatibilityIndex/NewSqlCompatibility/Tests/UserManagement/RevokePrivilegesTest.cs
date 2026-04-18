using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.UserManagement;

[SqlTest(SqlFeatureCategory.UserManagement, "Test REVOKE privileges", Configuration.DatabaseType.MySql)]
public class RevokePrivilegesTest : SqlTest
{
    protected override void SetupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "CREATE USER IF NOT EXISTS revoke_user@'localhost' IDENTIFIED BY 'revoke_pass'";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "CREATE TABLE revoke_test (id INT, data VARCHAR(100))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "GRANT SELECT, INSERT ON revoke_test TO revoke_user@'localhost'";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "REVOKE INSERT ON revoke_test FROM revoke_user@'localhost'";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SHOW GRANTS FOR revoke_user@'localhost'";
        bool hasSelect = false;
        bool hasInsert = false;
        using (DbDataReader reader = cmd.ExecuteReader())
        {
            while (reader.Read())
            {
                string grant = reader.GetString(0);
                if (grant.Contains("SELECT") && grant.Contains("revoke_test")) hasSelect = true;
                if (grant.Contains("INSERT") && grant.Contains("revoke_test")) hasInsert = true;
            }
        }
        AssertTrue(hasSelect, "User should still have SELECT privilege after revoke");
        AssertTrue(!hasInsert, "INSERT privilege should be revoked");
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
