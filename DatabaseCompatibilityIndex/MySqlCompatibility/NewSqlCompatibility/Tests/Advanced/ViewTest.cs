using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Advanced;

[SqlTest(SqlFeatureCategory.Views, "Test CREATE and SELECT from VIEW", DatabaseType.MySql)]
public class ViewTest : SqlTest
{
    public override void Setup(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "CREATE TABLE view_base (id INT PRIMARY KEY, name VARCHAR(50), status VARCHAR(20))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO view_base VALUES (1, 'Active Item', 'active'), (2, 'Inactive Item', 'inactive')";
        cmd.ExecuteNonQuery();
    }

    public override void Execute(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "CREATE VIEW active_items AS SELECT id, name FROM view_base WHERE status = 'active'";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM active_items";
        object? count = cmd.ExecuteScalar();
        AssertEqual(1L, (long)count!, "VIEW should work correctly");
    }

    public override void Cleanup(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP VIEW IF EXISTS active_items";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());

        cmd.CommandText = "DROP TABLE view_base";
        cmd.ExecuteNonQuery();
    }
}
