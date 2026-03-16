using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Upsert;

[SqlTest(SqlFeatureCategory.Upsert, "Test ON DUPLICATE KEY UPDATE with expressions", Configuration.DatabaseType.MySql)]
public class InsertOnDuplicateKeyExpressionTest : SqlTest
{
    protected override void SetupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "CREATE TABLE counters (id INT PRIMARY KEY, count INT)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO counters VALUES (1, 10)";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "INSERT INTO counters VALUES (1, 1) ON DUPLICATE KEY UPDATE count = count + VALUES(count)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT count FROM counters WHERE id = 1";
        object? result = cmd.ExecuteScalar();
        AssertEqual(11L, Convert.ToInt64(result!), "Counter should be incremented to 11");

        cmd.CommandText = "INSERT INTO counters VALUES (2, 5) ON DUPLICATE KEY UPDATE count = count + VALUES(count)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT count FROM counters WHERE id = 2";
        result = cmd.ExecuteScalar();
        AssertEqual(5L, Convert.ToInt64(result!), "New counter should be 5");
    }

    protected override void CleanupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE counters";
        cmd.ExecuteNonQuery();
    }
}
