using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Transactions;

[SqlTest(SqlFeatureCategory.Transactions, "Test transaction ROLLBACK")]
public class TransactionRollbackTest : SqlTest
{
    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "CREATE TABLE rollback_test (id INT PRIMARY KEY, value INT)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO rollback_test VALUES (1, 100)";
        cmd.ExecuteNonQuery();

        using DbTransaction transaction = connection.BeginTransaction();
        cmd.Transaction = transaction;

        cmd.CommandText = "INSERT INTO rollback_test VALUES (2, 200)";
        cmd.ExecuteNonQuery();

        transaction.Rollback();

        cmd.Transaction = null;
        cmd.CommandText = "SELECT COUNT(*) FROM rollback_test";
        object? count = cmd.ExecuteScalar();
        AssertEqual(1L, Convert.ToInt64(count!), "Rolled back transaction should not persist");

        cmd.CommandText = "DROP TABLE rollback_test";
        cmd.ExecuteNonQuery();
    }
}
