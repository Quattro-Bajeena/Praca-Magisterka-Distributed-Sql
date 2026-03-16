using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Locking;

[SqlTest(SqlFeatureCategory.Locking, "Test FOR UPDATE with WHERE clause")]
public class SelectForUpdateWithWhereTest : SqlTest
{
    protected override void SetupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "CREATE TABLE items (item_id INT PRIMARY KEY, status VARCHAR(20), value INT)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO items VALUES (1, 'active', 100), (2, 'inactive', 50), (3, 'active', 75)";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "BEGIN";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT item_id, value FROM items WHERE status = 'active' FOR UPDATE";
        int count = 0;
        using (DbDataReader reader = cmd.ExecuteReader())
        {
            while (reader.Read())
            {
                count++;
            }
        }
        AssertEqual(2, count, "Should lock 2 active items");

        cmd.CommandText = "COMMIT";
        cmd.ExecuteNonQuery();
    }

    protected override void CleanupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE items";
        cmd.ExecuteNonQuery();
    }

    protected override void SetupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "CREATE TABLE items (item_id INT PRIMARY KEY, status VARCHAR(20), value INT)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO items VALUES (1, 'active', 100), (2, 'inactive', 50), (3, 'active', 75)";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "BEGIN";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT item_id, value FROM items WHERE status = 'active' FOR UPDATE";
        int count = 0;
        using (DbDataReader reader = cmd.ExecuteReader())
        {
            while (reader.Read())
            {
                count++;
            }
        }
        AssertEqual(2, count, "Should lock 2 active items");

        cmd.CommandText = "COMMIT";
        cmd.ExecuteNonQuery();
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS items";
        cmd.ExecuteNonQuery();
    }
}
