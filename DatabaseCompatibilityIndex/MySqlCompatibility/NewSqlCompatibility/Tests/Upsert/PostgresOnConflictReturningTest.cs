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
                            event_count INT DEFAULT 1,
                            last_occurred TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                        )";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = @"INSERT INTO events_pg (event_name, event_count) 
                           VALUES ('user_login', 1)
                           ON CONFLICT (event_name) DO UPDATE 
                           SET event_count = events_pg.event_count + 1,
                               last_occurred = CURRENT_TIMESTAMP
                           RETURNING id, event_name, event_count";
        
        using (DbDataReader reader = cmd.ExecuteReader())
        {
            AssertTrue(reader.Read(), "Should return inserted row");
            int id = reader.GetInt32(0);
            string eventName = reader.GetString(1);
            int count = reader.GetInt32(2);
            
            AssertTrue(id > 0, "Should have valid ID");
            AssertEqual("user_login", eventName, "Event name should match");
            AssertEqual(1, count, "First occurrence should have count 1");
        }

        cmd.CommandText = @"INSERT INTO events_pg (event_name, event_count) 
                           VALUES ('user_login', 1)
                           ON CONFLICT (event_name) DO UPDATE 
                           SET event_count = events_pg.event_count + 1,
                               last_occurred = CURRENT_TIMESTAMP
                           RETURNING id, event_name, event_count, last_occurred";
        
        using (DbDataReader reader = cmd.ExecuteReader())
        {
            AssertTrue(reader.Read(), "Should return updated row");
            int count = reader.GetInt32(2);
            AssertEqual(2, count, "Second occurrence should have count 2");
        }

        cmd.CommandText = @"INSERT INTO events_pg (event_name, event_count) 
                           VALUES ('user_logout', 1), ('user_login', 1), ('page_view', 1)
                           ON CONFLICT (event_name) DO UPDATE 
                           SET event_count = events_pg.event_count + EXCLUDED.event_count
                           RETURNING event_name, event_count";
        
        int returnedRows = 0;
        using (DbDataReader reader = cmd.ExecuteReader())
        {
            while (reader.Read())
            {
                returnedRows++;
                string eventName = reader.GetString(0);
                int count = reader.GetInt32(1);
                
                if (eventName == "user_login")
                {
                    AssertEqual(3, count, "user_login should now have count 3");
                }
            }
        }
        AssertEqual(3, returnedRows, "Should return 3 rows (1 update + 2 inserts)");

        cmd.CommandText = @"INSERT INTO events_pg (event_name, event_count) 
                           VALUES ('error_500', 1)
                           ON CONFLICT (event_name) DO NOTHING
                           RETURNING *";
        
        using (DbDataReader reader = cmd.ExecuteReader())
        {
            AssertTrue(reader.Read(), "Should return inserted row with DO NOTHING");
        }

        cmd.CommandText = @"INSERT INTO events_pg (event_name, event_count) 
                           VALUES ('error_500', 1)
                           ON CONFLICT (event_name) DO NOTHING
                           RETURNING *";
        
        using (DbDataReader reader = cmd.ExecuteReader())
        {
            AssertTrue(!reader.Read(), "Should not return anything when DO NOTHING skips insert");
        }
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS events_pg CASCADE";
        cmd.ExecuteNonQuery();
    }
}
