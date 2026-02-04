using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Transactions;

[SqlTest(SqlFeatureCategory.Transactions, "Test multiple statements in transaction")]
public class MultipleStatementsTransactionTest : SqlTest
{
    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbTransaction transaction = connection.BeginTransaction();
        using DbCommand cmd = connection.CreateCommand();
        cmd.Transaction = transaction;
        bool committed = false;

        try
        {
            cmd.CommandText = "CREATE TABLE multi_trans (id INT PRIMARY KEY, value INT)";
            cmd.ExecuteNonQuery();

            cmd.CommandText = "INSERT INTO multi_trans VALUES (1, 100), (2, 200), (3, 300)";
            cmd.ExecuteNonQuery();

            cmd.CommandText = "UPDATE multi_trans SET value = 150 WHERE id = 1";
            cmd.ExecuteNonQuery();

            cmd.CommandText = "DELETE FROM multi_trans WHERE id = 3";
            cmd.ExecuteNonQuery();

            transaction.Commit();
            committed = true;

            cmd.Transaction = null;
            cmd.CommandText = "SELECT COUNT(*) FROM multi_trans";
            object? count = cmd.ExecuteScalar();
            AssertEqual(2L, Convert.ToInt64(count!), "Should have 2 rows after all operations");

            cmd.CommandText = "DROP TABLE multi_trans";
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
