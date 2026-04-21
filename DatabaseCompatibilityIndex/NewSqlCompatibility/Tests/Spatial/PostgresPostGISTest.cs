using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Spatial;

[SqlTest(SqlFeatureCategory.Spatial, "PostGIS geometry types and spatial functions", DatabaseType.PostgreSql)]
public class PostgresPostGISTest : SqlTest
{
    protected override void SetupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "CREATE EXTENSION IF NOT EXISTS postgis";
        cmd.ExecuteNonQuery();

        cmd.CommandText = """
            CREATE TABLE locations (
                id       SERIAL PRIMARY KEY,
                name     VARCHAR(50),
                position GEOMETRY(POINT)
            )
            """;
        cmd.ExecuteNonQuery();
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = """
            INSERT INTO locations (name, position) VALUES
            ('A', ST_GeomFromText('POINT(0 0)')),
            ('B', ST_GeomFromText('POINT(3 4)'))
            """;
        cmd.ExecuteNonQuery();

        // ST_X / ST_Y — basic coordinate readout
        cmd.CommandText = "SELECT ST_X(position), ST_Y(position) FROM locations WHERE name = 'A'";
        using (DbDataReader reader = cmd.ExecuteReader())
        {
            AssertTrue(reader.Read(), "Row A not found");
            AssertEqual(0.0, reader.GetDouble(0), "ST_X");
            AssertEqual(0.0, reader.GetDouble(1), "ST_Y");
        }

        // ST_Distance — 3-4-5 triangle, distance should be 5
        cmd.CommandText = """
            SELECT ST_Distance(
                ST_GeomFromText('POINT(0 0)'),
                ST_GeomFromText('POINT(3 4)')
            )
            """;
        double dist = (double)cmd.ExecuteScalar()!;
        AssertEqual(5.0, dist, "ST_Distance(3,4) should be 5");

        // ST_Contains — point inside polygon
        cmd.CommandText = """
            SELECT ST_Contains(
                ST_GeomFromText('POLYGON((0 0, 10 0, 10 10, 0 10, 0 0))'),
                ST_GeomFromText('POINT(5 5)')
            )
            """;
        bool inside = (bool)cmd.ExecuteScalar()!;
        AssertTrue(inside, "Point (5,5) should be inside the polygon");
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS locations";
        cmd.ExecuteNonQuery();
    }
}
