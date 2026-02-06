using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Spatial;

[SqlTest(SqlFeatureCategory.Misc, "Test SPATIAL data types (POINT, LINESTRING, POLYGON) - unsupported in TiDB")]
public class SpatialDataTypesTest : SqlTest
{
    protected override void SetupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = @"CREATE TABLE spatial_types (
                            id INT PRIMARY KEY,
                            location POINT,
                            route LINESTRING,
                            area POLYGON,
                            collection GEOMETRY
                        )";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "INSERT INTO spatial_types (id, location) VALUES (1, ST_GeomFromText('POINT(1 1)'))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO spatial_types (id, route) VALUES (2, ST_GeomFromText('LINESTRING(0 0, 1 1, 2 2)'))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO spatial_types (id, area) VALUES (3, ST_GeomFromText('POLYGON((0 0, 10 0, 10 10, 0 10, 0 0))'))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT ST_AsText(location) FROM spatial_types WHERE id = 1";
        object? result = cmd.ExecuteScalar();
        AssertTrue(result?.ToString()?.Contains("POINT(1 1)") == true, "POINT should be stored correctly");

        cmd.CommandText = "SELECT ST_AsText(route) FROM spatial_types WHERE id = 2";
        result = cmd.ExecuteScalar();
        AssertTrue(result?.ToString()?.Contains("LINESTRING") == true, "LINESTRING should be stored correctly");

        cmd.CommandText = "SELECT ST_AsText(area) FROM spatial_types WHERE id = 3";
        result = cmd.ExecuteScalar();
        AssertTrue(result?.ToString()?.Contains("POLYGON") == true, "POLYGON should be stored correctly");

        cmd.CommandText = "SELECT ST_GeometryType(location) FROM spatial_types WHERE id = 1";
        result = cmd.ExecuteScalar();
        AssertEqual("POINT", result?.ToString(), "Geometry type should be POINT");
    }

    protected override void CleanupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS spatial_types";
        cmd.ExecuteNonQuery();
    }
}
