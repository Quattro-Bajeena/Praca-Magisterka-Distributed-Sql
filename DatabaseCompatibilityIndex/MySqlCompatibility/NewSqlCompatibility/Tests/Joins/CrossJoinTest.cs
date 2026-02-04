using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Joins;

[SqlTest(SqlFeatureCategory.Joins, "Test CROSS JOIN")]
public class CrossJoinTest : SqlTest
{
    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "CREATE TABLE colors (id INT PRIMARY KEY, color VARCHAR(20))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "CREATE TABLE sizes (id INT PRIMARY KEY, size VARCHAR(20))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO colors VALUES (1, 'Red'), (2, 'Blue'), (3, 'Green')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO sizes VALUES (1, 'S'), (2, 'M'), (3, 'L')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM colors CROSS JOIN sizes";
        object? count = cmd.ExecuteScalar();
        AssertEqual(9L, (long)count!, "CROSS JOIN should return 3x3=9 combinations");

        cmd.CommandText = "DROP TABLE sizes";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "DROP TABLE colors";
        cmd.ExecuteNonQuery();
    }
}
