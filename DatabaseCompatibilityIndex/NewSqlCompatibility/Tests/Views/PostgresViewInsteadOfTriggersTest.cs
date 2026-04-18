using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Views;

[SqlTest(SqlFeatureCategory.Views, "Test PostgreSQL INSTEAD OF triggers on views", DatabaseType.PostgreSql)]
public class PostgresViewInsteadOfTriggersTest : SqlTest
{
    protected override void SetupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = @"CREATE TABLE products_trigger (
                            id SERIAL PRIMARY KEY,
                            name VARCHAR(100),
                            price DECIMAL(10,2)
                        )";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO products_trigger (name, price) VALUES ('Item A', 100)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"CREATE VIEW products_view AS
                           SELECT id, name, price FROM products_trigger";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"CREATE OR REPLACE FUNCTION view_insert_fn() RETURNS TRIGGER AS $$
                           BEGIN
                               INSERT INTO products_trigger (name, price) VALUES (NEW.name, NEW.price);
                               RETURN NEW;
                           END;
                           $$ LANGUAGE plpgsql";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"CREATE TRIGGER instead_of_insert
                           INSTEAD OF INSERT ON products_view
                           FOR EACH ROW EXECUTE FUNCTION view_insert_fn()";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "INSERT INTO products_view (name, price) VALUES ('Item B', 200)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM products_trigger";
        object? count = cmd.ExecuteScalar();
        AssertEqual(2L, Convert.ToInt64(count!), "INSTEAD OF trigger should insert into base table");

        cmd.CommandText = "SELECT name FROM products_trigger ORDER BY id DESC LIMIT 1";
        object? name = cmd.ExecuteScalar();
        AssertEqual("Item B", name?.ToString(), "New product should be in base table");
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP VIEW IF EXISTS products_view CASCADE";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());
        cmd.CommandText = "DROP FUNCTION IF EXISTS view_insert_fn() CASCADE";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());
        cmd.CommandText = "DROP TABLE IF EXISTS products_trigger CASCADE";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());
    }
}
