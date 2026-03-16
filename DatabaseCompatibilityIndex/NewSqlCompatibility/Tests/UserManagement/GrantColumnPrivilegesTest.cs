using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.UserManagement;

[SqlTest(SqlFeatureCategory.UserManagement, "Test GRANT on specific columns", Configuration.DatabaseType.MySql)]
public class GrantColumnPrivilegesTest : SqlTest
{
    protected override void SetupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "CREATE USER IF NOT EXISTS col_user@'localhost' IDENTIFIED BY 'col_pass'";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "CREATE TABLE sensitive_data (id INT, name VARCHAR(100), salary DECIMAL(10,2), ssn VARCHAR(11))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO sensitive_data VALUES (1, 'Alice', 50000, '123-45-6789')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO sensitive_data VALUES (2, 'Bob', 60000, '987-65-4321')";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "GRANT SELECT (id, name) ON sensitive_data TO col_user@'localhost'";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "GRANT UPDATE (name) ON sensitive_data TO col_user@'localhost'";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SHOW GRANTS FOR col_user@'localhost'";
        bool hasColumnGrant = false;
        using (DbDataReader reader = cmd.ExecuteReader())
        {
            while (reader.Read())
            {
                string grant = reader.GetString(0);
                if (grant.Contains("id") || grant.Contains("name"))
                {
                    hasColumnGrant = true;
                }
            }
        }
        AssertTrue(hasColumnGrant, "User should have column-level privileges");

        cmd.CommandText = "SELECT COUNT(*) FROM sensitive_data";
        object? count = cmd.ExecuteScalar();
        AssertEqual(2L, (long)count!, "Table should have 2 rows of data");

        cmd.CommandText = "GRANT SELECT (salary) ON sensitive_data TO col_user@'localhost'";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "REVOKE SELECT (salary) ON sensitive_data FROM col_user@'localhost'";
        cmd.ExecuteNonQuery();
    }

    protected override void CleanupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS sensitive_data";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());
        cmd.CommandText = "DROP USER IF EXISTS col_user@'localhost'";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());
    }
}
