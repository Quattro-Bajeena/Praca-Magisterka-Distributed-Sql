using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Transactions;

[SqlTest(SqlFeatureCategory.Transactions, "Test basic transaction COMMIT", DatabaseType.MySql)]
public class BasicTransactionCommitTest : SqlTest
{
    public override void Execute(DbConnection connection)
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

            // Verify after commit
            cmd.Transaction = null;
            cmd.CommandText = "SELECT COUNT(*) FROM trans_test";
            object? count = cmd.ExecuteScalar();
            AssertEqual(1L, (long)count!, "Transaction should be committed");

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

[SqlTest(SqlFeatureCategory.Transactions, "Test transaction ROLLBACK", DatabaseType.MySql)]
public class TransactionRollbackTest : SqlTest
{
    public override void Execute(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        // Create table first
        cmd.CommandText = "CREATE TABLE rollback_test (id INT PRIMARY KEY, value INT)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO rollback_test VALUES (1, 100)";
        cmd.ExecuteNonQuery();

        // Start transaction
        using DbTransaction transaction = connection.BeginTransaction();
        cmd.Transaction = transaction;

        cmd.CommandText = "INSERT INTO rollback_test VALUES (2, 200)";
        cmd.ExecuteNonQuery();

        // Rollback
        transaction.Rollback();

        // Verify rollback
        cmd.Transaction = null;
        cmd.CommandText = "SELECT COUNT(*) FROM rollback_test";
        object? count = cmd.ExecuteScalar();
        AssertEqual(1L, (long)count!, "Rolled back transaction should not persist");

        cmd.CommandText = "DROP TABLE rollback_test";
        cmd.ExecuteNonQuery();
    }
}

[SqlTest(SqlFeatureCategory.Transactions, "Test multiple statements in transaction", DatabaseType.MySql)]
public class MultipleStatementsTransactionTest : SqlTest
{
    public override void Execute(DbConnection connection)
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

            // Verify final state
            cmd.Transaction = null;
            cmd.CommandText = "SELECT COUNT(*) FROM multi_trans";
            object? count = cmd.ExecuteScalar();
            AssertEqual(2L, (long)count!, "Should have 2 rows after all operations");

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

[SqlTest(SqlFeatureCategory.Transactions, "Test transaction isolation READ_COMMITTED", DatabaseType.MySql)]
public class TransactionIsolationReadCommittedTest : SqlTest
{
    public override void Execute(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        // Create test table
        cmd.CommandText = "CREATE TABLE isolation_test (id INT PRIMARY KEY, value INT)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO isolation_test VALUES (1, 100)";
        cmd.ExecuteNonQuery();

        // This is a simplified test - true isolation testing requires multiple connections
        using DbTransaction transaction = connection.BeginTransaction(System.Data.IsolationLevel.ReadCommitted);
        cmd.Transaction = transaction;

        cmd.CommandText = "SELECT value FROM isolation_test WHERE id = 1";
        object? initialValue = cmd.ExecuteScalar();
        AssertEqual(100L, (long)initialValue!, "Should read committed value");

        transaction.Commit();

        cmd.CommandText = "DROP TABLE isolation_test";
        cmd.ExecuteNonQuery();
    }
}

[SqlTest(SqlFeatureCategory.Transactions, "Test SAVEPOINT (if supported)", DatabaseType.MySql)]
public class SavePointTest : SqlTest
{
    public override void Execute(DbConnection connection)
    {
        using DbTransaction transaction = connection.BeginTransaction();
        using DbCommand cmd = connection.CreateCommand();
        cmd.Transaction = transaction;
        bool committed = false;

        try
        {
            cmd.CommandText = "CREATE TABLE savepoint_test (id INT PRIMARY KEY, value INT)";
            cmd.ExecuteNonQuery();

            cmd.CommandText = "INSERT INTO savepoint_test VALUES (1, 100)";
            cmd.ExecuteNonQuery();

            // Try to create a savepoint (syntax may vary)
            cmd.CommandText = "SAVEPOINT sp1";
            cmd.ExecuteNonQuery();

            cmd.CommandText = "INSERT INTO savepoint_test VALUES (2, 200)";
            cmd.ExecuteNonQuery();

            cmd.CommandText = "ROLLBACK TO SAVEPOINT sp1";
            cmd.ExecuteNonQuery();

            transaction.Commit();
            committed = true;

            // Verify savepoint worked
            cmd.Transaction = null;
            cmd.CommandText = "SELECT COUNT(*) FROM savepoint_test";
            object? count = cmd.ExecuteScalar();
            AssertEqual(1L, (long)count!, "SAVEPOINT rollback should have worked");

            cmd.CommandText = "DROP TABLE savepoint_test";
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
