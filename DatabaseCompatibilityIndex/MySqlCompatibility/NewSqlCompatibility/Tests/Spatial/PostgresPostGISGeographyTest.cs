using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Spatial;

[SqlTest(SqlFeatureCategory.Spatial, "Test PostgreSQL/PostGIS Geography type and transformations", DatabaseType.PostgreSql)]
public class PostgresPostGISGeographyTest : SqlTest
{
    protected override void SetupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "CREATE EXTENSION IF NOT EXISTS postgis  SCHEMA public";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"CREATE TABLE postgis_geography_test (
                            id SERIAL PRIMARY KEY,
                            name VARCHAR(100),
                            location GEOGRAPHY(POINT, 4326)
                        )";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO postgis_geography_test (name, location) VALUES ('San Francisco', ST_GeogFromText('POINT(-122.4194 37.7749)'))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO postgis_geography_test (name, location) VALUES ('New York', ST_GeogFromText('POINT(-74.0060 40.7128)'))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO postgis_geography_test (name, location) VALUES ('London', ST_GeogFromText('POINT(-0.1278 51.5074)'))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"CREATE TABLE postgis_transform_test (
                            id SERIAL PRIMARY KEY,
                            name VARCHAR(100),
                            geom_4326 GEOMETRY(POINT, 4326),
                            geom_3857 GEOMETRY(POINT, 3857)
                        )";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = @"SELECT ST_Distance(
            (SELECT location FROM postgis_geography_test WHERE name = 'San Francisco'),
            (SELECT location FROM postgis_geography_test WHERE name = 'New York')
        )";
        object? distance = cmd.ExecuteScalar();
        double distanceMeters = Convert.ToDouble(distance!);
        AssertTrue(distanceMeters > 4000000 && distanceMeters < 5000000, "SF to NYC should be ~4100km");

        cmd.CommandText = @"SELECT ST_Distance(
            (SELECT location FROM postgis_geography_test WHERE name = 'San Francisco'),
            (SELECT location FROM postgis_geography_test WHERE name = 'London')
        )";
        object? distanceToLondon = cmd.ExecuteScalar();
        double distanceMetersLondon = Convert.ToDouble(distanceToLondon!);
        AssertTrue(distanceMetersLondon > 8000000, "SF to London should be > 8000km");

        cmd.CommandText = @"SELECT ST_Area(
            ST_GeogFromText('POLYGON((-122.5 37.7, -122.4 37.7, -122.4 37.8, -122.5 37.8, -122.5 37.7))')
        )";
        object? area = cmd.ExecuteScalar();
        AssertTrue(Convert.ToDouble(area!) > 0, "Geography area should be in square meters");

        cmd.CommandText = @"SELECT COUNT(*) FROM postgis_geography_test
                           WHERE ST_DWithin(location, ST_GeogFromText('POINT(-122.4 37.8)'), 100000)";
        object? nearby = cmd.ExecuteScalar();
        AssertEqual(1L, Convert.ToInt64(nearby!), "Should find 1 location within 100km of point");

        cmd.CommandText = "INSERT INTO postgis_transform_test (name, geom_4326) VALUES ('TestPoint', ST_SetSRID(ST_MakePoint(-122.4194, 37.7749), 4326))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "UPDATE postgis_transform_test SET geom_3857 = ST_Transform(geom_4326, 3857) WHERE name = 'TestPoint'";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT ST_SRID(geom_3857) FROM postgis_transform_test WHERE name = 'TestPoint'";
        object? srid = cmd.ExecuteScalar();
        AssertEqual(3857, Convert.ToInt32(srid!), "Transformed geometry should have SRID 3857 (Web Mercator)");

        cmd.CommandText = "SELECT ST_X(geom_3857), ST_Y(geom_3857) FROM postgis_transform_test WHERE name = 'TestPoint'";
        using (DbDataReader reader = cmd.ExecuteReader())
        {
            AssertTrue(reader.Read(), "Should have transformed coordinates");
            double x = reader.GetDouble(0);
            double y = reader.GetDouble(1);
            AssertTrue(Math.Abs(x) > 13000000, "Web Mercator X should be large");
            AssertTrue(Math.Abs(y) > 4000000, "Web Mercator Y should be large");
        }

        cmd.CommandText = @"SELECT ST_AsText(ST_Transform(
            ST_SetSRID(ST_MakePoint(-122.4194, 37.7749), 4326),
            4326
        ))";
        object? transformed = cmd.ExecuteScalar();
        AssertTrue(transformed?.ToString()?.Contains("POINT") == true, "Transform to same SRID should work");

        cmd.CommandText = "SELECT ST_AsGeoJSON(location) FROM postgis_geography_test WHERE name = 'San Francisco'";
        object? geoJson = cmd.ExecuteScalar();
        AssertTrue(geoJson?.ToString()?.Contains("coordinates") == true, "Should return valid GeoJSON");

        cmd.CommandText = "SELECT ST_AsKML(location) FROM postgis_geography_test WHERE name = 'New York'";
        object? kml = cmd.ExecuteScalar();
        AssertTrue(kml?.ToString()?.Contains("coordinates") == true, "Should return valid KML");
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS postgis_geography_test CASCADE";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "DROP TABLE IF EXISTS postgis_transform_test CASCADE";
        cmd.ExecuteNonQuery();
    }
}
