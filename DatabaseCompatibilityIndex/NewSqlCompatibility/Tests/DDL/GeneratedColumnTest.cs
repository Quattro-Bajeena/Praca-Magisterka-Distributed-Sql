using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.TableDefinition;

[SqlTest(SqlFeatureCategory.DDL, "Test GENERATED (computed/virtual) columns update automatically")]
public class GeneratedColumnTest : SqlTest
{
    protected override void SetupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = @"CREATE TABLE order_items_my (
            id         INT AUTO_INCREMENT PRIMARY KEY,
            unit_price DECIMAL(10, 2) NOT NULL,
            quantity   INT            NOT NULL,
            subtotal   DECIMAL(10, 2) AS (unit_price * quantity) STORED
        )";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO order_items_my (unit_price, quantity) VALUES (19.99, 3)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO order_items_my (unit_price, quantity) VALUES (5.00, 10)";
        cmd.ExecuteNonQuery();
    }

    protected override void SetupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = @"CREATE TABLE order_items_pg (
            id         SERIAL PRIMARY KEY,
            unit_price DECIMAL(10, 2) NOT NULL,
            quantity   INT            NOT NULL,
            subtotal   DECIMAL(10, 2) GENERATED ALWAYS AS (unit_price * quantity) STORED
        )";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO order_items_pg (unit_price, quantity) VALUES (19.99, 3)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO order_items_pg (unit_price, quantity) VALUES (5.00, 10)";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        // Verify generated value on first row: 19.99 * 3 = 59.97
        cmd.CommandText = "SELECT subtotal FROM order_items_my WHERE id = 1";
        decimal subtotal1 = Convert.ToDecimal(cmd.ExecuteScalar()!);
        AssertEqual(59.97m, subtotal1, "subtotal for (19.99, 3) should be 59.97");

        // Update quantity and verify subtotal recomputes: 19.99 * 5 = 99.95
        cmd.CommandText = "UPDATE order_items_my SET quantity = 5 WHERE id = 1";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT subtotal FROM order_items_my WHERE id = 1";
        decimal subtotalUpdated = Convert.ToDecimal(cmd.ExecuteScalar()!);
        AssertEqual(99.95m, subtotalUpdated, "subtotal should recompute to 99.95 after quantity update");

        // SUM across both rows: 99.95 + 50.00 = 149.95
        cmd.CommandText = "SELECT SUM(subtotal) FROM order_items_my";
        decimal total = Convert.ToDecimal(cmd.ExecuteScalar()!);
        AssertEqual(149.95m, total, "SUM(subtotal) should equal 149.95");
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        // Verify generated value on first row: 19.99 * 3 = 59.97
        cmd.CommandText = "SELECT subtotal FROM order_items_pg WHERE id = 1";
        decimal subtotal1 = Convert.ToDecimal(cmd.ExecuteScalar()!);
        AssertEqual(59.97m, subtotal1, "subtotal for (19.99, 3) should be 59.97");

        // Update base column and verify stored generated column recomputes
        cmd.CommandText = "UPDATE order_items_pg SET quantity = 5 WHERE id = 1";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT subtotal FROM order_items_pg WHERE id = 1";
        decimal subtotalUpdated = Convert.ToDecimal(cmd.ExecuteScalar()!);
        AssertEqual(99.95m, subtotalUpdated, "subtotal should recompute to 99.95 after quantity update");

        // GENERATED ALWAYS: explicit insert of subtotal must be rejected
        AssertThrows<Exception>(
            () =>
            {
                using DbCommand badCmd = connection.CreateCommand();
                badCmd.CommandText = "INSERT INTO order_items_pg (unit_price, quantity, subtotal) VALUES (1.00, 1, 999.00)";
                badCmd.ExecuteNonQuery();
            },
            "Writing to a GENERATED ALWAYS column should be rejected");

        // SUM across both rows: 99.95 + 50.00 = 149.95
        cmd.CommandText = "SELECT SUM(subtotal) FROM order_items_pg";
        decimal total = Convert.ToDecimal(cmd.ExecuteScalar()!);
        AssertEqual(149.95m, total, "SUM(subtotal) should equal 149.95");
    }

    protected override void CleanupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS order_items_my";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS order_items_pg CASCADE";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());
    }
}
