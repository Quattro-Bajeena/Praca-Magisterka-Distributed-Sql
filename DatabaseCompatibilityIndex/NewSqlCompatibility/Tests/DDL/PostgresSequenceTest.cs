using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.TableDefinition;

[SqlTest(SqlFeatureCategory.DDL, "Test CREATE SEQUENCE with NEXTVAL and CURRVAL", DatabaseType.PostgreSql)]
public class PostgresSequenceTest : SqlTest
{
    protected override void SetupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = @"CREATE SEQUENCE counter
            START WITH 10
            INCREMENT BY 5";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "SELECT NEXTVAL('counter')";
        long first = Convert.ToInt64(cmd.ExecuteScalar()!);
        AssertEqual(10L, first, "First NEXTVAL should return the START value 10");

        cmd.CommandText = "SELECT NEXTVAL('counter')";
        long second = Convert.ToInt64(cmd.ExecuteScalar()!);
        AssertEqual(15L, second, "Second NEXTVAL should return 15 (10 + increment 5)");

        cmd.CommandText = "SELECT CURRVAL('counter')";
        long curr = Convert.ToInt64(cmd.ExecuteScalar()!);
        AssertEqual(15L, curr, "CURRVAL should equal the last NEXTVAL result");
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP SEQUENCE IF EXISTS counter CASCADE";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());
    }
}
