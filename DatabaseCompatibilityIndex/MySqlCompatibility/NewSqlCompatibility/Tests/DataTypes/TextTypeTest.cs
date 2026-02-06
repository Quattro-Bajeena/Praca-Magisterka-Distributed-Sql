using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.DataTypes;

[SqlTest(SqlFeatureCategory.DataTypes, "Test TEXT type for large strings")]
public class TextTypeTest : SqlTest
{
    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
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

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "CREATE TABLE text_test (id INT PRIMARY KEY, content TEXT)";
        cmd.ExecuteNonQuery();

        string largeText = new string('x', 10000);
        cmd.CommandText = $"INSERT INTO text_test VALUES (1, '{largeText}')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT LENGTH(content) FROM text_test WHERE id = 1";
        object? length = cmd.ExecuteScalar();
        AssertEqual(10000, Convert.ToInt32(length!), "TEXT should store large strings");

        cmd.CommandText = "DROP TABLE text_test";
        cmd.ExecuteNonQuery();
    }
}
