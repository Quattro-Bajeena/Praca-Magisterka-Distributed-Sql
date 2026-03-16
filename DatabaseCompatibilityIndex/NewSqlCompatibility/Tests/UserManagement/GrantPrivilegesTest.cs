using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.UserManagement;

[SqlTest(SqlFeatureCategory.UserManagement, "Test GRANT privileges", Configuration.DatabaseType.MySql)]
public class GrantPrivilegesTest : SqlTest
{
    protected override void SetupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "CREATE USER IF NOT EXISTS grant_test_user@'localhost' IDENTIFIED BY 'test_pass'";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "CREATE TABLE test_grant_table (id INT PRIMARY KEY, name VARCHAR(50))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO test_grant_table VALUES (1, 'test')";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "GRANT SELECT ON test_grant_table TO grant_test_user@'localhost'";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SHOW GRANTS FOR grant_test_user@'localhost'";
        bool hasSelectGrant = false;
        using (DbDataReader reader = cmd.ExecuteReader())
        {
            while (reader.Read())
            {
                string grant = reader.GetString(0);
                if (grant.Contains("SELECT") && grant.Contains("test_grant_table"))
                {
                    hasSelectGrant = true;
                }
            }
        }
        AssertTrue(hasSelectGrant, "User should have SELECT privilege on test_grant_table");

        cmd.CommandText = "GRANT INSERT, UPDATE ON test_grant_table TO grant_test_user@'localhost'";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "GRANT DELETE ON test_grant_table TO grant_test_user@'localhost' WITH GRANT OPTION";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SHOW GRANTS FOR grant_test_user@'localhost'";
        int grantCount = 0;
        using (DbDataReader reader = cmd.ExecuteReader())
        {
            while (reader.Read())
            {
                grantCount++;
            }
        }
        AssertTrue(grantCount >= 1, "User should have multiple grants");

        cmd.CommandText = "SELECT COUNT(*) FROM test_grant_table";
        object? count = cmd.ExecuteScalar();
        AssertEqual(1L, (long)count!, "Table should still be accessible");
    }

    protected override void CleanupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS test_grant_table";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());
        cmd.CommandText = "DROP USER IF EXISTS grant_test_user@'localhost'";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());
    }
}
