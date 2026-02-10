using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Spatial;

[SqlTest(SqlFeatureCategory.Spatial, "Test PostgreSQL/PostGIS measurements and calculations", DatabaseType.PostgreSql)]
public class PostgresPostGISMeasurementsTest : SqlTest
{
    protected override void SetupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        
        cmd.CommandText = "CREATE EXTENSION IF NOT EXISTS postgis";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"CREATE TABLE postgis_measurements (
                            id SERIAL PRIMARY KEY,
                            name VARCHAR(100),
                            geom GEOMETRY
                        )";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "SELECT ST_Distance(ST_GeomFromText('POINT(0 0)', 4326), ST_GeomFromText('POINT(3 4)', 4326))";
        object? distance = cmd.ExecuteScalar();
        AssertEqual(5.0, Convert.ToDouble(distance!), "Distance should be 5 (3-4-5 triangle)");

        cmd.CommandText = "SELECT ST_Length(ST_GeomFromText('LINESTRING(0 0, 3 4)', 4326))";
        object? length = cmd.ExecuteScalar();
        AssertEqual(5.0, Convert.ToDouble(length!), "Length should be 5");

        cmd.CommandText = "SELECT ST_Area(ST_GeomFromText('POLYGON((0 0, 4 0, 4 3, 0 3, 0 0))', 4326))";
        object? area = cmd.ExecuteScalar();
        AssertEqual(12.0, Convert.ToDouble(area!), "Area should be 12 (4x3 rectangle)");

        cmd.CommandText = "SELECT ST_Perimeter(ST_GeomFromText('POLYGON((0 0, 4 0, 4 3, 0 3, 0 0))', 4326))";
        object? perimeter = cmd.ExecuteScalar();
        AssertEqual(14.0, Convert.ToDouble(perimeter!), "Perimeter should be 14");

        cmd.CommandText = "SELECT ST_NumPoints(ST_GeomFromText('LINESTRING(0 0, 1 1, 2 2, 3 3)', 4326))";
        object? numPoints = cmd.ExecuteScalar();
        AssertEqual(4, Convert.ToInt32(numPoints!), "Should have 4 points");

        cmd.CommandText = "SELECT ST_NPoints(ST_GeomFromText('POLYGON((0 0, 4 0, 4 4, 0 4, 0 0))', 4326))";
        object? nPoints = cmd.ExecuteScalar();
        AssertEqual(5, Convert.ToInt32(nPoints!), "Polygon should have 5 points (including closing point)");

        cmd.CommandText = "SELECT ST_AsText(ST_Centroid(ST_GeomFromText('POLYGON((0 0, 4 0, 4 4, 0 4, 0 0))', 4326)))";
        object? centroid = cmd.ExecuteScalar();
        AssertTrue(centroid?.ToString()?.Contains("POINT(2 2)") == true, "Centroid should be at (2, 2)");

        cmd.CommandText = "SELECT ST_AsText(ST_PointOnSurface(ST_GeomFromText('POLYGON((0 0, 4 0, 4 4, 0 4, 0 0))', 4326)))";
        object? pointOnSurface = cmd.ExecuteScalar();
        AssertTrue(pointOnSurface?.ToString()?.Contains("POINT") == true, "Should return a point on surface");

        cmd.CommandText = "SELECT ST_AsText(ST_Envelope(ST_GeomFromText('LINESTRING(0 0, 3 4)', 4326)))";
        object? envelope = cmd.ExecuteScalar();
        AssertTrue(envelope?.ToString()?.Contains("POLYGON") == true, "Envelope should be a polygon");

        cmd.CommandText = "SELECT ST_AsText(ST_ConvexHull(ST_GeomFromText('MULTIPOINT(0 0, 1 0, 1 1, 0 1, 0.5 0.5)', 4326)))";
        object? convexHull = cmd.ExecuteScalar();
        AssertTrue(convexHull?.ToString()?.Contains("POLYGON") == true, "Convex hull should be a polygon");

        cmd.CommandText = @"SELECT ST_DistanceSphere(
            ST_GeomFromText('POINT(-122.4194 37.7749)', 4326),
            ST_GeomFromText('POINT(-74.0060 40.7128)', 4326)
        )";
        object? sphereDistance = cmd.ExecuteScalar();
        AssertTrue(Convert.ToDouble(sphereDistance!) > 4000000, "SF to NYC should be > 4000km");
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS postgis_measurements CASCADE";
        cmd.ExecuteNonQuery();
    }
}
