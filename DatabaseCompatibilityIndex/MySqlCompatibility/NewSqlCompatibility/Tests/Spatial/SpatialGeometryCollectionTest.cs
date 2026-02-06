using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Spatial;

[SqlTest(SqlFeatureCategory.Misc, "Test SPATIAL geometry collections and multi-geometries - unsupported in TiDB", DatabaseType.MySql)]
public class SpatialGeometryCollectionTest : SqlTest
{
    protected override void SetupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "CREATE TABLE spatial_collections (id INT PRIMARY KEY, geom GEOMETRY)";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "INSERT INTO spatial_collections (id, geom) VALUES (1, ST_GeomFromText('MULTIPOINT((0 0), (1 1), (2 2))'))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT ST_GeometryType(geom) FROM spatial_collections WHERE id = 1";
        object? geomType = cmd.ExecuteScalar();
        AssertEqual("MULTIPOINT", geomType?.ToString(), "Should be MULTIPOINT type");

        cmd.CommandText = "SELECT ST_NumGeometries(geom) FROM spatial_collections WHERE id = 1";
        object? numGeoms = cmd.ExecuteScalar();
        AssertEqual(3, Convert.ToInt32(numGeoms!), "Should have 3 points in MULTIPOINT");

        cmd.CommandText = "SELECT ST_AsText(ST_GeometryN(geom, 2)) FROM spatial_collections WHERE id = 1";
        object? secondPoint = cmd.ExecuteScalar();
        AssertTrue(secondPoint?.ToString()?.Contains("POINT(1 1)") == true, "Second point should be (1 1)");

        cmd.CommandText = "INSERT INTO spatial_collections (id, geom) VALUES (2, ST_GeomFromText('MULTILINESTRING((0 0, 1 1), (2 2, 3 3))'))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT ST_NumGeometries(geom) FROM spatial_collections WHERE id = 2";
        numGeoms = cmd.ExecuteScalar();
        AssertEqual(2, Convert.ToInt32(numGeoms!), "Should have 2 linestrings in MULTILINESTRING");

        cmd.CommandText = "INSERT INTO spatial_collections (id, geom) VALUES (3, ST_GeomFromText('MULTIPOLYGON(((0 0, 5 0, 5 5, 0 5, 0 0)), ((10 10, 15 10, 15 15, 10 15, 10 10)))'))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT ST_Area(geom) FROM spatial_collections WHERE id = 3";
        object? totalArea = cmd.ExecuteScalar();
        AssertEqual(50.0, Convert.ToDouble(totalArea!), "Total area of two 5x5 polygons should be 50");

        cmd.CommandText = "INSERT INTO spatial_collections (id, geom) VALUES (4, ST_GeomFromText('GEOMETRYCOLLECTION(POINT(1 1), LINESTRING(0 0, 2 2), POLYGON((0 0, 3 0, 3 3, 0 3, 0 0)))'))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT ST_NumGeometries(geom) FROM spatial_collections WHERE id = 4";
        numGeoms = cmd.ExecuteScalar();
        AssertEqual(3, Convert.ToInt32(numGeoms!), "Should have 3 different geometries in collection");

        cmd.CommandText = "SELECT ST_AsText(ST_StartPoint(ST_GeomFromText('LINESTRING(0 0, 5 5, 10 0)')))";
        object? startPoint = cmd.ExecuteScalar();
        AssertTrue(startPoint?.ToString()?.Contains("POINT(0 0)") == true, "Start point should be (0 0)");

        cmd.CommandText = "SELECT ST_AsText(ST_EndPoint(ST_GeomFromText('LINESTRING(0 0, 5 5, 10 0)')))";
        object? endPoint = cmd.ExecuteScalar();
        AssertTrue(endPoint?.ToString()?.Contains("POINT(10 0)") == true, "End point should be (10 0)");

        cmd.CommandText = "SELECT ST_AsText(ST_PointN(ST_GeomFromText('LINESTRING(0 0, 5 5, 10 0)'), 2))";
        object? middlePoint = cmd.ExecuteScalar();
        AssertTrue(middlePoint?.ToString()?.Contains("POINT(5 5)") == true, "Middle point should be (5 5)");

        cmd.CommandText = "SELECT ST_NumPoints(ST_GeomFromText('LINESTRING(0 0, 5 5, 10 0, 15 5)'))";
        object? numPoints = cmd.ExecuteScalar();
        AssertEqual(4, Convert.ToInt32(numPoints!), "Should have 4 points in linestring");

        cmd.CommandText = "SELECT ST_IsClosed(ST_GeomFromText('LINESTRING(0 0, 5 5, 10 0, 0 0)'))";
        object? isClosed = cmd.ExecuteScalar();
        AssertEqual(1, Convert.ToInt32(isClosed!), "Closed linestring should return 1");

        cmd.CommandText = "SELECT ST_AsText(ST_ExteriorRing(ST_GeomFromText('POLYGON((0 0, 10 0, 10 10, 0 10, 0 0), (2 2, 8 2, 8 8, 2 8, 2 2))')))";
        object? exteriorRing = cmd.ExecuteScalar();
        AssertTrue(exteriorRing?.ToString()?.Contains("LINESTRING") == true, "Exterior ring should be a LINESTRING");

        cmd.CommandText = "SELECT ST_NumInteriorRings(ST_GeomFromText('POLYGON((0 0, 10 0, 10 10, 0 10, 0 0), (2 2, 8 2, 8 8, 2 8, 2 2))'))";
        object? numRings = cmd.ExecuteScalar();
        AssertEqual(1, Convert.ToInt32(numRings!), "Should have 1 interior ring (hole)");

        cmd.CommandText = "SELECT ST_AsText(ST_InteriorRingN(ST_GeomFromText('POLYGON((0 0, 10 0, 10 10, 0 10, 0 0), (2 2, 8 2, 8 8, 2 8, 2 2))'), 1))";
        object? interiorRing = cmd.ExecuteScalar();
        AssertTrue(interiorRing?.ToString()?.Contains("LINESTRING") == true, "Interior ring should be a LINESTRING");
    }

    protected override void CleanupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS spatial_collections";
        cmd.ExecuteNonQuery();
    }
}
