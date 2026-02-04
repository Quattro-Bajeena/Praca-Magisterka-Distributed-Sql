using MySqlConnector;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Indexes;

[SqlTest(SqlFeatureCategory.Indexes, "Test UNIQUE INDEX")]
public class UniqueIndexTest : SqlTest
{
    protected override void SetupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "CREATE TABLE unique_indexed (id INT PRIMARY KEY, code VARCHAR(20))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "CREATE UNIQUE INDEX uk_code ON unique_indexed(code)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO unique_indexed VALUES (1, 'ABC123')";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "INSERT INTO unique_indexed VALUES (2, 'ABC123')";
        AssertThrows<MySqlException>(() => cmd.ExecuteNonQuery(), "Should throw exception for unique index violation");
    }

    protected override void CleanupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP INDEX uk_code ON unique_indexed";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "DROP TABLE unique_indexed";
        cmd.ExecuteNonQuery();
    }
}
