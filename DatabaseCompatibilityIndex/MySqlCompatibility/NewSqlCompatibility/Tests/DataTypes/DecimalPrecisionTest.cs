using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.DataTypes;

[SqlTest(SqlFeatureCategory.DataTypes, "Test DECIMAL precision (10,4)", DatabaseType.MySql)]
public class DecimalPrecisionTest : SqlTest
{
    public override void Execute(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "CREATE TABLE decimal_test (id INT PRIMARY KEY, amount DECIMAL(10,4))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO decimal_test VALUES (1, 123.4567)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT amount FROM decimal_test WHERE id = 1";
        object? result = cmd.ExecuteScalar();
        AssertEqual(123.4567m, Convert.ToDecimal(result), "DECIMAL precision should be preserved");

        cmd.CommandText = "DROP TABLE decimal_test";
        cmd.ExecuteNonQuery();
    }
}
