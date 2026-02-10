using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Transactions;

[SqlTest(SqlFeatureCategory.Transactions, "Test PostgreSQL Serializable Snapshot Isolation", DatabaseType.PostgreSql)]
public class PostgresSerializableIsolationTest : SqlTest
{
    protected override void SetupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = @"CREATE TABLE accounts_ssi (
                            id INT PRIMARY KEY,
                            balance DECIMAL(10,2),
                            account_type VARCHAR(50)
                        )";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO accounts_ssi VALUES (1, 1000.00, 'checking'), (2, 2000.00, 'savings')";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd1 = connection.CreateCommand();
        using DbCommand cmd2 = connectionSecond.CreateCommand();

        cmd1.CommandText = "BEGIN TRANSACTION ISOLATION LEVEL SERIALIZABLE";
        cmd1.ExecuteNonQuery();

        cmd1.CommandText = "SELECT balance FROM accounts_ssi WHERE id = 1";
        object? balance1_tx1 = cmd1.ExecuteScalar();
        AssertEqual(1000.00m, Convert.ToDecimal(balance1_tx1!), "Initial balance should be 1000");

        cmd2.CommandText = "BEGIN TRANSACTION ISOLATION LEVEL SERIALIZABLE";
        cmd2.ExecuteNonQuery();

        cmd2.CommandText = "SELECT balance FROM accounts_ssi WHERE id = 2";
        object? balance2_tx2 = cmd2.ExecuteScalar();
        AssertEqual(2000.00m, Convert.ToDecimal(balance2_tx2!), "Initial balance should be 2000");

        cmd1.CommandText = "UPDATE accounts_ssi SET balance = balance - 100 WHERE id = 1";
        cmd1.ExecuteNonQuery();

        cmd2.CommandText = "UPDATE accounts_ssi SET balance = balance - 100 WHERE id = 2";
        cmd2.ExecuteNonQuery();

        cmd1.CommandText = "COMMIT";
        cmd1.ExecuteNonQuery();

        cmd2.CommandText = "COMMIT";
        cmd2.ExecuteNonQuery();

        cmd1.CommandText = "SELECT balance FROM accounts_ssi WHERE id = 1";
        object? finalBalance1 = cmd1.ExecuteScalar();
        AssertEqual(900.00m, Convert.ToDecimal(finalBalance1!), "Balance 1 should be reduced");

        cmd1.CommandText = "SELECT balance FROM accounts_ssi WHERE id = 2";
        object? finalBalance2 = cmd1.ExecuteScalar();
        AssertEqual(1900.00m, Convert.ToDecimal(finalBalance2!), "Balance 2 should be reduced");

        cmd1.CommandText = "BEGIN TRANSACTION ISOLATION LEVEL SERIALIZABLE";
        cmd1.ExecuteNonQuery();

        cmd1.CommandText = "SELECT SUM(balance) FROM accounts_ssi";
        object? sum1 = cmd1.ExecuteScalar();

        cmd2.CommandText = "BEGIN TRANSACTION ISOLATION LEVEL SERIALIZABLE";
        cmd2.ExecuteNonQuery();

        cmd2.CommandText = "SELECT SUM(balance) FROM accounts_ssi";
        object? sum2 = cmd2.ExecuteScalar();

        AssertEqual(sum1, sum2, "Both transactions should see consistent snapshot");

        cmd1.CommandText = "COMMIT";
        cmd1.ExecuteNonQuery();

        cmd2.CommandText = "COMMIT";
        cmd2.ExecuteNonQuery();

        cmd1.CommandText = "SELECT COUNT(*) FROM accounts_ssi";
        object? count = cmd1.ExecuteScalar();
        AssertEqual(2L, Convert.ToInt64(count!), "Should have 2 accounts");
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS accounts_ssi CASCADE";
        cmd.ExecuteNonQuery();
    }
}
