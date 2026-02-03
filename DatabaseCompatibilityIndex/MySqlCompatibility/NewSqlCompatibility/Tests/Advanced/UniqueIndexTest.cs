using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Advanced;

[SqlTest(SqlFeatureCategory.Indexes, "Test UNIQUE INDEX", DatabaseType.MySql)]
public class UniqueIndexTest : SqlTest
{
    public override void Setup(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "CREATE TABLE unique_indexed (id INT PRIMARY KEY, code VARCHAR(20))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "CREATE UNIQUE INDEX uk_code ON unique_indexed(code)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO unique_indexed VALUES (1, 'ABC123')";
        cmd.ExecuteNonQuery();
    }

    public override string? Command => "INSERT INTO unique_indexed VALUES (2, 'ABC123')";

    public override void Cleanup(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP INDEX uk_code ON unique_indexed";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "DROP TABLE unique_indexed";
        cmd.ExecuteNonQuery();
    }
}
