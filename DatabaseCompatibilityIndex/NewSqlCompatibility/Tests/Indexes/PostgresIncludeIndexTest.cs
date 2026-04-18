using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Indexes;

[SqlTest(SqlFeatureCategory.Indexes, "Test PostgreSQL INCLUDE columns in indexes", DatabaseType.PostgreSql)]
public class PostgresIncludeIndexTest : SqlTest
{
    protected override void SetupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = @"CREATE TABLE products_include (
                            id SERIAL PRIMARY KEY,
                            sku VARCHAR(50),
                            name VARCHAR(200),
                            price DECIMAL(10,2)
                        )";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "CREATE INDEX idx_sku_include ON products_include (sku) INCLUDE (name, price)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO products_include (sku, name, price) VALUES ('PROD001', 'Laptop', 999.99)";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "INSERT INTO products_include (sku, name, price) VALUES ('PROD002', 'Mouse', 29.99)";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "SELECT name, price FROM products_include WHERE sku = 'PROD001'";
        using DbDataReader reader = cmd.ExecuteReader();
        AssertTrue(reader.Read(), "Should find product");
        AssertEqual("Laptop", reader.GetString(0), "Should be Laptop");
        AssertEqual(999.99m, reader.GetDecimal(1), "Price should be 999.99");
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS products_include CASCADE";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());
    }
}
