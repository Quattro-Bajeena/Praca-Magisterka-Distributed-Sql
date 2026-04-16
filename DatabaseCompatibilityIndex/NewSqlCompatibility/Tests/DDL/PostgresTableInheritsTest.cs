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
            model TEXT NOT NULL,
            year  INT  NOT NULL
        )";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"CREATE TABLE car (
            doors INT NOT NULL DEFAULT 4
        ) INHERITS (vehicle)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"CREATE TABLE truck (
            payload_tons NUMERIC(5, 2) NOT NULL
        ) INHERITS (vehicle)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO vehicle  (make, model, year)                VALUES ('Ford',   'Transit',  2020)";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "INSERT INTO car      (make, model, year, doors)          VALUES ('Toyota', 'Corolla',  2022, 4)";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "INSERT INTO truck    (make, model, year, payload_tons)   VALUES ('Volvo',  'FH16',     2021, 20.5)";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        // SELECT from parent includes all child rows
        cmd.CommandText = "SELECT COUNT(*) FROM vehicle";
        long allCount = Convert.ToInt64(cmd.ExecuteScalar()!);
        AssertEqual(3L, allCount, "SELECT FROM vehicle should return rows from all child tables");

        // ONLY restricts to the parent table's own rows
        cmd.CommandText = "SELECT COUNT(*) FROM ONLY vehicle";
        long onlyCount = Convert.ToInt64(cmd.ExecuteScalar()!);
        AssertEqual(1L, onlyCount, "SELECT FROM ONLY vehicle should return only directly-inserted rows");

        // Each child table contains exactly its own row
        cmd.CommandText = "SELECT COUNT(*) FROM car";
        long carCount = Convert.ToInt64(cmd.ExecuteScalar()!);
        AssertEqual(1L, carCount, "car table should contain 1 row");

        cmd.CommandText = "SELECT COUNT(*) FROM truck";
        long truckCount = Convert.ToInt64(cmd.ExecuteScalar()!);
        AssertEqual(1L, truckCount, "truck table should contain 1 row");

        // pg_inherits records both child relationships
        cmd.CommandText = "SELECT COUNT(*) FROM pg_inherits WHERE inhparent = 'vehicle'::regclass";
        long inheritCount = Convert.ToInt64(cmd.ExecuteScalar()!);
        AssertEqual(2L, inheritCount, "pg_inherits should show 2 children of vehicle");
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "DROP TABLE IF EXISTS car CASCADE";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());

        cmd.CommandText = "DROP TABLE IF EXISTS truck CASCADE";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());

        cmd.CommandText = "DROP TABLE IF EXISTS vehicle CASCADE";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());
    }
}
