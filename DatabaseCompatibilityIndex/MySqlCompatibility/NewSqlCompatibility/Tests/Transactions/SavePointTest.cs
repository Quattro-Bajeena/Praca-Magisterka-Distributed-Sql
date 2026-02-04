using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Transactions;

[SqlTest(SqlFeatureCategory.Transactions, "Test SAVEPOINT (if supported)", DatabaseType.MySql)]
public class SavePointTest : SqlTest
{
    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "CREATE TABLE savepoint_test (id INT PRIMARY KEY, value INT)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "START TRANSACTION";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO savepoint_test VALUES (1, 100)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SAVEPOINT sp1";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO savepoint_test VALUES (2, 200)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "ROLLBACK TO SAVEPOINT sp1";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "COMMIT";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM savepoint_test";
        object? count = cmd.ExecuteScalar();
        AssertEqual(1L, Convert.ToInt64(count!), "SAVEPOINT rollback should have worked");

        cmd.CommandText = "DROP TABLE savepoint_test";
        cmd.ExecuteNonQuery();
    }
}

