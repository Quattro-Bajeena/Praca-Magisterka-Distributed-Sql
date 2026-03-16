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

        cmd.CommandText = @"CREATE TABLE audit_log (
                            id SERIAL PRIMARY KEY,
                            action VARCHAR(50),
                            record_id INT,
                            timestamp TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                        )";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"CREATE TABLE products_trigger (
                            id SERIAL PRIMARY KEY,
                            name VARCHAR(100),
                            price DECIMAL(10,2)
                        )";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO products_trigger (name, price) VALUES ('Item A', 100)";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "INSERT INTO products_trigger (name, price) VALUES ('Item B', 200)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"CREATE VIEW products_with_audit AS
                           SELECT id, name, price FROM products_trigger";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"CREATE OR REPLACE FUNCTION audit_insert() RETURNS TRIGGER AS $$
                           BEGIN
                               INSERT INTO products_trigger (name, price) VALUES (NEW.name, NEW.price);
                               INSERT INTO audit_log (action, record_id) VALUES ('INSERT', currval('products_trigger_id_seq'));
                               RETURN NEW;
                           END;
                           $$ LANGUAGE plpgsql";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"CREATE TRIGGER instead_of_insert
                           INSTEAD OF INSERT ON products_with_audit
                           FOR EACH ROW EXECUTE FUNCTION audit_insert()";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "SELECT COUNT(*) FROM products_trigger";
        object? initialCount = cmd.ExecuteScalar();
        AssertEqual(2L, Convert.ToInt64(initialCount!), "Should start with 2 products");

        cmd.CommandText = "INSERT INTO products_with_audit (name, price) VALUES ('Item C', 300)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM products_trigger";
        object? count = cmd.ExecuteScalar();
        AssertEqual(3L, Convert.ToInt64(count!), "INSTEAD OF trigger should insert into base table");

        cmd.CommandText = "SELECT COUNT(*) FROM audit_log WHERE action = 'INSERT'";
        object? auditCount = cmd.ExecuteScalar();
        AssertEqual(1L, Convert.ToInt64(auditCount!), "Audit log should record the insert");

        cmd.CommandText = "SELECT record_id FROM audit_log WHERE action = 'INSERT' ORDER BY id DESC LIMIT 1";
        object? recordId = cmd.ExecuteScalar();
        AssertTrue(recordId != null && Convert.ToInt32(recordId) > 0, "Audit should have valid record_id");

        cmd.CommandText = "SELECT name FROM products_trigger ORDER BY id DESC LIMIT 1";
        object? name = cmd.ExecuteScalar();
        AssertEqual("Item C", name?.ToString(), "New product should be in base table");

        cmd.CommandText = @"CREATE OR REPLACE FUNCTION audit_update() RETURNS TRIGGER AS $$
                           BEGIN
                               UPDATE products_trigger SET name = NEW.name, price = NEW.price WHERE id = OLD.id;
                               INSERT INTO audit_log (action, record_id) VALUES ('UPDATE', OLD.id);
                               RETURN NEW;
                           END;
                           $$ LANGUAGE plpgsql";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"CREATE TRIGGER instead_of_update
                           INSTEAD OF UPDATE ON products_with_audit
                           FOR EACH ROW EXECUTE FUNCTION audit_update()";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "UPDATE products_with_audit SET price = 350 WHERE name = 'Item C'";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT price FROM products_trigger WHERE name = 'Item C'";
        object? price = cmd.ExecuteScalar();
        AssertEqual(350m, Convert.ToDecimal(price!), "UPDATE through view should work");

        cmd.CommandText = "SELECT COUNT(*) FROM audit_log WHERE action = 'UPDATE'";
        object? updateAuditCount = cmd.ExecuteScalar();
        AssertEqual(1L, Convert.ToInt64(updateAuditCount!), "Audit log should record the update");
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP VIEW IF EXISTS products_with_audit CASCADE";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());
        cmd.CommandText = "DROP FUNCTION IF EXISTS audit_insert() CASCADE";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());
        cmd.CommandText = "DROP FUNCTION IF EXISTS audit_update() CASCADE";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());
        cmd.CommandText = "DROP TABLE IF EXISTS products_trigger CASCADE";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());
        cmd.CommandText = "DROP TABLE IF EXISTS audit_log CASCADE";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());
    }
}
