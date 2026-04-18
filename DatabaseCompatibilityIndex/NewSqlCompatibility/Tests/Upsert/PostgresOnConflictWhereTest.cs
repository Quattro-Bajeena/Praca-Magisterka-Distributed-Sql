using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Upsert;

[SqlTest(SqlFeatureCategory.Upsert, "Test PostgreSQL ON CONFLICT with WHERE clause", DatabaseType.PostgreSql)]
public class PostgresOnConflictWhereTest : SqlTest
{
    protected override void SetupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = @"CREATE TABLE inventory_pg (
                            product_id INT PRIMARY KEY,
                            name VARCHAR(100),
                            quantity INT
                        )";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO inventory_pg VALUES (1, 'Laptop', 10)";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        // WHERE clause in DO UPDATE: update only when quantity > 0
        cmd.CommandText = @"INSERT INTO inventory_pg (product_id, name, quantity)
                           VALUES (1, 'Laptop Pro', 5)
                           ON CONFLICT (product_id) DO UPDATE
                           SET quantity = inventory_pg.quantity + EXCLUDED.quantity,
                               name = EXCLUDED.name
                           WHERE inventory_pg.quantity > 0";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT quantity, name FROM inventory_pg WHERE product_id = 1";
        using (DbDataReader reader = cmd.ExecuteReader())
        {
            AssertTrue(reader.Read(), "Should find product 1");
            AssertEqual(15L, Convert.ToInt64(reader.GetValue(0)), "Quantity should be 15 (10 + 5)");
            AssertEqual("Laptop Pro", reader.GetString(1), "Name should be updated");
        }

        // WHERE clause not met: update should be skipped
        cmd.CommandText = "UPDATE inventory_pg SET quantity = 0 WHERE product_id = 1";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"INSERT INTO inventory_pg (product_id, name, quantity)
                           VALUES (1, 'New Name', 20)
                           ON CONFLICT (product_id) DO UPDATE
                           SET quantity = inventory_pg.quantity + EXCLUDED.quantity,
                               name = EXCLUDED.name
                           WHERE inventory_pg.quantity > 0";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT name FROM inventory_pg WHERE product_id = 1";
        object? name = cmd.ExecuteScalar();
        AssertEqual("Laptop Pro", name?.ToString(), "Name should not be updated when WHERE clause is not met");
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS inventory_pg CASCADE";
        cmd.ExecuteNonQuery();
    }
}
