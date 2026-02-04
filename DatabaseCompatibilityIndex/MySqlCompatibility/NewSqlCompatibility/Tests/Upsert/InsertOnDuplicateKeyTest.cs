using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Upsert;

[SqlTest(SqlFeatureCategory.Upsert, "Test INSERT...ON DUPLICATE KEY UPDATE")]
public class InsertOnDuplicateKeyTest : SqlTest
{
    protected override void SetupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "CREATE TABLE upsert_test (id INT PRIMARY KEY, name VARCHAR(50), email VARCHAR(100))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO upsert_test VALUES (1, 'Alice', 'alice@example.com')";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "INSERT INTO upsert_test VALUES (2, 'Bob', 'bob@example.com') ON DUPLICATE KEY UPDATE name = VALUES(name)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO upsert_test VALUES (1, 'Alice Updated', 'alice_new@example.com') ON DUPLICATE KEY UPDATE name = VALUES(name), email = VALUES(email)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT name, email FROM upsert_test WHERE id = 1";
        using DbDataReader reader = cmd.ExecuteReader();
        AssertTrue(reader.Read(), "Should have data for id=1");
        AssertEqual("Alice Updated", reader.GetString(0), "Name should be updated");
        AssertEqual("alice_new@example.com", reader.GetString(1), "Email should be updated");
    }

    protected override void CleanupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE upsert_test";
        cmd.ExecuteNonQuery();
    }
}
