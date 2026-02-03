using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.DataTypes;

[SqlTest(SqlFeatureCategory.DataTypes, "Test INT data type", DatabaseType.MySql)]
public class IntTypeTest : SqlTest
{
    public override void Execute(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "CREATE TABLE int_test (id INT PRIMARY KEY, value INT)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO int_test VALUES (1, 2147483647), (2, -2147483648), (3, 0)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM int_test";
        object? count = cmd.ExecuteScalar();
        AssertEqual(3L, (long)count!, "Should have 3 INT values");

        cmd.CommandText = "DROP TABLE int_test";
        cmd.ExecuteNonQuery();
    }
}
