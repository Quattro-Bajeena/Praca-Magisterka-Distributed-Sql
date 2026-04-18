using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Upsert;

[SqlTest(SqlFeatureCategory.Upsert, "Test PostgreSQL ON CONFLICT with RETURNING clause", DatabaseType.PostgreSql)]
public class PostgresOnConflictReturningTest : SqlTest
{
    protected override void SetupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = @"CREATE TABLE events_pg (
                            id SERIAL PRIMARY KEY,
                            event_name VARCHAR(100) UNIQUE,
                            event_count INT DEFAULT 1
                        )";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        // Insert new row — RETURNING should give back the inserted row
        cmd.CommandText = @"INSERT INTO events_pg (event_name, event_count)
                           VALUES ('user_login', 1)
                           ON CONFLICT (event_name) DO UPDATE
                           SET event_count = events_pg.event_count + 1
                           RETURNING event_name, event_count";

        using (DbDataReader reader = cmd.ExecuteReader())
        {
            AssertTrue(reader.Read(), "RETURNING should give back a row");
            AssertEqual("user_login", reader.GetString(0), "Event name should match");
            AssertEqual(1, reader.GetInt32(1), "First occurrence should have count 1");
        }

        // Update on conflict — RETURNING should give back the updated row
        cmd.CommandText = @"INSERT INTO events_pg (event_name, event_count)
                           VALUES ('user_login', 1)
                           ON CONFLICT (event_name) DO UPDATE
                           SET event_count = events_pg.event_count + 1
                           RETURNING event_count";

        using (DbDataReader reader = cmd.ExecuteReader())
        {
            AssertTrue(reader.Read(), "RETURNING should give back the updated row");
            AssertEqual(2, reader.GetInt32(0), "Second occurrence should have count 2");
        }

        // DO NOTHING — RETURNING should be empty on conflict
        cmd.CommandText = @"INSERT INTO events_pg (event_name)
                           VALUES ('user_login')
                           ON CONFLICT (event_name) DO NOTHING
                           RETURNING *";

        using (DbDataReader reader = cmd.ExecuteReader())
        {
            AssertTrue(!reader.Read(), "DO NOTHING should return no rows when skipping");
        }
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS events_pg CASCADE";
        cmd.ExecuteNonQuery();
    }
}
