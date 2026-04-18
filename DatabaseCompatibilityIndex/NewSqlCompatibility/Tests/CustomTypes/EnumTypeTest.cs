using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.CustomTypes;

[SqlTest(SqlFeatureCategory.CustomTypes, "Test enumerated type enforces valid values")]
public class EnumTypeTest : SqlTest
{
    protected override void SetupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = @"CREATE TABLE orders_my (
            id     INT AUTO_INCREMENT PRIMARY KEY,
            status ENUM('pending', 'active', 'closed') NOT NULL
        )";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO orders_my (status) VALUES ('pending'), ('active'), ('closed')";
        cmd.ExecuteNonQuery();
    }

    protected override void SetupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "CREATE TYPE order_status AS ENUM ('pending', 'active', 'closed')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"CREATE TABLE orders_pg (
            id     SERIAL PRIMARY KEY,
            status order_status NOT NULL
        )";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO orders_pg (status) VALUES ('pending'), ('active'), ('closed')";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "SELECT COUNT(*) FROM orders_my WHERE status = 'active'";
        long activeCount = Convert.ToInt64(cmd.ExecuteScalar()!);
        AssertEqual(1L, activeCount, "Should find 1 active order");

        AssertThrows<Exception>(
            () =>
            {
                using DbCommand badCmd = connection.CreateCommand();
                badCmd.CommandText = "INSERT INTO orders_my (status) VALUES ('unknown')";
                badCmd.ExecuteNonQuery();
            },
            "Inserting an invalid ENUM value should be rejected by MySQL");
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "SELECT COUNT(*) FROM orders_pg WHERE status = 'active'";
        long activeCount = Convert.ToInt64(cmd.ExecuteScalar()!);
        AssertEqual(1L, activeCount, "Should find 1 active order");

        AssertThrows<Exception>(
            () =>
            {
                using DbCommand badCmd = connection.CreateCommand();
                badCmd.CommandText = "INSERT INTO orders_pg (status) VALUES ('unknown')";
                badCmd.ExecuteNonQuery();
            },
            "Inserting an invalid enum label should be rejected by PostgreSQL");
    }

    protected override void CleanupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS orders_my";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "DROP TABLE IF EXISTS orders_pg CASCADE";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());

        cmd.CommandText = "DROP TYPE IF EXISTS order_status CASCADE";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());
    }
}
