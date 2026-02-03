using NSCI.Testing;using NSCI.Configuration;
using System.Data.Common;

namespace NSCI.Tests.JSONOperations;

[SqlTest(SqlFeatureCategory.JSONOperations, "Test JSON_LENGTH function", DatabaseType.MySql)]
public class JsonLengthTest : SqlTest
{
    public override void Setup(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = @"CREATE TABLE items (
                            id INT PRIMARY KEY,
                            tags JSON
                        )";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"INSERT INTO items VALUES (1, '[""python"", ""javascript"", ""sql""]')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"INSERT INTO items VALUES (2, '[""java""]')";
        cmd.ExecuteNonQuery();
    }

    public override void Execute(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        // Get array length
        cmd.CommandText = "SELECT JSON_LENGTH(tags) FROM items WHERE id = 1";
        object? length = cmd.ExecuteScalar();
        AssertEqual(3L, (long)length!, "Should have 3 tags");

        cmd.CommandText = "SELECT JSON_LENGTH(tags) FROM items WHERE id = 2";
        length = cmd.ExecuteScalar();
        AssertEqual(1L, (long)length!, "Should have 1 tag");

        // Filter by JSON length
        cmd.CommandText = "SELECT COUNT(*) FROM items WHERE JSON_LENGTH(tags) > 1";
        object? count = cmd.ExecuteScalar();
        AssertEqual(1L, (long)count!, "Should find 1 item with more than 1 tag");
    }

    public override void Cleanup(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE items";
        cmd.ExecuteNonQuery();
    }
}
