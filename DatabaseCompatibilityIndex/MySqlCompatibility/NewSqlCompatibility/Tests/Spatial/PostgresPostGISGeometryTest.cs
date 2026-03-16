using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Spatial;

[SqlTest(SqlFeatureCategory.Spatial, "Test PostgreSQL/PostGIS basic geometry types", DatabaseType.PostgreSql)]
public class PostgresPostGISGeometryTest : SqlTest
{
    protected override void SetupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        
        cmd.CommandText = "CREATE EXTENSION IF NOT EXISTS postgis  SCHEMA public";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"CREATE TABLE postgis_geom_test (
                            id SERIAL PRIMARY KEY,
                            name VARCHAR(100),
                            geom GEOMETRY
                        )";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "INSERT INTO postgis_geom_test (name, geom) VALUES ('Point1', ST_GeomFromText('POINT(1 2)', 4326))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO postgis_geom_test (name, geom) VALUES ('Line1', ST_GeomFromText('LINESTRING(0 0, 1 1, 2 2)', 4326))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO postgis_geom_test (name, geom) VALUES ('Polygon1', ST_GeomFromText('POLYGON((0 0, 4 0, 4 4, 0 4, 0 0))', 4326))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT ST_AsText(geom) FROM postgis_geom_test WHERE name = 'Point1'";
        object? point = cmd.ExecuteScalar();
        AssertTrue(point?.ToString()?.Contains("POINT") == true, "Should retrieve POINT geometry");

        cmd.CommandText = "SELECT ST_X(geom), ST_Y(geom) FROM postgis_geom_test WHERE name = 'Point1'";
        using (DbDataReader reader = cmd.ExecuteReader())
        {
            AssertTrue(reader.Read(), "Should have point coordinates");
            double x = reader.GetDouble(0);
            double y = reader.GetDouble(1);
            AssertEqual(1.0, x, "X coordinate should be 1");
            AssertEqual(2.0, y, "Y coordinate should be 2");
        }

        cmd.CommandText = "SELECT ST_GeometryType(geom) FROM postgis_geom_test WHERE name = 'Line1'";
        object? geomType = cmd.ExecuteScalar();
        AssertTrue(geomType?.ToString()?.Contains("LineString") == true, "Should be LineString type");

        cmd.CommandText = "SELECT ST_SRID(geom) FROM postgis_geom_test WHERE name = 'Point1'";
        object? srid = cmd.ExecuteScalar();
        AssertEqual(4326, Convert.ToInt32(srid!), "SRID should be 4326 (WGS84)");

        cmd.CommandText = "SELECT ST_IsValid(geom) FROM postgis_geom_test WHERE name = 'Polygon1'";
        object? isValid = cmd.ExecuteScalar();
        AssertTrue(Convert.ToBoolean(isValid!), "Polygon should be valid");

        cmd.CommandText = "SELECT ST_IsEmpty(ST_GeomFromText('POINT EMPTY'))";
        object? isEmpty = cmd.ExecuteScalar();
        AssertTrue(Convert.ToBoolean(isEmpty!), "Empty point should return true");

        cmd.CommandText = "SELECT ST_Dimension(geom) FROM postgis_geom_test WHERE name = 'Polygon1'";
        object? dimension = cmd.ExecuteScalar();
        AssertEqual(2, Convert.ToInt32(dimension!), "Polygon should have dimension 2");
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS postgis_geom_test CASCADE";
        cmd.ExecuteNonQuery();
    }
}
