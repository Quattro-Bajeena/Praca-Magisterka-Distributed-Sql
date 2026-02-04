using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.DataTypes;

[SqlTest(SqlFeatureCategory.DataTypes, "Test DATETIME type")]
public class DateTimeTypeTest : SqlTest
{
    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "CREATE TABLE datetime_test (id INT PRIMARY KEY, timestamp_col DATETIME)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO datetime_test VALUES (1, '2024-01-15 14:30:45'), (2, '2025-12-31 23:59:59')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM datetime_test";
        object? count = cmd.ExecuteScalar();
        AssertEqual(2L, (long)count!, "DATETIME values should be stored");

        cmd.CommandText = "DROP TABLE datetime_test";
        cmd.ExecuteNonQuery();
    }
}
