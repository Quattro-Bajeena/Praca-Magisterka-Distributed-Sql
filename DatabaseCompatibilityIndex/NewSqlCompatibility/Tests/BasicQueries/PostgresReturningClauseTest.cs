using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.BasicQueries;

[SqlTest(SqlFeatureCategory.BasicQueries, "Test PostgreSQL RETURNING clause with INSERT/UPDATE/DELETE", DatabaseType.PostgreSql)]
public class PostgresReturningClauseTest : SqlTest
{
    protected override void SetupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = @"CREATE TABLE products_returning (
                            id SERIAL PRIMARY KEY,
                            name VARCHAR(100),
                            price DECIMAL(10,2),
                            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                        )";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = @"INSERT INTO products_returning (name, price) 
                           VALUES ('Laptop', 999.99), ('Mouse', 29.99), ('Keyboard', 79.99)
                           RETURNING id, name, price";

        int insertedCount = 0;
        int firstId = 0;
        using (DbDataReader reader = cmd.ExecuteReader())
        {
            while (reader.Read())
            {
                insertedCount++;
                int id = reader.GetInt32(0);
                string name = reader.GetString(1);
                decimal price = reader.GetDecimal(2);

                AssertTrue(id > 0, "ID should be generated");
                AssertTrue(name.Length > 0, "Name should not be empty");
                AssertTrue(price > 0, "Price should be positive");

                if (insertedCount == 1)
                {
                    firstId = id;
                    AssertEqual("Laptop", name, "First item should be Laptop");
                }
            }
        }
        AssertEqual(3, insertedCount, "Should return 3 inserted rows");

        cmd.CommandText = @"UPDATE products_returning 
                           SET price = price * 0.9 
                           WHERE name = 'Mouse'
                           RETURNING id, name, price, created_at";

        using (DbDataReader reader = cmd.ExecuteReader())
        {
            AssertTrue(reader.Read(), "Should return updated row");
            int id = reader.GetInt32(0);
            string name = reader.GetString(1);
            decimal price = reader.GetDecimal(2);

            AssertEqual("Mouse", name, "Should update Mouse");
            AssertEqual(26.99m, Math.Round(price, 2), "Price should be reduced by 10%");
        }

        cmd.CommandText = @"DELETE FROM products_returning 
                           WHERE price < 50
                           RETURNING id, name, price";

        int deletedCount = 0;
        using (DbDataReader reader = cmd.ExecuteReader())
        {
            while (reader.Read())
            {
                deletedCount++;
                string name = reader.GetString(1);
                decimal price = reader.GetDecimal(2);
                AssertTrue(price < 50, "Deleted items should have price < 50");
            }
        }
        AssertTrue(deletedCount >= 1, "Should delete at least 1 row");

        cmd.CommandText = "SELECT COUNT(*) FROM products_returning";
        object? remainingCount = cmd.ExecuteScalar();
        AssertTrue(Convert.ToInt64(remainingCount!) < 3, "Some rows should be deleted");

        cmd.CommandText = @"INSERT INTO products_returning (name, price) 
                           VALUES ('Monitor', 299.99)
                           RETURNING *";

        using (DbDataReader reader = cmd.ExecuteReader())
        {
            AssertTrue(reader.Read(), "Should return all columns with *");
            AssertTrue(reader.FieldCount >= 4, "Should have at least 4 columns (id, name, price, created_at)");
            int id = reader.GetInt32(0);
            string name = reader.GetString(1);
            AssertTrue(id > 0, "ID should be generated");
            AssertEqual("Monitor", name, "Should insert Monitor");
        }
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS products_returning CASCADE";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());
    }
}
