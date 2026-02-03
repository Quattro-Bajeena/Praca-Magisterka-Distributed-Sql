using NSCI.Testing;using NSCI.Configuration;
using System.Data.Common;

namespace NSCI.Tests.Upsert;

[SqlTest(SqlFeatureCategory.Upsert, "Test INSERT...ON DUPLICATE KEY UPDATE", DatabaseType.MySql)]
public class InsertOnDuplicateKeyTest : SqlTest
{
    public override void Setup(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "CREATE TABLE upsert_test (id INT PRIMARY KEY, name VARCHAR(50), email VARCHAR(100))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO upsert_test VALUES (1, 'Alice', 'alice@example.com')";
        cmd.ExecuteNonQuery();
    }

    public override void Execute(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        // Insert new row
        cmd.CommandText = "INSERT INTO upsert_test VALUES (2, 'Bob', 'bob@example.com') ON DUPLICATE KEY UPDATE name = VALUES(name)";
        cmd.ExecuteNonQuery();

        // Update existing row
        cmd.CommandText = "INSERT INTO upsert_test VALUES (1, 'Alice Updated', 'alice_new@example.com') ON DUPLICATE KEY UPDATE name = VALUES(name), email = VALUES(email)";
        cmd.ExecuteNonQuery();

        // Verify the update
        cmd.CommandText = "SELECT name, email FROM upsert_test WHERE id = 1";
        using DbDataReader reader = cmd.ExecuteReader();
        AssertTrue(reader.Read(), "Should have data for id=1");
        AssertEqual("Alice Updated", reader.GetString(0), "Name should be updated");
        AssertEqual("alice_new@example.com", reader.GetString(1), "Email should be updated");
    }

    public override void Cleanup(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE upsert_test";
        cmd.ExecuteNonQuery();
    }
}
