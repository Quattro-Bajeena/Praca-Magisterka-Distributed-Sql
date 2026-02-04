using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Indexes;

[SqlTest(SqlFeatureCategory.Indexes, "Test CREATE INDEX")]
public class CreateIndexTest : SqlTest
{
    protected override void SetupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "CREATE TABLE indexed_table (id INT PRIMARY KEY, email VARCHAR(100), name VARCHAR(50))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "CREATE INDEX idx_email ON indexed_table(email)";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "INSERT INTO indexed_table VALUES (1, 'test@example.com', 'Test User')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM indexed_table WHERE email = 'test@example.com'";
        object? count = cmd.ExecuteScalar();
        AssertEqual(1L, (long)count!, "Indexed query should work");
    }

    protected override void CleanupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP INDEX idx_email ON indexed_table";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "DROP TABLE indexed_table";
        cmd.ExecuteNonQuery();
    }
}
