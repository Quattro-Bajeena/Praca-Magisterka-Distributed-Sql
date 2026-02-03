using NSCI.Testing;using NSCI.Configuration;
using System.Data.Common;

namespace NSCI.Tests.Locking;

[SqlTest(SqlFeatureCategory.Locking, "Test FOR UPDATE with WHERE clause", DatabaseType.MySql)]
public class SelectForUpdateWithWhereTest : SqlTest
{
    public override void Setup(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "CREATE TABLE items (item_id INT PRIMARY KEY, status VARCHAR(20), value INT)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO items VALUES (1, 'active', 100), (2, 'inactive', 50), (3, 'active', 75)";
        cmd.ExecuteNonQuery();
    }

    public override void Execute(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "BEGIN";
        cmd.ExecuteNonQuery();

        // Lock only active items
        cmd.CommandText = "SELECT item_id, value FROM items WHERE status = 'active' FOR UPDATE";
        using DbDataReader reader = cmd.ExecuteReader();
        int count = 0;
        while (reader.Read())
        {
            count++;
        }
        AssertEqual(2, count, "Should lock 2 active items");

        cmd.CommandText = "COMMIT";
        cmd.ExecuteNonQuery();
    }

    public override void Cleanup(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE items";
        cmd.ExecuteNonQuery();
    }
}
