using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Joins;

[SqlTest(SqlFeatureCategory.Joins, "Test RIGHT JOIN (if supported)")]
public class RightJoinTest : SqlTest
{
    protected override void SetupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "CREATE TABLE left_t (id INT PRIMARY KEY, name VARCHAR(50))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "CREATE TABLE right_t (id INT PRIMARY KEY, left_id INT, value INT)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO left_t VALUES (1, 'A'), (2, 'B')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO right_t VALUES (1, 1, 100), (2, 1, 200), (3, 3, 300)";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "SELECT COUNT(*) FROM left_t l RIGHT JOIN right_t r ON l.id = r.left_id";
        object? count = cmd.ExecuteScalar();
        AssertEqual(3L, (long)count!, "RIGHT JOIN should work correctly");
    }

    protected override void CleanupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE right_t";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "DROP TABLE left_t";
        cmd.ExecuteNonQuery();
    }
}
