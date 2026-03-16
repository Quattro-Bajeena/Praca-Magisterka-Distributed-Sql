using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.DataTypes;

[SqlTest(SqlFeatureCategory.DataTypes, "Test DATE type")]
public class DateTypeTest : SqlTest
{
    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "CREATE TABLE date_test (id INT PRIMARY KEY, date_col DATE)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO date_test VALUES (1, '2024-01-15'), (2, '2025-12-31')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM date_test WHERE date_col >= '2024-01-01'";
        object? count = cmd.ExecuteScalar();
        AssertEqual(2L, (long)count!, "DATE comparison should work");

        cmd.CommandText = "DROP TABLE date_test";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "CREATE TABLE date_test (id INT PRIMARY KEY, date_col DATE)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO date_test VALUES (1, '2024-01-15'), (2, '2025-12-31')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM date_test WHERE date_col >= '2024-01-01'";
        object? count = cmd.ExecuteScalar();
        AssertEqual(2L, (long)count!, "DATE comparison should work");

        cmd.CommandText = "DROP TABLE date_test";
        cmd.ExecuteNonQuery();
    }
}
