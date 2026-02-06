using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Indexes;

[SqlTest(SqlFeatureCategory.Indexes, "Test composite INDEXusing NSCI.Testing;")]
public class CompositeIndexTest : SqlTest
{
    protected override void SetupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "CREATE TABLE composite_idx (id INT PRIMARY KEY, first_name VARCHAR(50), last_name VARCHAR(50))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "CREATE INDEX idx_name ON composite_idx(first_name, last_name)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO composite_idx VALUES (1, 'John', 'Doe'), (2, 'Jane', 'Doe')";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "SELECT COUNT(*) FROM composite_idx WHERE first_name = 'John' AND last_name = 'Doe'";
        object? count = cmd.ExecuteScalar();
        AssertEqual(1L, (long)count!, "Composite index should work");
    }

    protected override void CleanupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP INDEX idx_name ON composite_idx";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "DROP TABLE composite_idx";
        cmd.ExecuteNonQuery();
    }

    protected override void SetupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "CREATE TABLE composite_idx (id INT PRIMARY KEY, first_name VARCHAR(50), last_name VARCHAR(50))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "CREATE INDEX idx_name ON composite_idx(first_name, last_name)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO composite_idx VALUES (1, 'John', 'Doe'), (2, 'Jane', 'Doe')";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "SELECT COUNT(*) FROM composite_idx WHERE first_name = 'John' AND last_name = 'Doe'";
        object? count = cmd.ExecuteScalar();
        AssertEqual(1L, (long)count!, "Composite index should work");
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE composite_idx";
        cmd.ExecuteNonQuery();
    }
}
