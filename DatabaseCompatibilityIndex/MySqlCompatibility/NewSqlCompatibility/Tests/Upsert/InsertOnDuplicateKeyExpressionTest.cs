using NSCI.Testing;using NSCI.Configuration;
using System.Data.Common;

namespace NSCI.Tests.Upsert;

[SqlTest(SqlFeatureCategory.Upsert, "Test ON DUPLICATE KEY UPDATE with expressions", DatabaseType.MySql)]
public class InsertOnDuplicateKeyExpressionTest : SqlTest
{
    public override void Setup(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "CREATE TABLE counters (id INT PRIMARY KEY, count INT)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO counters VALUES (1, 10)";
        cmd.ExecuteNonQuery();
    }

    public override void Execute(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        // Increment counter on duplicate
        cmd.CommandText = "INSERT INTO counters VALUES (1, 1) ON DUPLICATE KEY UPDATE count = count + VALUES(count)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT count FROM counters WHERE id = 1";
        object? result = cmd.ExecuteScalar();
        AssertEqual(11L, (long)result!, "Counter should be incremented to 11");

        // Insert new counter
        cmd.CommandText = "INSERT INTO counters VALUES (2, 5) ON DUPLICATE KEY UPDATE count = count + VALUES(count)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT count FROM counters WHERE id = 2";
        result = cmd.ExecuteScalar();
        AssertEqual(5L, (long)result!, "New counter should be 5");
    }

    public override void Cleanup(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE counters";
        cmd.ExecuteNonQuery();
    }
}
