using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.TableDefinition;

[SqlTest(SqlFeatureCategory.DDL, "Test CREATE SEQUENCE with NEXTVAL, CURRVAL, SETVAL and pg_sequences catalog", DatabaseType.PostgreSql)]
public class PostgresSequenceTest : SqlTest
{
    protected override void SetupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = @"CREATE SEQUENCE counter
            START WITH 10
            INCREMENT BY 5
            MINVALUE 10
            MAXVALUE 10000
            NO CYCLE";
        cmd.ExecuteNonQuery();

        // Sequence used as a column DEFAULT — inserts automatically advance the sequence
        cmd.CommandText = @"CREATE TABLE seq_items (
            id   INT  NOT NULL DEFAULT NEXTVAL('counter'),
            name TEXT NOT NULL
        )";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        // NEXTVAL advances the sequence and returns the new value
        cmd.CommandText = "SELECT NEXTVAL('counter')";
        long first = Convert.ToInt64(cmd.ExecuteScalar()!);
        AssertEqual(10L, first, "First NEXTVAL should return the START value 10");

        cmd.CommandText = "SELECT NEXTVAL('counter')";
        long second = Convert.ToInt64(cmd.ExecuteScalar()!);
        AssertEqual(15L, second, "Second NEXTVAL should return 15 (10 + increment 5)");

        cmd.CommandText = "SELECT NEXTVAL('counter')";
        long third = Convert.ToInt64(cmd.ExecuteScalar()!);
        AssertEqual(20L, third, "Third NEXTVAL should return 20");

        // CURRVAL returns the last value returned by NEXTVAL in the current session
        cmd.CommandText = "SELECT CURRVAL('counter')";
        long curr = Convert.ToInt64(cmd.ExecuteScalar()!);
        AssertEqual(20L, curr, "CURRVAL should equal the last NEXTVAL result");

        // SETVAL(seq, value, false): marks the value as NOT YET returned,
        // so the very next NEXTVAL will emit exactly `value` (not value + increment)
        cmd.CommandText = "SELECT SETVAL('counter', 100, false)";
        cmd.ExecuteScalar();

        cmd.CommandText = "SELECT NEXTVAL('counter')";
        long afterSet = Convert.ToInt64(cmd.ExecuteScalar()!);
        AssertEqual(100L, afterSet, "NEXTVAL after SETVAL(100, false) should return exactly 100");

        cmd.CommandText = "SELECT NEXTVAL('counter')";
        long resume = Convert.ToInt64(cmd.ExecuteScalar()!);
        AssertEqual(105L, resume, "Sequence should resume with normal increment after SETVAL: 105");

        // DEFAULT NEXTVAL: inserting without specifying id triggers sequence advancement
        cmd.CommandText = "INSERT INTO seq_items (name) VALUES ('alpha')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO seq_items (name) VALUES ('beta')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT id FROM seq_items WHERE name = 'alpha'";
        long alphaId = Convert.ToInt64(cmd.ExecuteScalar()!);

        cmd.CommandText = "SELECT id FROM seq_items WHERE name = 'beta'";
        long betaId = Convert.ToInt64(cmd.ExecuteScalar()!);

        AssertEqual(5L, betaId - alphaId, "Consecutive inserts should get IDs that differ by the sequence increment (5)");

        // pg_sequences exposes the sequence configuration as a catalog view
        cmd.CommandText = "SELECT increment_by FROM pg_sequences WHERE sequencename = 'counter'";
        long incrementBy = Convert.ToInt64(cmd.ExecuteScalar()!);
        AssertEqual(5L, incrementBy, "pg_sequences.increment_by should be 5");

        cmd.CommandText = "SELECT start_value FROM pg_sequences WHERE sequencename = 'counter'";
        long startValue = Convert.ToInt64(cmd.ExecuteScalar()!);
        AssertEqual(10L, startValue, "pg_sequences.start_value should be 10");

        cmd.CommandText = "SELECT cycle FROM pg_sequences WHERE sequencename = 'counter'";
        bool cycle = Convert.ToBoolean(cmd.ExecuteScalar()!);
        AssertEqual(false, cycle, "pg_sequences.cycle should be false (NO CYCLE)");
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "DROP TABLE IF EXISTS seq_items CASCADE";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());

        cmd.CommandText = "DROP SEQUENCE IF EXISTS counter CASCADE";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());
    }
}
