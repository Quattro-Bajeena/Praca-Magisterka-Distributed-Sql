using NSCI.Testing;using NSCI.Configuration;
using System.Data.Common;

namespace NSCI.Tests.Locking;

[SqlTest(SqlFeatureCategory.Locking, "Test SELECT...FOR UPDATE row locking", DatabaseType.MySql)]
public class SelectForUpdateTest : SqlTest
{
    public override void Setup(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "CREATE TABLE accounts (id INT PRIMARY KEY, balance DECIMAL(10,2))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO accounts VALUES (1, 1000.00), (2, 500.00)";
        cmd.ExecuteNonQuery();
    }

    public override void Execute(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        // Lock row for update within transaction
        cmd.CommandText = "BEGIN";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT balance FROM accounts WHERE id = 1 FOR UPDATE";
        object? balance = cmd.ExecuteScalar();
        AssertEqual(1000.00m, (decimal)balance!, "Should read locked row correctly");

        // Update locked row
        cmd.CommandText = "UPDATE accounts SET balance = 900.00 WHERE id = 1";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "COMMIT";
        cmd.ExecuteNonQuery();

        // Verify update was applied
        cmd.CommandText = "SELECT balance FROM accounts WHERE id = 1";
        object? newBalance = cmd.ExecuteScalar();
        AssertEqual(900.00m, (decimal)newBalance!, "Balance should be updated to 900");
    }

    public override void Cleanup(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE accounts";
        cmd.ExecuteNonQuery();
    }
}
