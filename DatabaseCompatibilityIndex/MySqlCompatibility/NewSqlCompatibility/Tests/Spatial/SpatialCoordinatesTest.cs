using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Spatial;

[SqlTest(SqlFeatureCategory.Misc, "Test SPATIAL coordinate functions (ST_X, ST_Y, ST_SRID, ST_Transform) - unsupported in TiDB")]
public class SpatialCoordinatesTest : SqlTest
{
    protected override void SetupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "CREATE TABLE spatial_coords (id INT PRIMARY KEY, location POINT)";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "SELECT ST_X(ST_GeomFromText('POINT(42.5 -71.3)'))";
        object? x = cmd.ExecuteScalar();
        AssertEqual(42.5, Convert.ToDouble(x!), "X coordinate should be 42.5");

        cmd.CommandText = "SELECT ST_Y(ST_GeomFromText('POINT(42.5 -71.3)'))";
        object? y = cmd.ExecuteScalar();
        AssertEqual(-71.3, Convert.ToDouble(y!), "Y coordinate should be -71.3");

        cmd.CommandText = "SELECT ST_Latitude(ST_GeomFromText('POINT(42.5 -71.3)', 4326))";
        object? lat = cmd.ExecuteScalar();
        AssertEqual(42.5, Convert.ToDouble(lat!), "Latitude should be 42.5");

        cmd.CommandText = "SELECT ST_Longitude(ST_GeomFromText('POINT(42.5 -71.3)', 4326))";
        object? lon = cmd.ExecuteScalar();
        AssertEqual(-71.3, Convert.ToDouble(lon!), "Longitude should be -71.3");

        cmd.CommandText = "SELECT ST_SRID(ST_GeomFromText('POINT(1 1)', 4326))";
        object? srid = cmd.ExecuteScalar();
        AssertEqual(4326, Convert.ToInt32(srid!), "SRID should be 4326 (WGS 84)");

        cmd.CommandText = "SELECT ST_SRID(ST_SRID(ST_GeomFromText('POINT(1 1)'), 4326))";
        srid = cmd.ExecuteScalar();
        AssertEqual(4326, Convert.ToInt32(srid!), "SRID should be set to 4326");

        cmd.CommandText = "SELECT ST_AsText(ST_SwapXY(ST_GeomFromText('POINT(1 2)')))";
        object? swapped = cmd.ExecuteScalar();
        AssertTrue(swapped?.ToString()?.Contains("POINT(2 1)") == true, "Coordinates should be swapped");

        cmd.CommandText = "SELECT ST_AsText(ST_PointFromGeoHash('9q8yy', 0))";
        object? fromGeoHash = cmd.ExecuteScalar();
        AssertTrue(fromGeoHash?.ToString()?.Contains("POINT") == true, "Should create point from geohash");

        cmd.CommandText = "SELECT ST_GeoHash(ST_GeomFromText('POINT(-89.4194 37.7749)', 4326), 10)";
        object? geoHash = cmd.ExecuteScalar();
        AssertTrue(geoHash != null && geoHash.ToString()!.Length == 10, "GeoHash should be 10 characters");

        cmd.CommandText = "SELECT ST_LatFromGeoHash('9q8yy')";
        object? latFromHash = cmd.ExecuteScalar();
        AssertTrue(latFromHash != null, "Should extract latitude from geohash");

        cmd.CommandText = "SELECT ST_LongFromGeoHash('9q8yy')";
        object? lonFromHash = cmd.ExecuteScalar();
        AssertTrue(lonFromHash != null, "Should extract longitude from geohash");

        cmd.CommandText = "SELECT ST_IsValid(ST_GeomFromText('POINT(1 1)'))";
        object? isValid = cmd.ExecuteScalar();
        AssertEqual(1, Convert.ToInt32(isValid!), "Valid geometry should return 1");

        cmd.CommandText = "SELECT ST_IsEmpty(ST_GeomFromText('POINT(1 1)'))";
        object? isEmpty = cmd.ExecuteScalar();
        AssertEqual(0, Convert.ToInt32(isEmpty!), "Non-empty geometry should return 0");

        cmd.CommandText = "SELECT ST_IsSimple(ST_GeomFromText('LINESTRING(0 0, 1 1)'))";
        object? isSimple = cmd.ExecuteScalar();
        AssertEqual(1, Convert.ToInt32(isSimple!), "Simple linestring should return 1");
    }

    protected override void CleanupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS spatial_coords";
        cmd.ExecuteNonQuery();
    }
}
