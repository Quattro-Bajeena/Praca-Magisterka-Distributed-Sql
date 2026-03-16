using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Misc;

[SqlTest(SqlFeatureCategory.Misc, "Test OPTIMIZE TABLE syntax ")]
public class OptimizeTableTest : SqlTest
{
    protected override void SetupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "CREATE TABLE optimize_test (id INT PRIMARY KEY, data VARCHAR(100))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO optimize_test VALUES (1, 'data1'), (2, 'data2')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "DELETE FROM optimize_test WHERE id = 2";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "OPTIMIZE TABLE optimize_test";
        using (DbDataReader reader = cmd.ExecuteReader())
        {
            AssertTrue(reader.Read(), "OPTIMIZE TABLE should return results");

            string? msgType = reader.GetString(reader.GetOrdinal("Msg_type"));
            AssertTrue(msgType != null && msgType.Length > 0, "Should return message type");
        }
    }

    protected override void CleanupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS optimize_test";
        cmd.ExecuteNonQuery();
    }

    protected override void SetupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "CREATE TABLE optimize_test (id INT PRIMARY KEY, data VARCHAR(100))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO optimize_test VALUES (1, 'data1'), (2, 'data2')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "DELETE FROM optimize_test WHERE id = 2";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "VACUUM ANALYZE optimize_test";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM optimize_test";
        object? count = cmd.ExecuteScalar();
        AssertEqual(1L, Convert.ToInt64(count!), "Should have 1 row after vacuum");
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS optimize_test";
        cmd.ExecuteNonQuery();
    }
}
