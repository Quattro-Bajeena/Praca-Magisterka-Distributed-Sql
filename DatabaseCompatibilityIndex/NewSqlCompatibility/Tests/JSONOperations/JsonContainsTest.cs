using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.JSONOperations;

[SqlTest(SqlFeatureCategory.JSONOperations, "Test JSON_CONTAINS function")]
public class JsonContainsTest : SqlTest
{
    protected override void SetupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = @"CREATE TABLE products (
                            id INT PRIMARY KEY,
                            attributes JSON
                        )";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"INSERT INTO products VALUES (1, '{""colors"": [""red"", ""blue"", ""green""], ""size"": ""large""}')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"INSERT INTO products VALUES (2, '{""colors"": [""black"", ""white""], ""size"": ""medium""}')";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "SELECT COUNT(*) FROM products WHERE JSON_CONTAINS(attributes, '\"red\"', '$.colors')";
        object? count = cmd.ExecuteScalar();
        AssertEqual(1L, (long)count!, "Should find product with 'red' color");

        cmd.CommandText = "SELECT COUNT(*) FROM products WHERE JSON_CONTAINS(attributes, '\"blue\"', '$.colors')";
        count = cmd.ExecuteScalar();
        AssertEqual(1L, (long)count!, "Should find product with 'blue' color");

        cmd.CommandText = "SELECT COUNT(*) FROM products WHERE JSON_CONTAINS(attributes, '{\"size\": \"large\"}')";
        count = cmd.ExecuteScalar();
        AssertEqual(1L, (long)count!, "Should find product with size 'large'");
    }

    protected override void CleanupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE products";
        cmd.ExecuteNonQuery();
    }

    protected override void SetupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = @"CREATE TABLE products (
                            id INT PRIMARY KEY,
                            attributes JSONB
                        )";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"INSERT INTO products VALUES (1, '{""colors"": [""red"", ""blue"", ""green""], ""size"": ""large""}')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"INSERT INTO products VALUES (2, '{""colors"": [""black"", ""white""], ""size"": ""medium""}')";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "SELECT COUNT(*) FROM products WHERE attributes->'colors' @> '\"red\"'::jsonb";
        object? count = cmd.ExecuteScalar();
        AssertEqual(1L, (long)count!, "Should find product with 'red' color");

        cmd.CommandText = "SELECT COUNT(*) FROM products WHERE attributes->'colors' @> '\"blue\"'::jsonb";
        count = cmd.ExecuteScalar();
        AssertEqual(1L, (long)count!, "Should find product with 'blue' color");

        cmd.CommandText = "SELECT COUNT(*) FROM products WHERE attributes @> '{\"size\": \"large\"}'::jsonb";
        count = cmd.ExecuteScalar();
        AssertEqual(1L, (long)count!, "Should find product with size 'large'");
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE products";
        cmd.ExecuteNonQuery();
    }
}
