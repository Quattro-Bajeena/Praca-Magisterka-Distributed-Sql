using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Spatial;

[SqlTest(SqlFeatureCategory.Misc, "Test SPATIAL relationship functions (ST_Contains, ST_Intersects, ST_Within) - unsupported in TiDB")]
public class SpatialRelationshipsTest : SqlTest
{
    protected override void SetupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "CREATE TABLE spatial_relations (id INT PRIMARY KEY, geom GEOMETRY)";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = @"SELECT ST_Contains(
            ST_GeomFromText('POLYGON((0 0, 10 0, 10 10, 0 10, 0 0))'),
            ST_GeomFromText('POINT(5 5)')
        )";
        object? contains = cmd.ExecuteScalar();
        AssertEqual(1, Convert.ToInt32(contains!), "Polygon should contain the point");

        cmd.CommandText = @"SELECT ST_Contains(
            ST_GeomFromText('POLYGON((0 0, 10 0, 10 10, 0 10, 0 0))'),
            ST_GeomFromText('POINT(15 15)')
        )";
        contains = cmd.ExecuteScalar();
        AssertEqual(0, Convert.ToInt32(contains!), "Polygon should not contain point outside");

        cmd.CommandText = @"SELECT ST_Within(
            ST_GeomFromText('POINT(5 5)'),
            ST_GeomFromText('POLYGON((0 0, 10 0, 10 10, 0 10, 0 0))')
        )";
        object? within = cmd.ExecuteScalar();
        AssertEqual(1, Convert.ToInt32(within!), "Point should be within polygon");

        cmd.CommandText = @"SELECT ST_Intersects(
            ST_GeomFromText('POLYGON((0 0, 10 0, 10 10, 0 10, 0 0))'),
            ST_GeomFromText('POLYGON((5 5, 15 5, 15 15, 5 15, 5 5))')
        )";
        object? intersects = cmd.ExecuteScalar();
        AssertEqual(1, Convert.ToInt32(intersects!), "Polygons should intersect");

        cmd.CommandText = @"SELECT ST_Disjoint(
            ST_GeomFromText('POLYGON((0 0, 5 0, 5 5, 0 5, 0 0))'),
            ST_GeomFromText('POLYGON((10 10, 15 10, 15 15, 10 15, 10 10))')
        )";
        object? disjoint = cmd.ExecuteScalar();
        AssertEqual(1, Convert.ToInt32(disjoint!), "Polygons should be disjoint");

        cmd.CommandText = @"SELECT ST_Touches(
            ST_GeomFromText('POLYGON((0 0, 5 0, 5 5, 0 5, 0 0))'),
            ST_GeomFromText('POLYGON((5 0, 10 0, 10 5, 5 5, 5 0))')
        )";
        object? touches = cmd.ExecuteScalar();
        AssertEqual(1, Convert.ToInt32(touches!), "Adjacent polygons should touch");

        cmd.CommandText = @"SELECT ST_Crosses(
            ST_GeomFromText('LINESTRING(-5 5, 15 5)'),
            ST_GeomFromText('POLYGON((0 0, 10 0, 10 10, 0 10, 0 0))')
        )";
        object? crosses = cmd.ExecuteScalar();
        AssertEqual(1, Convert.ToInt32(crosses!), "Linestring should cross polygon");

        cmd.CommandText = @"SELECT ST_Overlaps(
            ST_GeomFromText('POLYGON((0 0, 10 0, 10 10, 0 10, 0 0))'),
            ST_GeomFromText('POLYGON((5 5, 15 5, 15 15, 5 15, 5 5))')
        )";
        object? overlaps = cmd.ExecuteScalar();
        AssertEqual(1, Convert.ToInt32(overlaps!), "Polygons should overlap");

        cmd.CommandText = @"SELECT ST_Equals(
            ST_GeomFromText('POINT(1 1)'),
            ST_GeomFromText('POINT(1 1)')
        )";
        object? equals = cmd.ExecuteScalar();
        AssertEqual(1, Convert.ToInt32(equals!), "Identical points should be equal");
    }

    protected override void CleanupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS spatial_relations";
        cmd.ExecuteNonQuery();
    }
}
