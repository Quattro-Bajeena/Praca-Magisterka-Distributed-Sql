using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Advanced;

[SqlTest(SqlFeatureCategory.Misc, "Test EVENT ")]
public class EventTest : SqlTest
{
    protected override void SetupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "CREATE TABLE event_log (id INT AUTO_INCREMENT PRIMARY KEY, log_time TIMESTAMP DEFAULT CURRENT_TIMESTAMP)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SET GLOBAL event_scheduler = ON";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"CREATE EVENT test_event
            ON SCHEDULE AT CURRENT_TIMESTAMP + INTERVAL 1 SECOND
            DO INSERT INTO event_log (id) VALUES (NULL)";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        Thread.Sleep(2000);

        cmd.CommandText = "SELECT COUNT(*) FROM event_log";
        object? count = cmd.ExecuteScalar();
        AssertEqual(1L, Convert.ToInt64(count!), "Event should have inserted 1 row");
    }

    protected override void CleanupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP EVENT IF EXISTS test_event";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "DROP TABLE IF EXISTS event_log";
        cmd.ExecuteNonQuery();
    }

    protected override void SetupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "CREATE TABLE event_log (id SERIAL PRIMARY KEY, log_time TIMESTAMP DEFAULT CURRENT_TIMESTAMP)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"CREATE OR REPLACE FUNCTION insert_event_log() RETURNS void AS $$
            BEGIN
                INSERT INTO event_log (log_time) VALUES (CURRENT_TIMESTAMP);
            END;
            $$ LANGUAGE plpgsql";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT pg_sleep(0.1)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT insert_event_log()";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "SELECT COUNT(*) FROM event_log";
        object? count = cmd.ExecuteScalar();
        AssertEqual(1L, Convert.ToInt64(count!), "Function should have inserted 1 row");
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP FUNCTION IF EXISTS insert_event_log()";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "DROP TABLE IF EXISTS event_log";
        cmd.ExecuteNonQuery();
    }
}