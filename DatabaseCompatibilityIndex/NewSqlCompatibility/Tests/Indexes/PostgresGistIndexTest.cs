using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Indexes;

[SqlTest(SqlFeatureCategory.Indexes, "Test PostgreSQL GiST index on tsrange type for range overlap queries", DatabaseType.PostgreSql)]
public class PostgresGistIndexTest : SqlTest
{
    protected override void SetupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = @"CREATE TABLE reservations_gist (
            id SERIAL PRIMARY KEY,
            room_id INT NOT NULL,
            during TSRANGE NOT NULL
        )";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "CREATE INDEX idx_reservations_during ON reservations_gist USING GIST (during)";
        cmd.ExecuteNonQuery();

        // 10 reservations spread across the day for 5 rooms:
        //  Room 1: [08:00-10:00)  [12:00-14:00)
        //  Room 2: [09:00-11:00)  [14:00-16:00)
        //  Room 3: [07:00-09:00)  [11:00-13:00)
        //  Room 4: [08:30-09:30)  [15:00-17:00)
        //  Room 5: [06:00-08:00)  [10:00-12:00)
        string[] insertValues =
        [
            "(1, '[2024-01-15 08:00, 2024-01-15 10:00)')",
            "(1, '[2024-01-15 12:00, 2024-01-15 14:00)')",
            "(2, '[2024-01-15 09:00, 2024-01-15 11:00)')",
            "(2, '[2024-01-15 14:00, 2024-01-15 16:00)')",
            "(3, '[2024-01-15 07:00, 2024-01-15 09:00)')",
            "(3, '[2024-01-15 11:00, 2024-01-15 13:00)')",
            "(4, '[2024-01-15 08:30, 2024-01-15 09:30)')",
            "(4, '[2024-01-15 15:00, 2024-01-15 17:00)')",
            "(5, '[2024-01-15 06:00, 2024-01-15 08:00)')",
            "(5, '[2024-01-15 10:00, 2024-01-15 12:00)')",
        ];

        foreach (string values in insertValues)
        {
            cmd.CommandText = $"INSERT INTO reservations_gist (room_id, during) VALUES {values}";
            cmd.ExecuteNonQuery();
        }
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        // Confirm the GiST index was registered in the catalog
        cmd.CommandText = @"SELECT COUNT(*) FROM pg_indexes
                            WHERE tablename = 'reservations_gist'
                              AND indexname = 'idx_reservations_during'";
        object? indexCount = cmd.ExecuteScalar();
        AssertEqual(1L, Convert.ToInt64(indexCount!), "GiST index should be present in pg_indexes");

        // Overlap operator &&
        // [09:30, 10:30) overlaps: Room1[08:00-10:00), Room2[09:00-11:00), Room5[10:00-12:00)
        cmd.CommandText = "SELECT COUNT(*) FROM reservations_gist WHERE during && '[2024-01-15 09:30, 2024-01-15 10:30)'::tsrange";
        object? count = cmd.ExecuteScalar();
        AssertEqual(3L, Convert.ToInt64(count!), "Should find 3 reservations overlapping [09:30, 10:30)");

        // Containment operator @> (range contains the argument range)
        // [12:30, 12:31) fits inside: Room1[12:00-14:00), Room3[11:00-13:00)
        cmd.CommandText = "SELECT COUNT(*) FROM reservations_gist WHERE during @> '[2024-01-15 12:30, 2024-01-15 12:31)'::tsrange";
        count = cmd.ExecuteScalar();
        AssertEqual(2L, Convert.ToInt64(count!), "Should find 2 reservations containing the [12:30, 12:31) window");

        // Contained-in operator <@ (row range is fully inside the argument range)
        // Fully inside [07:00, 12:00): Room1[08:00-10:00), Room2[09:00-11:00),
        //   Room3[07:00-09:00), Room4[08:30-09:30), Room5[10:00-12:00)
        cmd.CommandText = "SELECT COUNT(*) FROM reservations_gist WHERE during <@ '[2024-01-15 07:00, 2024-01-15 12:00)'::tsrange";
        count = cmd.ExecuteScalar();
        AssertEqual(5L, Convert.ToInt64(count!), "Should find 5 reservations fully within [07:00, 12:00)");

        // Window with no matches at all
        cmd.CommandText = "SELECT COUNT(*) FROM reservations_gist WHERE during && '[2024-01-15 20:00, 2024-01-15 21:00)'::tsrange";
        count = cmd.ExecuteScalar();
        AssertEqual(0L, Convert.ToInt64(count!), "No reservations should overlap with [20:00, 21:00)");

        // EXPLAIN must produce a plan (index is available for the planner to choose)
        cmd.CommandText = "EXPLAIN SELECT * FROM reservations_gist WHERE during && '[2024-01-15 09:30, 2024-01-15 10:30)'::tsrange";
        bool planGenerated = false;
        using (DbDataReader reader = cmd.ExecuteReader())
        {
            planGenerated = reader.HasRows;
        }
        AssertTrue(planGenerated, "EXPLAIN should produce a query plan for a GiST-indexed range query");

        // Insert a new reservation and verify it is immediately queryable through the index
        // Room6: [09:00, 10:30) — overlaps with [09:30, 10:30), raising the expected count to 4
        cmd.CommandText = "INSERT INTO reservations_gist (room_id, during) VALUES (6, '[2024-01-15 09:00, 2024-01-15 10:30)')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM reservations_gist WHERE during && '[2024-01-15 09:30, 2024-01-15 10:30)'::tsrange";
        count = cmd.ExecuteScalar();
        AssertEqual(4L, Convert.ToInt64(count!), "Newly inserted reservation should be indexed and visible in overlap query");
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS reservations_gist CASCADE";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());
    }
}
