using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Spatial;

[SqlTest(SqlFeatureCategory.Spatial, "Test SPATIAL operations (ST_Buffer, ST_Union, ST_Intersection)", Configuration.DatabaseType.MySql)]
public class SpatialOperationsTest : SqlTest
{
    protected override void SetupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "CREATE TABLE spatial_ops (id INT PRIMARY KEY, geom GEOMETRY)";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "SELECT ST_Area(ST_Buffer(ST_GeomFromText('POINT(0 0)'), 5))";
        object? bufferArea = cmd.ExecuteScalar();
        double area = Convert.ToDouble(bufferArea!);
        AssertTrue(area > 75 && area < 80, "Buffer area should be approximately π * 5² ≈ 78.5");

        cmd.CommandText = "SELECT ST_AsText(ST_Envelope(ST_GeomFromText('LINESTRING(0 0, 10 5, 5 10)')))";
        object? envelope = cmd.ExecuteScalar();
        AssertTrue(envelope?.ToString()?.Contains("POLYGON") == true, "Envelope should return a polygon");

        cmd.CommandText = "SELECT ST_AsText(ST_ConvexHull(ST_GeomFromText('MULTIPOINT(0 0, 10 0, 10 10, 5 5)')))";
        object? convexHull = cmd.ExecuteScalar();
        AssertTrue(convexHull?.ToString()?.Contains("POLYGON") == true, "ConvexHull should return a polygon");

        cmd.CommandText = @"SELECT ST_Area(ST_Union(
            ST_GeomFromText('POLYGON((0 0, 5 0, 5 5, 0 5, 0 0))'),
            ST_GeomFromText('POLYGON((3 3, 8 3, 8 8, 3 8, 3 3))')
        ))";
        object? unionArea = cmd.ExecuteScalar();
        double unionAreaValue = Convert.ToDouble(unionArea!);
        AssertTrue(unionAreaValue > 45 && unionAreaValue < 47, "Union area should be ~46");

        cmd.CommandText = @"SELECT ST_Area(ST_Intersection(
            ST_GeomFromText('POLYGON((0 0, 5 0, 5 5, 0 5, 0 0))'),
            ST_GeomFromText('POLYGON((3 3, 8 3, 8 8, 3 8, 3 3))')
        ))";
        object? intersectionArea = cmd.ExecuteScalar();
        double intersectAreaValue = Convert.ToDouble(intersectionArea!);
        AssertTrue(intersectAreaValue > 3.5 && intersectAreaValue < 4.5, "Intersection area should be ~4");

        cmd.CommandText = @"SELECT ST_Area(ST_Difference(
            ST_GeomFromText('POLYGON((0 0, 10 0, 10 10, 0 10, 0 0))'),
            ST_GeomFromText('POLYGON((3 3, 7 3, 7 7, 3 7, 3 3))')
        ))";
        object? differenceArea = cmd.ExecuteScalar();
        double diffAreaValue = Convert.ToDouble(differenceArea!);
        AssertTrue(diffAreaValue > 83 && diffAreaValue < 85, "Difference area should be ~84");

        cmd.CommandText = @"SELECT ST_Area(ST_SymDifference(
            ST_GeomFromText('POLYGON((0 0, 5 0, 5 5, 0 5, 0 0))'),
            ST_GeomFromText('POLYGON((3 3, 8 3, 8 8, 3 8, 3 3))')
        ))";
        object? symDiffArea = cmd.ExecuteScalar();
        AssertTrue(symDiffArea != null, "SymDifference should return a value");

        cmd.CommandText = "SELECT ST_AsText(ST_Centroid(ST_GeomFromText('POLYGON((0 0, 10 0, 10 10, 0 10, 0 0))')))";
        object? centroid = cmd.ExecuteScalar();
        AssertTrue(centroid?.ToString()?.Contains("POINT(5 5)") == true, "Centroid of square should be at center");

        cmd.CommandText = "SELECT ST_NumPoints(ST_Simplify(ST_GeomFromText('LINESTRING(0 0, 1 0.1, 2 0.2, 3 0.3, 4 0.4, 5 0)'), 0.5))";
        object? simplifiedPoints = cmd.ExecuteScalar();
        int pointCount = Convert.ToInt32(simplifiedPoints!);
        AssertTrue(pointCount < 6, "Simplified linestring should have fewer points than original");
    }

    protected override void CleanupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS spatial_ops";
        cmd.ExecuteNonQuery();
    }
}
