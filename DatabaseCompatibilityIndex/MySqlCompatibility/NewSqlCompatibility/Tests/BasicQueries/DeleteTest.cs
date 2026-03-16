using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.BasicQueries;

[SqlTest(SqlFeatureCategory.BasicQueries, "DELETE operation")]
public class DeleteTest : SqlTest
{
    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "CREATE TABLE records (id INT PRIMARY KEY, value INT)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO records VALUES (1, 100), (2, 200), (3, 300)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "DELETE FROM records WHERE id = 2";
        int affectedRows = cmd.ExecuteNonQuery();
        AssertEqual(1, affectedRows, "Should delete 1 row");

        cmd.CommandText = "SELECT COUNT(*) FROM records";
        object? count = cmd.ExecuteScalar();
        AssertEqual(2L, (long)count!, "Should have 2 records left");

        cmd.CommandText = "DROP TABLE records";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "CREATE TABLE records (id INT PRIMARY KEY, value INT)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO records VALUES (1, 100), (2, 200), (3, 300)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "DELETE FROM records WHERE id = 2";
        int affectedRows = cmd.ExecuteNonQuery();
        AssertEqual(1, affectedRows, "Should delete 1 row");

        cmd.CommandText = "SELECT COUNT(*) FROM records";
        object? count = cmd.ExecuteScalar();
        AssertEqual(2L, (long)count!, "Should have 2 records left");

        cmd.CommandText = "DROP TABLE records";
        cmd.ExecuteNonQuery();
    }
}
