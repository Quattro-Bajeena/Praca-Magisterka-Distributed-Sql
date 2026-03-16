using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Spatial;

[SqlTest(SqlFeatureCategory.Spatial, "Test PostgreSQL/PostGIS spatial indexes (GIST)", DatabaseType.PostgreSql)]
public class PostgresPostGISSpatialIndexTest : SqlTest
{
    protected override void SetupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        
        cmd.CommandText = "CREATE EXTENSION IF NOT EXISTS postgis  SCHEMA public";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"CREATE TABLE postgis_index_test (
                            id SERIAL PRIMARY KEY,
                            name VARCHAR(100),
                            location GEOMETRY(POINT, 4326)
                        )";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "CREATE INDEX idx_postgis_location ON postgis_index_test USING GIST (location)";
        cmd.ExecuteNonQuery();

        for (int i = 1; i <= 100; i++)
        {
            double lon = -122.0 + (i * 0.01);
            double lat = 37.0 + (i * 0.01);
            cmd.CommandText = $"INSERT INTO postgis_index_test (name, location) VALUES ('Point{i}', ST_SetSRID(ST_MakePoint({lon}, {lat}), 4326))";
            cmd.ExecuteNonQuery();
        }
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = @"SELECT COUNT(*) FROM postgis_index_test 
                           WHERE ST_DWithin(location, ST_SetSRID(ST_MakePoint(-121.5, 37.5), 4326), 0.1)";
        object? count = cmd.ExecuteScalar();
        AssertTrue(Convert.ToInt64(count!) > 0, "Should find points within distance using spatial index");

        cmd.CommandText = @"SELECT name FROM postgis_index_test 
                           WHERE ST_Intersects(location, ST_MakeEnvelope(-122.0, 37.0, -121.0, 38.0, 4326))
                           ORDER BY name LIMIT 1";
        object? name = cmd.ExecuteScalar();
        AssertTrue(name != null, "Should find points in bounding box using spatial index");

        cmd.CommandText = @"SELECT COUNT(*) FROM postgis_index_test 
                           WHERE location && ST_MakeEnvelope(-122.0, 37.0, -121.0, 38.0, 4326)";
        object? boundingBoxCount = cmd.ExecuteScalar();
        AssertTrue(Convert.ToInt64(boundingBoxCount!) > 0, "Bounding box operator && should work with GIST index");

        cmd.CommandText = @"SELECT name, ST_Distance(location, ST_SetSRID(ST_MakePoint(-121.5, 37.5), 4326)) as dist
                           FROM postgis_index_test
                           ORDER BY location <-> ST_SetSRID(ST_MakePoint(-121.5, 37.5), 4326)
                           LIMIT 5";
        int nearestCount = 0;
        using (DbDataReader reader = cmd.ExecuteReader())
        {
            while (reader.Read())
            {
                nearestCount++;
            }
        }
        AssertEqual(5, nearestCount, "Should find 5 nearest points using KNN operator <->");

        cmd.CommandText = @"SELECT indexname FROM pg_indexes 
                           WHERE tablename = 'postgis_index_test' AND indexname = 'idx_postgis_location'";
        object? indexExists = cmd.ExecuteScalar();
        AssertTrue(indexExists != null, "GIST index should exist");

        cmd.CommandText = @"EXPLAIN SELECT COUNT(*) FROM postgis_index_test 
                           WHERE ST_DWithin(location, ST_SetSRID(ST_MakePoint(-121.5, 37.5), 4326), 0.1)";
        bool hasIndexScan = false;
        using (DbDataReader reader = cmd.ExecuteReader())
        {
            while (reader.Read())
            {
                string? plan = reader.GetString(0);
                if (plan != null && plan.Contains("Index"))
                {
                    hasIndexScan = true;
                }
            }
        }
        AssertTrue(hasIndexScan, "Query plan should use spatial index");
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS postgis_index_test CASCADE";
        cmd.ExecuteNonQuery();
    }
}
