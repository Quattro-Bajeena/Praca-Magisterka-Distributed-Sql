using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.DataTypes;

[SqlTest(SqlFeatureCategory.DataTypes, "Test BIGINT data type", DatabaseType.MySql)]
public class BigIntTypeTest : SqlTest
{
    public override void Execute(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "CREATE TABLE bigint_test (id BIGINT PRIMARY KEY)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO bigint_test VALUES (9223372036854775807), (-9223372036854775808)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM bigint_test";
        object? count = cmd.ExecuteScalar();
        AssertEqual(2L, (long)count!, "Should have 2 BIGINT values");

        cmd.CommandText = "DROP TABLE bigint_test";
        cmd.ExecuteNonQuery();
    }
}
