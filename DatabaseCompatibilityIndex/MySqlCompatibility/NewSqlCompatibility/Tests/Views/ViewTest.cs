using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Views;

[SqlTest(SqlFeatureCategory.Views, "Test CREATE and SELECT from VIEW")]
public class ViewTest : SqlTest
{
    protected override void SetupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "CREATE TABLE view_base (id INT PRIMARY KEY, name VARCHAR(50), status VARCHAR(20))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO view_base VALUES (1, 'Active Item', 'active'), (2, 'Inactive Item', 'inactive')";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "CREATE VIEW active_items AS SELECT id, name FROM view_base WHERE status = 'active'";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM active_items";
        object? count = cmd.ExecuteScalar();
        AssertEqual(1L, (long)count!, "VIEW should work correctly");
    }

    protected override void CleanupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP VIEW IF EXISTS active_items";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());

        cmd.CommandText = "DROP TABLE view_base";
        cmd.ExecuteNonQuery();
    }

    protected override void SetupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "CREATE TABLE view_base (id INT PRIMARY KEY, name VARCHAR(50), status VARCHAR(20))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO view_base VALUES (1, 'Active Item', 'active'), (2, 'Inactive Item', 'inactive')";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "CREATE VIEW active_items AS SELECT id, name FROM view_base WHERE status = 'active'";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM active_items";
        object? count = cmd.ExecuteScalar();
        AssertEqual(1L, (long)count!, "VIEW should work correctly");

        cmd.CommandText = "SELECT name FROM active_items WHERE id = 1";
        object? name = cmd.ExecuteScalar();
        AssertEqual("Active Item", name?.ToString(), "Should retrieve correct data from view");
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP VIEW IF EXISTS active_items CASCADE";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());

        cmd.CommandText = "DROP TABLE IF EXISTS view_base CASCADE";
        cmd.ExecuteNonQuery();
    }
}
