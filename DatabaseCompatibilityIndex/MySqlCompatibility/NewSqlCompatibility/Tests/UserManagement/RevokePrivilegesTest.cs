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

        cmd.CommandText = "INSERT INTO revoke_test VALUES (1, 'data1'), (2, 'data2')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "GRANT SELECT, INSERT, UPDATE, DELETE ON revoke_test TO revoke_user@'localhost'";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "SHOW GRANTS FOR revoke_user@'localhost'";
        int initialGrantCount = 0;
        using (DbDataReader reader = cmd.ExecuteReader())
        {
            while (reader.Read())
            {
                initialGrantCount++;
            }
        }
        AssertTrue(initialGrantCount >= 1, "User should have initial grants");

        cmd.CommandText = "REVOKE INSERT ON revoke_test FROM revoke_user@'localhost'";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "REVOKE UPDATE, DELETE ON revoke_test FROM revoke_user@'localhost'";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SHOW GRANTS FOR revoke_user@'localhost'";
        bool hasSelectOnly = false;
        using (DbDataReader reader = cmd.ExecuteReader())
        {
            while (reader.Read())
            {
                string grant = reader.GetString(0);
                if (grant.Contains("SELECT") && grant.Contains("revoke_test"))
                {
                    hasSelectOnly = true;
                }
            }
        }
        AssertTrue(hasSelectOnly, "User should still have SELECT privilege");

        cmd.CommandText = "REVOKE ALL PRIVILEGES ON revoke_test FROM revoke_user@'localhost'";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM revoke_test";
        object? count = cmd.ExecuteScalar();
        AssertEqual(2L, (long)count!, "Table should have 2 rows");
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
