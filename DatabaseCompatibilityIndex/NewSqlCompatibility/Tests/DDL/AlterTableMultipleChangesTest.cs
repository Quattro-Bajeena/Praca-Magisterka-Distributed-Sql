using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.TableDefinition;

[SqlTest(SqlFeatureCategory.DDL, "Test ALTER TABLE with multiple changes to same column")]
public class AlterTableMultipleChangesTest : SqlTest
{
    protected override void SetupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "CREATE TABLE alter_multi (id INT PRIMARY KEY, name VARCHAR(50), age INT, status VARCHAR(20))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO alter_multi VALUES (1, 'Alice', 30, 'active')";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "ALTER TABLE alter_multi MODIFY COLUMN name VARCHAR(100), ADD INDEX idx_name (name)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT name FROM alter_multi WHERE id = 1";
        object? name = cmd.ExecuteScalar();
        AssertEqual("Alice", name?.ToString(), "Column should still contain data after modify");

        cmd.CommandText = "ALTER TABLE alter_multi DROP COLUMN status";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "ALTER TABLE alter_multi ADD COLUMN email VARCHAR(100), ADD COLUMN phone VARCHAR(20)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "UPDATE alter_multi SET email = 'alice@example.com' WHERE id = 1";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT email FROM alter_multi WHERE id = 1";
        object? email = cmd.ExecuteScalar();
        AssertEqual("alice@example.com", email?.ToString(), "New column should work");
    }

    protected override void CleanupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS alter_multi";
        cmd.ExecuteNonQuery();
    }

    protected override void SetupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "CREATE TABLE alter_multi (id INT PRIMARY KEY, name VARCHAR(50), age INT, status VARCHAR(20))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO alter_multi VALUES (1, 'Alice', 30, 'active')";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "ALTER TABLE alter_multi ALTER COLUMN name TYPE VARCHAR(100)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "CREATE INDEX idx_name ON alter_multi(name)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT name FROM alter_multi WHERE id = 1";
        object? name = cmd.ExecuteScalar();
        AssertEqual("Alice", name?.ToString(), "Column should still contain data after alter");

        cmd.CommandText = "ALTER TABLE alter_multi DROP COLUMN status";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "ALTER TABLE alter_multi ADD COLUMN email VARCHAR(100), ADD COLUMN phone VARCHAR(20)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "UPDATE alter_multi SET email = 'alice@example.com' WHERE id = 1";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT email FROM alter_multi WHERE id = 1";
        object? email = cmd.ExecuteScalar();
        AssertEqual("alice@example.com", email?.ToString(), "New column should work");
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS alter_multi";
        cmd.ExecuteNonQuery();
    }
}
