using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Spatial;

[SqlTest(SqlFeatureCategory.Misc, "Test SPATIAL measurement functions (ST_Distance, ST_Area, ST_Length) - unsupported in TiDB")]
public class SpatialMeasurementTest : SqlTest
{
    protected override void SetupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "CREATE TABLE spatial_measure (id INT PRIMARY KEY, geom GEOMETRY)";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "SELECT ST_Distance(ST_GeomFromText('POINT(0 0)'), ST_GeomFromText('POINT(3 4)'))";
        object? distance = cmd.ExecuteScalar();
        AssertEqual(5.0, Convert.ToDouble(distance!), "Distance should be 5 (3-4-5 right triangle)");

        cmd.CommandText = "SELECT ST_Distance_Sphere(ST_GeomFromText('POINT(0 0)', 4326), ST_GeomFromText('POINT(0 1)', 4326))";
        distance = cmd.ExecuteScalar();
        double sphereDistance = Convert.ToDouble(distance!);
        AssertTrue(sphereDistance > 110000 && sphereDistance < 112000, "Sphere distance should be ~111km for 1 degree");

        cmd.CommandText = "SELECT ST_Length(ST_GeomFromText('LINESTRING(0 0, 3 0, 3 4)'))";
        object? length = cmd.ExecuteScalar();
        AssertEqual(7.0, Convert.ToDouble(length!), "Length should be 7 (3 + 4)");

        cmd.CommandText = "SELECT ST_Area(ST_GeomFromText('POLYGON((0 0, 10 0, 10 10, 0 10, 0 0))'))";
        object? area = cmd.ExecuteScalar();
        AssertEqual(100.0, Convert.ToDouble(area!), "Area should be 100 (10x10 square)");

        cmd.CommandText = "SELECT ST_Length(ST_ExteriorRing(ST_GeomFromText('POLYGON((0 0, 5 0, 5 5, 0 5, 0 0))')))";
        object? perimeter = cmd.ExecuteScalar();
        AssertEqual(20.0, Convert.ToDouble(perimeter!), "Perimeter should be 20 (5+5+5+5)");
    }

    protected override void CleanupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS spatial_measure";
        cmd.ExecuteNonQuery();
    }
}
