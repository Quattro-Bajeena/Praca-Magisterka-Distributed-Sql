using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Transactions;

[SqlTest(SqlFeatureCategory.Transactions, "Test PostgreSQL advisory locks", DatabaseType.PostgreSql)]
public class PostgresAdvisoryLocksTest : SqlTest
{
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

        cmd1.CommandText = "SELECT pg_advisory_unlock(12345)";
        object? unlock1 = cmd1.ExecuteScalar();
        AssertTrue(Convert.ToBoolean(unlock1!), "Should successfully unlock advisory lock");

        cmd2.CommandText = "SELECT pg_try_advisory_lock(12345)";
        object? lock2_retry = cmd2.ExecuteScalar();
        AssertTrue(Convert.ToBoolean(lock2_retry!), "Second connection should now acquire lock after unlock");

        cmd2.CommandText = "SELECT pg_advisory_unlock(12345)";
        cmd2.ExecuteNonQuery();
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT pg_advisory_unlock_all()";
        cmd.ExecuteNonQuery();
    }
}
