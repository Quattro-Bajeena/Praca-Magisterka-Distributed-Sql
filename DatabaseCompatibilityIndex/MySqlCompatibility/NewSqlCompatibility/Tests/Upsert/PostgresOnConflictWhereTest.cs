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
                            quantity INT,
                            last_updated TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                        )";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO inventory_pg VALUES (1, 'Laptop', 10, CURRENT_TIMESTAMP)";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "INSERT INTO inventory_pg VALUES (2, 'Mouse', 50, CURRENT_TIMESTAMP)";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = @"INSERT INTO inventory_pg (product_id, name, quantity) 
                           VALUES (1, 'Laptop Pro', 5)
                           ON CONFLICT (product_id) DO UPDATE 
                           SET quantity = inventory_pg.quantity + EXCLUDED.quantity,
                               name = EXCLUDED.name,
                               last_updated = CURRENT_TIMESTAMP
                           WHERE inventory_pg.quantity > 0";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT quantity, name FROM inventory_pg WHERE product_id = 1";
        using (DbDataReader reader = cmd.ExecuteReader())
        {
            AssertTrue(reader.Read(), "Should find product 1");
            AssertEqual(15L, Convert.ToInt64(reader.GetValue(0)), "Quantity should be added (10 + 5)");
            AssertEqual("Laptop Pro", reader.GetString(1), "Name should be updated");
        }

        cmd.CommandText = "UPDATE inventory_pg SET quantity = 0 WHERE product_id = 2";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"INSERT INTO inventory_pg (product_id, name, quantity) 
                           VALUES (2, 'Wireless Mouse', 20)
                           ON CONFLICT (product_id) DO UPDATE 
                           SET quantity = inventory_pg.quantity + EXCLUDED.quantity,
                               name = EXCLUDED.name
                           WHERE inventory_pg.quantity > 0";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT quantity, name FROM inventory_pg WHERE product_id = 2";
        using (DbDataReader reader = cmd.ExecuteReader())
        {
            AssertTrue(reader.Read(), "Should find product 2");
            AssertEqual(0L, Convert.ToInt64(reader.GetValue(0)), "Quantity should remain 0 (WHERE clause not met)");
            AssertEqual("Mouse", reader.GetString(1), "Name should not be updated");
        }

        cmd.CommandText = @"INSERT INTO inventory_pg (product_id, name, quantity) 
                           VALUES (3, 'Keyboard', 30)
                           ON CONFLICT (product_id) DO UPDATE 
                           SET quantity = EXCLUDED.quantity";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT quantity FROM inventory_pg WHERE product_id = 3";
        object? qty = cmd.ExecuteScalar();
        AssertEqual(30L, Convert.ToInt64(qty!), "New product should be inserted");
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS inventory_pg CASCADE";
        cmd.ExecuteNonQuery();
    }
}
