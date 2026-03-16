using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Transactions;

[SqlTest(SqlFeatureCategory.Transactions, "Test transaction isolation READ_COMMITTED")]
public class TransactionIsolationReadCommittedTest : SqlTest
{
    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "CREATE TABLE isolation_test (id INT PRIMARY KEY, value INT)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO isolation_test VALUES (1, 100)";
        cmd.ExecuteNonQuery();

        using DbTransaction transaction = connection.BeginTransaction(System.Data.IsolationLevel.ReadCommitted);
        cmd.Transaction = transaction;

        cmd.CommandText = "SELECT value FROM isolation_test WHERE id = 1";
        object? initialValue = cmd.ExecuteScalar();
        AssertEqual(100L, Convert.ToInt64(initialValue!), "Should read committed value");

        transaction.Commit();

        cmd.Transaction = null;
        cmd.CommandText = "DROP TABLE isolation_test";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "CREATE TABLE isolation_test (id INT PRIMARY KEY, value INT)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO isolation_test VALUES (1, 100)";
        cmd.ExecuteNonQuery();

        using DbTransaction transaction = connection.BeginTransaction(System.Data.IsolationLevel.ReadCommitted);
        cmd.Transaction = transaction;

        cmd.CommandText = "SELECT value FROM isolation_test WHERE id = 1";
        object? initialValue = cmd.ExecuteScalar();
        AssertEqual(100L, Convert.ToInt64(initialValue!), "Should read committed value");

        transaction.Commit();

        cmd.Transaction = null;
        cmd.CommandText = "DROP TABLE isolation_test";
        cmd.ExecuteNonQuery();
    }
}
