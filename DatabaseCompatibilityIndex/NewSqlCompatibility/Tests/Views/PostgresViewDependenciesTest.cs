using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Views;

[SqlTest(SqlFeatureCategory.Views, "Test PostgreSQL view dependencies and CASCADE", DatabaseType.PostgreSql)]
public class PostgresViewDependenciesTest : SqlTest
{
    protected override void SetupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = @"CREATE TABLE base_products (
                            id SERIAL PRIMARY KEY,
                            name VARCHAR(100),
                            in_stock BOOLEAN
                        )";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO base_products (name, in_stock) VALUES ('Laptop', true)";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "INSERT INTO base_products (name, in_stock) VALUES ('Desk', false)";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = @"CREATE VIEW available_products AS
                           SELECT id, name FROM base_products WHERE in_stock = true";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"CREATE VIEW featured_products AS
                           SELECT id, name FROM available_products";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM featured_products";
        object? count = cmd.ExecuteScalar();
        AssertEqual(1L, Convert.ToInt64(count!), "Dependent view should show 1 available product");

        // DROP without CASCADE should fail because featured_products depends on available_products
        AssertThrows<Exception>(
            () =>
            {
                using DbCommand badCmd = connection.CreateCommand();
                badCmd.CommandText = "DROP VIEW available_products";
                badCmd.ExecuteNonQuery();
            },
            "Dropping base view without CASCADE should fail due to dependencies");

        // DROP CASCADE should remove both views
        cmd.CommandText = "DROP VIEW available_products CASCADE";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"SELECT COUNT(*) FROM information_schema.views
                           WHERE table_schema = 'public' AND table_name = 'featured_products'";
        count = cmd.ExecuteScalar();
        AssertEqual(0L, Convert.ToInt64(count!), "Dependent view should be dropped with CASCADE");

        cmd.CommandText = "SELECT COUNT(*) FROM base_products";
        count = cmd.ExecuteScalar();
        AssertEqual(2L, Convert.ToInt64(count!), "Base table should remain untouched");
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP VIEW IF EXISTS featured_products CASCADE";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());
        cmd.CommandText = "DROP VIEW IF EXISTS available_products CASCADE";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());
        cmd.CommandText = "DROP TABLE IF EXISTS base_products CASCADE";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());
    }
}
