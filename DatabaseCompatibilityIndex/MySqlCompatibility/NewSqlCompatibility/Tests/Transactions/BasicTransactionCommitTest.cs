using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Transactions;

[SqlTest(SqlFeatureCategory.Transactions, "Test basic transaction COMMIT")]
public class BasicTransactionCommitTest : SqlTest
{
    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbTransaction transaction = connection.BeginTransaction();
        using DbCommand cmd = connection.CreateCommand();
        cmd.Transaction = transaction;
        bool committed = false;

        try
        {
            cmd.CommandText = "CREATE TABLE trans_test (id INT PRIMARY KEY, value INT)";
            cmd.ExecuteNonQuery();

            cmd.CommandText = "INSERT INTO trans_test VALUES (1, 100)";
            cmd.ExecuteNonQuery();

            transaction.Commit();
            committed = true;

            cmd.Transaction = null;
            cmd.CommandText = "SELECT COUNT(*) FROM trans_test";
            object? count = cmd.ExecuteScalar();
            AssertEqual(1L, Convert.ToInt64(count!), "Transaction should be committed");

            cmd.CommandText = "DROP TABLE trans_test";
            cmd.ExecuteNonQuery();
        }
        finally
        {
            if (!committed)
            {
                transaction.Rollback();
            }
        }
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbTransaction transaction = connection.BeginTransaction();
        using DbCommand cmd = connection.CreateCommand();
        cmd.Transaction = transaction;
        bool committed = false;

        try
        {
            cmd.CommandText = "CREATE TABLE trans_test (id INT PRIMARY KEY, value INT)";
            cmd.ExecuteNonQuery();

            cmd.CommandText = "INSERT INTO trans_test VALUES (1, 100)";
            cmd.ExecuteNonQuery();

            transaction.Commit();
            committed = true;

            cmd.Transaction = null;
            cmd.CommandText = "SELECT COUNT(*) FROM trans_test";
            object? count = cmd.ExecuteScalar();
            AssertEqual(1L, Convert.ToInt64(count!), "Transaction should be committed");

            cmd.CommandText = "DROP TABLE trans_test";
            cmd.ExecuteNonQuery();
        }
        finally
        {
            if (!committed)
            {
                transaction.Rollback();
            }
        }
    }
}
