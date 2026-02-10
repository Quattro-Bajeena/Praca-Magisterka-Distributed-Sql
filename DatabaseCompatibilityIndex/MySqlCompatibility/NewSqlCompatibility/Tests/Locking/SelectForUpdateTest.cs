using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Locking;

[SqlTest(SqlFeatureCategory.Locking, "Test SELECT...FOR UPDATE row locking")]
public class SelectForUpdateTest : SqlTest
{
    protected override void SetupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "CREATE TABLE accounts (id INT PRIMARY KEY, balance DECIMAL(10,2))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO accounts VALUES (1, 1000.00), (2, 500.00)";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "BEGIN";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT balance FROM accounts WHERE id = 1 FOR UPDATE";
        object? balance = cmd.ExecuteScalar();
        AssertEqual(1000.00m, (decimal)balance!, "Should read locked row correctly");

        cmd.CommandText = "UPDATE accounts SET balance = 900.00 WHERE id = 1";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "COMMIT";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT balance FROM accounts WHERE id = 1";
        object? newBalance = cmd.ExecuteScalar();
        AssertEqual(900.00m, (decimal)newBalance!, "Balance should be updated to 900");
    }

    protected override void CleanupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE accounts";
        cmd.ExecuteNonQuery();
    }

    protected override void SetupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "CREATE TABLE accounts (id INT PRIMARY KEY, balance DECIMAL(10,2))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO accounts VALUES (1, 1000.00), (2, 500.00)";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "BEGIN";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT balance FROM accounts WHERE id = 1 FOR UPDATE";
        object? balance = cmd.ExecuteScalar();
        AssertEqual(1000.00m, (decimal)balance!, "Should read locked row correctly");

        cmd.CommandText = "UPDATE accounts SET balance = 900.00 WHERE id = 1";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "COMMIT";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT balance FROM accounts WHERE id = 1";
        object? newBalance = cmd.ExecuteScalar();
        AssertEqual(900.00m, (decimal)newBalance!, "Balance should be updated to 900");
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS accounts";
        cmd.ExecuteNonQuery();
    }
}
