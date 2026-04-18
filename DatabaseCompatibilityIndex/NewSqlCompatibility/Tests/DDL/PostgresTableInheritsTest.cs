using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.TableDefinition;

[SqlTest(SqlFeatureCategory.DDL, "Test table inheritance with INHERITS clause", DatabaseType.PostgreSql)]
public class PostgresTableInheritsTest : SqlTest
{
    protected override void SetupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = @"CREATE TABLE vehicle (
            id    SERIAL PRIMARY KEY,
            make  TEXT NOT NULL,
            model TEXT NOT NULL
        )";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"CREATE TABLE car (
            doors INT NOT NULL DEFAULT 4
        ) INHERITS (vehicle)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO vehicle (make, model) VALUES ('Ford', 'Transit')";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "INSERT INTO car    (make, model, doors) VALUES ('Toyota', 'Corolla', 4)";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        // SELECT from parent includes child rows
        cmd.CommandText = "SELECT COUNT(*) FROM vehicle";
        long allCount = Convert.ToInt64(cmd.ExecuteScalar()!);
        AssertEqual(2L, allCount, "SELECT FROM vehicle should return rows from all child tables");

        // ONLY restricts to the parent table's own rows
        cmd.CommandText = "SELECT COUNT(*) FROM ONLY vehicle";
        long onlyCount = Convert.ToInt64(cmd.ExecuteScalar()!);
        AssertEqual(1L, onlyCount, "SELECT FROM ONLY vehicle should return only directly-inserted rows");
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "DROP TABLE IF EXISTS car CASCADE";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());

        cmd.CommandText = "DROP TABLE IF EXISTS vehicle CASCADE";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());
    }
}
