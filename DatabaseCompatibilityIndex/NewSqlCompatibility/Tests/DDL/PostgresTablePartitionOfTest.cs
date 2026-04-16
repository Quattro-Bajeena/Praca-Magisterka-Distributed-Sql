using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.TableDefinition;

[SqlTest(SqlFeatureCategory.DDL, "Test declarative table partitioning with PARTITION OF", DatabaseType.PostgreSql)]
public class PostgresTablePartitionOfTest : SqlTest
{
    protected override void SetupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = @"CREATE TABLE event_log (
            id         SERIAL,
            event_name TEXT NOT NULL,
            event_date DATE NOT NULL
        ) PARTITION BY RANGE (event_date)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "CREATE TABLE event_log_q1 PARTITION OF event_log FOR VALUES FROM ('2024-01-01') TO ('2024-04-01')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "CREATE TABLE event_log_q2 PARTITION OF event_log FOR VALUES FROM ('2024-04-01') TO ('2024-07-01')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "CREATE TABLE event_log_q3 PARTITION OF event_log FOR VALUES FROM ('2024-07-01') TO ('2024-10-01')";
        cmd.ExecuteNonQuery();

        string[] inserts =
        [
            "('q1-event-a', '2024-01-15')",
            "('q1-event-b', '2024-03-20')",
            "('q2-event-a', '2024-05-10')",
            "('q3-event-a', '2024-08-01')",
        ];

        foreach (string values in inserts)
        {
            cmd.CommandText = $"INSERT INTO event_log (event_name, event_date) VALUES {values}";
            cmd.ExecuteNonQuery();
        }
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "SELECT COUNT(*) FROM event_log";
        long total = Convert.ToInt64(cmd.ExecuteScalar()!);
        AssertEqual(4L, total, "Parent table should expose all 4 rows across partitions");

        cmd.CommandText = "SELECT COUNT(*) FROM event_log_q1";
        long q1Count = Convert.ToInt64(cmd.ExecuteScalar()!);
        AssertEqual(2L, q1Count, "Q1 partition should contain 2 rows");

        cmd.CommandText = "SELECT COUNT(*) FROM event_log_q2";
        long q2Count = Convert.ToInt64(cmd.ExecuteScalar()!);
        AssertEqual(1L, q2Count, "Q2 partition should contain 1 row");

        cmd.CommandText = "SELECT COUNT(*) FROM event_log_q3";
        long q3Count = Convert.ToInt64(cmd.ExecuteScalar()!);
        AssertEqual(1L, q3Count, "Q3 partition should contain 1 row");

        // pg_inherits records the three PARTITION OF children
        cmd.CommandText = "SELECT COUNT(*) FROM pg_inherits WHERE inhparent = 'event_log'::regclass";
        long partitionCount = Convert.ToInt64(cmd.ExecuteScalar()!);
        AssertEqual(3L, partitionCount, "pg_inherits should show 3 partitions for event_log");
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS event_log CASCADE";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());
    }
}
