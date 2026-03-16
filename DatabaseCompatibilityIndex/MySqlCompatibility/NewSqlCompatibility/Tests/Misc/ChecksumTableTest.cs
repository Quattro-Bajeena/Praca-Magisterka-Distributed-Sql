using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Misc;

[SqlTest(SqlFeatureCategory.Misc, "Test CHECKSUM TABLE syntax", DatabaseType.MySql)]
public class ChecksumTableTest : SqlTest
{
    protected override void SetupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "CREATE TABLE checksum_test (id INT PRIMARY KEY, data VARCHAR(100))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO checksum_test VALUES (1, 'data1'), (2, 'data2'), (3, 'data3')";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "CHECKSUM TABLE checksum_test";
        using (DbDataReader reader = cmd.ExecuteReader())
        {
            AssertTrue(reader.Read(), "CHECKSUM TABLE should return results");
            object? checksum = reader.GetValue(reader.GetOrdinal("Checksum"));
            AssertTrue(checksum != null, "Should return a checksum value");
        }
    }

    protected override void CleanupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS checksum_test";
        cmd.ExecuteNonQuery();
    }
}
