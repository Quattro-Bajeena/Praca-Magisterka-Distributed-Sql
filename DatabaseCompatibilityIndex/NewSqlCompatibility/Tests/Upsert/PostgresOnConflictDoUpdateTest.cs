using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Upsert;

[SqlTest(SqlFeatureCategory.Upsert, "Test PostgreSQL INSERT...ON CONFLICT DO UPDATE", DatabaseType.PostgreSql)]
public class PostgresOnConflictDoUpdateTest : SqlTest
{
    protected override void SetupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = @"CREATE TABLE upsert_pg_test (
                            id INT PRIMARY KEY,
                            name VARCHAR(50),
                            email VARCHAR(100),
                            updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                        )";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO upsert_pg_test (id, name, email) VALUES (1, 'Alice', 'alice@example.com')";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = @"INSERT INTO upsert_pg_test (id, name, email) 
                           VALUES (2, 'Bob', 'bob@example.com')
                           ON CONFLICT (id) DO UPDATE 
                           SET name = EXCLUDED.name, email = EXCLUDED.email";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM upsert_pg_test";
        object? count = cmd.ExecuteScalar();
        AssertEqual(2L, Convert.ToInt64(count!), "Should have 2 rows");

        cmd.CommandText = @"INSERT INTO upsert_pg_test (id, name, email) 
                           VALUES (1, 'Alice Updated', 'alice_new@example.com')
                           ON CONFLICT (id) DO UPDATE 
                           SET name = EXCLUDED.name, 
                               email = EXCLUDED.email,
                               updated_at = CURRENT_TIMESTAMP";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT name, email FROM upsert_pg_test WHERE id = 1";
        using (DbDataReader reader = cmd.ExecuteReader())
        {
            AssertTrue(reader.Read(), "Should have data for id=1");
            AssertEqual("Alice Updated", reader.GetString(0), "Name should be updated");
            AssertEqual("alice_new@example.com", reader.GetString(1), "Email should be updated");
        }

        cmd.CommandText = @"INSERT INTO upsert_pg_test (id, name, email) 
                           VALUES (3, 'Charlie', 'charlie@example.com')
                           ON CONFLICT (id) DO NOTHING";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM upsert_pg_test";
        count = cmd.ExecuteScalar();
        AssertEqual(3L, Convert.ToInt64(count!), "Should have 3 rows after DO NOTHING insert");

        cmd.CommandText = @"INSERT INTO upsert_pg_test (id, name, email) 
                           VALUES (3, 'Charlie Updated', 'charlie_new@example.com')
                           ON CONFLICT (id) DO NOTHING";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT name FROM upsert_pg_test WHERE id = 3";
        object? name = cmd.ExecuteScalar();
        AssertEqual("Charlie", name?.ToString(), "Name should not be updated with DO NOTHING");
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS upsert_pg_test CASCADE";
        cmd.ExecuteNonQuery();
    }
}
