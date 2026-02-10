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
                            category VARCHAR(50),
                            price DECIMAL(10,2),
                            in_stock BOOLEAN
                        )";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO base_products (name, category, price, in_stock) VALUES ('Laptop', 'Electronics', 999, true)";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "INSERT INTO base_products (name, category, price, in_stock) VALUES ('Mouse', 'Electronics', 29, true)";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "INSERT INTO base_products (name, category, price, in_stock) VALUES ('Desk', 'Furniture', 299, false)";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "INSERT INTO base_products (name, category, price, in_stock) VALUES ('Chair', 'Furniture', 199, true)";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = @"CREATE VIEW available_products AS
                           SELECT id, name, category, price
                           FROM base_products
                           WHERE in_stock = true";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM available_products";
        object? count = cmd.ExecuteScalar();
        AssertEqual(3L, Convert.ToInt64(count!), "Should have 3 available products");

        cmd.CommandText = @"CREATE VIEW electronics_view AS
                           SELECT id, name, price
                           FROM available_products
                           WHERE category = 'Electronics'";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM electronics_view";
        count = cmd.ExecuteScalar();
        AssertEqual(2L, Convert.ToInt64(count!), "Should have 2 electronics in stock");

        cmd.CommandText = @"CREATE VIEW premium_electronics AS
                           SELECT id, name, price
                           FROM electronics_view
                           WHERE price > 100";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM premium_electronics";
        count = cmd.ExecuteScalar();
        AssertEqual(1L, Convert.ToInt64(count!), "Should have 1 premium electronics item");

        cmd.CommandText = @"SELECT COUNT(*) 
                           FROM information_schema.views 
                           WHERE table_schema = 'public' 
                           AND (table_name LIKE '%product%' OR table_name LIKE '%electronic%')";
        count = cmd.ExecuteScalar();
        AssertTrue(Convert.ToInt64(count!) >= 3, "Should have at least 3 views created");

        bool cascadeRequired = false;
        try
        {
            cmd.CommandText = "DROP VIEW available_products";
            cmd.ExecuteNonQuery();
        }
        catch
        {
            cascadeRequired = true;
        }
        AssertTrue(cascadeRequired, "Dropping base view without CASCADE should fail due to dependencies");

        cmd.CommandText = "DROP VIEW IF EXISTS available_products CASCADE";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"SELECT COUNT(*) FROM information_schema.views 
                           WHERE table_schema = 'public' AND table_name = 'electronics_view'";
        count = cmd.ExecuteScalar();
        AssertEqual(0L, Convert.ToInt64(count!), "Dependent view should be dropped with CASCADE");

        cmd.CommandText = @"SELECT COUNT(*) FROM information_schema.views 
                           WHERE table_schema = 'public' AND table_name = 'premium_electronics'";
        count = cmd.ExecuteScalar();
        AssertEqual(0L, Convert.ToInt64(count!), "Nested dependent view should also be dropped with CASCADE");

        cmd.CommandText = "SELECT COUNT(*) FROM base_products";
        count = cmd.ExecuteScalar();
        AssertEqual(4L, Convert.ToInt64(count!), "Base table should remain untouched");
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP VIEW IF EXISTS premium_electronics CASCADE";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());
        cmd.CommandText = "DROP VIEW IF EXISTS electronics_view CASCADE";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());
        cmd.CommandText = "DROP VIEW IF EXISTS available_products CASCADE";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());
        cmd.CommandText = "DROP TABLE IF EXISTS base_products CASCADE";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());
    }
}
