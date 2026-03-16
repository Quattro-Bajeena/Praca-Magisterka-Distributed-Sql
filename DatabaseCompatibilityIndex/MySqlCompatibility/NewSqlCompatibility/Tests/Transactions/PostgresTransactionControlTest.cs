using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Transactions;

[SqlTest(SqlFeatureCategory.Transactions, "Test PostgreSQL transaction control commands", DatabaseType.PostgreSql)]
public class PostgresTransactionControlTest : SqlTest
{
    protected override void SetupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = @"CREATE TABLE transaction_control_test (
                            id INT PRIMARY KEY,
                            status VARCHAR(50),
                            updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                        )";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO transaction_control_test (id, status) VALUES (1, 'initial')";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "BEGIN WORK";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "UPDATE transaction_control_test SET status = 'updated' WHERE id = 1";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "END WORK";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT status FROM transaction_control_test WHERE id = 1";
        object? status1 = cmd.ExecuteScalar();
        AssertEqual("updated", status1?.ToString(), "BEGIN WORK / END WORK should commit transaction");

        cmd.CommandText = "BEGIN TRANSACTION READ WRITE";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "UPDATE transaction_control_test SET status = 'read-write' WHERE id = 1";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "COMMIT";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT status FROM transaction_control_test WHERE id = 1";
        object? status2 = cmd.ExecuteScalar();
        AssertEqual("read-write", status2?.ToString(), "READ WRITE transaction should work");

        cmd.CommandText = "START TRANSACTION READ ONLY";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT status FROM transaction_control_test WHERE id = 1";
        object? status3 = cmd.ExecuteScalar();
        AssertEqual("read-write", status3?.ToString(), "READ ONLY transaction should allow reads");

        bool writeBlocked = false;
        try
        {
            cmd.CommandText = "UPDATE transaction_control_test SET status = 'should-fail' WHERE id = 1";
            cmd.ExecuteNonQuery();
        }
        catch
        {
            writeBlocked = true;
        }

        if (writeBlocked)
        {
            cmd.CommandText = "ROLLBACK";
            cmd.ExecuteNonQuery();
        }
        else
        {
            cmd.CommandText = "COMMIT";
            cmd.ExecuteNonQuery();
        }

        AssertTrue(writeBlocked, "READ ONLY transaction should block writes");

        cmd.CommandText = "BEGIN TRANSACTION DEFERRABLE";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM transaction_control_test";
        object? count = cmd.ExecuteScalar();
        AssertTrue(count != null, "DEFERRABLE transaction should work");

        cmd.CommandText = "COMMIT";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SHOW transaction_isolation";
        object? isolation = cmd.ExecuteScalar();
        AssertTrue(isolation != null, "Should be able to query transaction isolation level");

        cmd.CommandText = "SHOW transaction_read_only";
        object? readOnly = cmd.ExecuteScalar();
        AssertTrue(readOnly != null, "Should be able to query transaction read-only status");
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS transaction_control_test CASCADE";
        cmd.ExecuteNonQuery();
    }
}
