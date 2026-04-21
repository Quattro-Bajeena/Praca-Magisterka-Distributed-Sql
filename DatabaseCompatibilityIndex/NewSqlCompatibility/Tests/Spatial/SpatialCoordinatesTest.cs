using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Spatial;

[SqlTest(SqlFeatureCategory.Spatial, "Basic spatial point and polygon types")]
public class SpatialCoordinatesTest : SqlTest
{
    // ── MySQL ────────────────────────────────────────────────────────────────

    protected override string? SetupCommandMy => """
        CREATE TABLE locations (
            id   INT PRIMARY KEY,
            name VARCHAR(50),
            pt   POINT NOT NULL
        )
        """;

    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = """
            INSERT INTO locations VALUES
            (1, 'A', ST_GeomFromText('POINT(0 0)')),
            (2, 'B', ST_GeomFromText('POINT(3 4)'))
            """;
        cmd.ExecuteNonQuery();

        // ST_X / ST_Y
        cmd.CommandText = "SELECT ST_X(pt), ST_Y(pt) FROM locations WHERE id = 1";
        using (DbDataReader reader = cmd.ExecuteReader())
        {
            AssertTrue(reader.Read(), "Row 1 not found");
            AssertEqual(0.0, reader.GetDouble(0), "ST_X");
            AssertEqual(0.0, reader.GetDouble(1), "ST_Y");
        }

        // ST_Distance — 3-4-5 triangle
        cmd.CommandText = """
            SELECT ST_Distance(
                ST_GeomFromText('POINT(0 0)'),
                ST_GeomFromText('POINT(3 4)')
            )
            """;
        double dist = Convert.ToDouble(cmd.ExecuteScalar());
        AssertEqual(5.0, dist, "ST_Distance(3,4) should be 5");

        // ST_Contains — point inside polygon
        cmd.CommandText = """
            SELECT ST_Contains(
                ST_GeomFromText('POLYGON((0 0, 10 0, 10 10, 0 10, 0 0))'),
                ST_GeomFromText('POINT(5 5)')
            )
            """;
        long inside = Convert.ToInt64(cmd.ExecuteScalar());
        AssertEqual(1L, inside, "Point (5,5) should be inside the polygon");
    }

    protected override string? CleanupCommandMy => "DROP TABLE locations";

    // ── PostgreSQL ───────────────────────────────────────────────────────────

    protected override string? SetupCommandPg => """
        CREATE TABLE locations (
            id   SERIAL PRIMARY KEY,
            name VARCHAR(50),
            pt   POINT NOT NULL
        )
        """;

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = """
            INSERT INTO locations (id, name, pt) VALUES
            (1, 'A', POINT(0, 0)),
            (2, 'B', POINT(3, 4))
            """;
        cmd.ExecuteNonQuery();

        // coordinate subscript
        cmd.CommandText = "SELECT pt[0], pt[1] FROM locations WHERE id = 1";
        using (DbDataReader reader = cmd.ExecuteReader())
        {
            AssertTrue(reader.Read(), "Row 1 not found");
            AssertEqual(0.0, reader.GetDouble(0), "pt[0]");
            AssertEqual(0.0, reader.GetDouble(1), "pt[1]");
        }

        // <-> distance operator — 3-4-5 triangle
        cmd.CommandText = "SELECT POINT(0,0) <-> POINT(3,4)";
        double dist = (double)cmd.ExecuteScalar()!;
        AssertEqual(5.0, dist, "<-> distance should be 5");

        // @> polygon contains point
        cmd.CommandText = "SELECT POLYGON '((0,0),(10,0),(10,10),(0,10))' @> POINT(5,5)";
        bool inside = (bool)cmd.ExecuteScalar()!;
        AssertTrue(inside, "Point (5,5) should be inside the polygon");
    }

    protected override string? CleanupCommandPg => "DROP TABLE locations";
}
