using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Spatial;

[SqlTest(SqlFeatureCategory.Spatial, "Test SPATIAL indexes", Configuration.DatabaseType.MySql)]
public class SpatialIndexTest : SqlTest
{
    protected override void SetupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = @"CREATE TABLE locations (
                            id INT PRIMARY KEY AUTO_INCREMENT,
                            name VARCHAR(100),
                            coordinates POINT NOT NULL,
                            SPATIAL INDEX idx_coordinates (coordinates)
                        )";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO locations (name, coordinates) VALUES ('Point A', ST_GeomFromText('POINT(0 0)'))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO locations (name, coordinates) VALUES ('Point B', ST_GeomFromText('POINT(1 1)'))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO locations (name, coordinates) VALUES ('Point C', ST_GeomFromText('POINT(5 5)'))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO locations (name, coordinates) VALUES ('Point D', ST_GeomFromText('POINT(10 10)'))";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = @"SELECT COUNT(*) FROM locations 
                           WHERE ST_Contains(
                               ST_GeomFromText('POLYGON((-1 -1, 6 -1, 6 6, -1 6, -1 -1))'),
                               coordinates
                           )";
        object? count = cmd.ExecuteScalar();
        AssertEqual(3L, Convert.ToInt64(count!), "Should find 3 points within the polygon");

        cmd.CommandText = @"SELECT name FROM locations 
                           WHERE ST_Distance(coordinates, ST_GeomFromText('POINT(0 0)')) < 2 
                           ORDER BY name";
        using (DbDataReader reader = cmd.ExecuteReader())
        {
            int resultCount = 0;
            while (reader.Read())
            {
                resultCount++;
            }
            AssertEqual(2, resultCount, "Should find 2 points near origin");
        }

        cmd.CommandText = "SHOW INDEX FROM locations WHERE Key_name = 'idx_coordinates'";
        using (DbDataReader reader = cmd.ExecuteReader())
        {
            AssertTrue(reader.Read(), "Spatial index should exist");
            string? indexType = reader.IsDBNull(reader.GetOrdinal("Index_type"))
                ? null
                : reader.GetString(reader.GetOrdinal("Index_type"));
            AssertEqual("SPATIAL", indexType, "Index type should be SPATIAL");
        }
    }

    protected override void CleanupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS locations";
        cmd.ExecuteNonQuery();
    }
}
