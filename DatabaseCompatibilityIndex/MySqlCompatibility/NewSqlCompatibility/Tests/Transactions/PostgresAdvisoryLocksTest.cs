using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Transactions;

[SqlTest(SqlFeatureCategory.Transactions, "Test PostgreSQL advisory locks", DatabaseType.PostgreSql)]
public class PostgresAdvisoryLocksTest : SqlTest
{
    protected override void SetupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = @"CREATE TABLE advisory_test (
                            id SERIAL PRIMARY KEY,
                            resource_id INT,
                            status VARCHAR(50)
                        )";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO advisory_test (resource_id, status) VALUES (1, 'available'), (2, 'available'), (3, 'available')";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd1 = connection.CreateCommand();
        using DbCommand cmd2 = connectionSecond.CreateCommand();

        cmd1.CommandText = "SELECT pg_try_advisory_lock(12345)";
        object? lock1 = cmd1.ExecuteScalar();
        AssertTrue(Convert.ToBoolean(lock1!), "First connection should acquire advisory lock");

        cmd2.CommandText = "SELECT pg_try_advisory_lock(12345)";
        object? lock2 = cmd2.ExecuteScalar();
        AssertTrue(!Convert.ToBoolean(lock2!), "Second connection should fail to acquire same advisory lock");

        cmd1.CommandText = "UPDATE advisory_test SET status = 'processing' WHERE resource_id = 1";
        cmd1.ExecuteNonQuery();

        cmd1.CommandText = "SELECT pg_advisory_unlock(12345)";
        object? unlock1 = cmd1.ExecuteScalar();
        AssertTrue(Convert.ToBoolean(unlock1!), "Should successfully unlock advisory lock");

        cmd2.CommandText = "SELECT pg_try_advisory_lock(12345)";
        object? lock2_retry = cmd2.ExecuteScalar();
        AssertTrue(Convert.ToBoolean(lock2_retry!), "Second connection should now acquire lock after unlock");

        cmd2.CommandText = "SELECT pg_advisory_unlock(12345)";
        cmd2.ExecuteNonQuery();

        cmd1.CommandText = "BEGIN";
        cmd1.ExecuteNonQuery();

        cmd1.CommandText = "SELECT pg_advisory_xact_lock(54321)";
        cmd1.ExecuteNonQuery();

        cmd1.CommandText = "UPDATE advisory_test SET status = 'locked' WHERE resource_id = 2";
        cmd1.ExecuteNonQuery();

        cmd1.CommandText = "COMMIT";
        cmd1.ExecuteNonQuery();

        cmd1.CommandText = "SELECT pg_try_advisory_lock(54321)";
        object? lockAfterCommit = cmd1.ExecuteScalar();
        AssertTrue(Convert.ToBoolean(lockAfterCommit!), "Transaction-level advisory lock should be released after commit");

        cmd1.CommandText = "SELECT pg_advisory_unlock(54321)";
        cmd1.ExecuteNonQuery();

        cmd1.CommandText = "SELECT COUNT(*) FROM advisory_test WHERE status IN ('processing', 'locked')";
        object? count = cmd1.ExecuteScalar();
        AssertTrue(Convert.ToInt64(count!) >= 2, "Advisory lock operations should have succeeded");
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT pg_advisory_unlock_all()";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "DROP TABLE IF EXISTS advisory_test CASCADE";
        cmd.ExecuteNonQuery();
    }
}
