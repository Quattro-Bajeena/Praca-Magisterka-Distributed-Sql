using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Spatial;

[SqlTest(SqlFeatureCategory.Spatial, "Test PostgreSQL/PostGIS spatial relationships", DatabaseType.PostgreSql)]
public class PostgresPostGISSpatialRelationshipsTest : SqlTest
{
    protected override void SetupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        
        cmd.CommandText = "CREATE EXTENSION IF NOT EXISTS postgis  SCHEMA public";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"CREATE TABLE postgis_spatial_test (
                            id SERIAL PRIMARY KEY,
                            name VARCHAR(100),
                            geom GEOMETRY(GEOMETRY, 4326)
                        )";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO postgis_spatial_test (name, geom) VALUES ('BigSquare', ST_GeomFromText('POLYGON((0 0, 10 0, 10 10, 0 10, 0 0))', 4326))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO postgis_spatial_test (name, geom) VALUES ('SmallSquare', ST_GeomFromText('POLYGON((2 2, 4 2, 4 4, 2 4, 2 2))', 4326))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO postgis_spatial_test (name, geom) VALUES ('PointInside', ST_GeomFromText('POINT(3 3)', 4326))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO postgis_spatial_test (name, geom) VALUES ('PointOutside', ST_GeomFromText('POINT(20 20)', 4326))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO postgis_spatial_test (name, geom) VALUES ('Line1', ST_GeomFromText('LINESTRING(1 1, 5 5)', 4326))";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = @"SELECT ST_Contains(
            (SELECT geom FROM postgis_spatial_test WHERE name = 'BigSquare'),
            (SELECT geom FROM postgis_spatial_test WHERE name = 'SmallSquare')
        )";
        object? contains = cmd.ExecuteScalar();
        AssertTrue(Convert.ToBoolean(contains!), "BigSquare should contain SmallSquare");

        cmd.CommandText = @"SELECT ST_Within(
            (SELECT geom FROM postgis_spatial_test WHERE name = 'PointInside'),
            (SELECT geom FROM postgis_spatial_test WHERE name = 'BigSquare')
        )";
        object? within = cmd.ExecuteScalar();
        AssertTrue(Convert.ToBoolean(within!), "PointInside should be within BigSquare");

        cmd.CommandText = @"SELECT ST_Intersects(
            (SELECT geom FROM postgis_spatial_test WHERE name = 'Line1'),
            (SELECT geom FROM postgis_spatial_test WHERE name = 'SmallSquare')
        )";
        object? intersects = cmd.ExecuteScalar();
        AssertTrue(Convert.ToBoolean(intersects!), "Line1 should intersect SmallSquare");

        cmd.CommandText = @"SELECT ST_Disjoint(
            (SELECT geom FROM postgis_spatial_test WHERE name = 'PointOutside'),
            (SELECT geom FROM postgis_spatial_test WHERE name = 'BigSquare')
        )";
        object? disjoint = cmd.ExecuteScalar();
        AssertTrue(Convert.ToBoolean(disjoint!), "PointOutside should be disjoint from BigSquare");

        cmd.CommandText = @"SELECT ST_Touches(
            ST_GeomFromText('POLYGON((0 0, 1 0, 1 1, 0 1, 0 0))', 4326),
            ST_GeomFromText('POLYGON((1 0, 2 0, 2 1, 1 1, 1 0))', 4326)
        )";
        object? touches = cmd.ExecuteScalar();
        AssertTrue(Convert.ToBoolean(touches!), "Adjacent polygons should touch");

        cmd.CommandText = @"SELECT ST_Overlaps(
            ST_GeomFromText('POLYGON((0 0, 3 0, 3 3, 0 3, 0 0))', 4326),
            ST_GeomFromText('POLYGON((2 2, 5 2, 5 5, 2 5, 2 2))', 4326)
        )";
        object? overlaps = cmd.ExecuteScalar();
        AssertTrue(Convert.ToBoolean(overlaps!), "Overlapping polygons should return true");

        cmd.CommandText = @"SELECT ST_Equals(
            ST_GeomFromText('POINT(1 1)', 4326),
            ST_GeomFromText('POINT(1 1)', 4326)
        )";
        object? equals = cmd.ExecuteScalar();
        AssertTrue(Convert.ToBoolean(equals!), "Identical geometries should be equal");

        cmd.CommandText = @"SELECT COUNT(*) FROM postgis_spatial_test 
                           WHERE ST_DWithin(geom, ST_GeomFromText('POINT(3 3)', 4326), 5)";
        object? nearby = cmd.ExecuteScalar();
        AssertTrue(Convert.ToInt64(nearby!) >= 3, "Should find geometries within distance");
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS postgis_spatial_test CASCADE";
        cmd.ExecuteNonQuery();
    }
}
