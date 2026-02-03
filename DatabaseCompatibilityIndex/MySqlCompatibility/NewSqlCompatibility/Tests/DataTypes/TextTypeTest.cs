using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.DataTypes;

[SqlTest(SqlFeatureCategory.DataTypes, "Test TEXT type for large strings", DatabaseType.MySql)]
public class TextTypeTest : SqlTest
{
    public override void Execute(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "CREATE TABLE text_test (id INT PRIMARY KEY, content TEXT)";
        cmd.ExecuteNonQuery();

        string largeText = new string('x', 10000);
        cmd.CommandText = $"INSERT INTO text_test VALUES (1, '{largeText}')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT LENGTH(content) FROM text_test WHERE id = 1";
        object? length = cmd.ExecuteScalar();
        AssertEqual(10000L, (long)length!, "TEXT should store large strings");

        cmd.CommandText = "DROP TABLE text_test";
        cmd.ExecuteNonQuery();
    }
}
