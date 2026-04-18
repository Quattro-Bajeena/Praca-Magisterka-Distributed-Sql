using Npgsql;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.CustomTypes;

[SqlTest(SqlFeatureCategory.CustomTypes, "Test enumerated type enforces valid values")]
public class EnumTypeTest : SqlTest
{
    protected override void SetupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        // MySQL uses inline ENUM column definition
        cmd.CommandText = @"CREATE TABLE orders_my (
            id     INT AUTO_INCREMENT PRIMARY KEY,
            status ENUM('pending', 'active', 'closed') NOT NULL
        )";
        cmd.ExecuteNonQuery();

        string[] statuses = ["'pending'", "'active'", "'active'", "'closed'"];
        foreach (string s in statuses)
        {
            cmd.CommandText = $"INSERT INTO orders_my (status) VALUES ({s})";
            cmd.ExecuteNonQuery();
        }
    }

    protected override void SetupPg(DbConnection connection)
    {

    }

    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "SELECT COUNT(*) FROM orders_my WHERE status = 'active'";
        long activeCount = Convert.ToInt64(cmd.ExecuteScalar()!);
        AssertEqual(2L, activeCount, "Should find 2 active orders");

        cmd.CommandText = "SELECT COUNT(*) FROM orders_my WHERE status = 'pending'";
        long pendingCount = Convert.ToInt64(cmd.ExecuteScalar()!);
        AssertEqual(1L, pendingCount, "Should find 1 pending order");

        // ENUM enforces valid values — inserting an unlisted value must fail
        AssertThrows<Exception>(
            () =>
            {
                using DbCommand badCmd = connection.CreateCommand();
                badCmd.CommandText = "INSERT INTO orders_my (status) VALUES ('unknown')";
                badCmd.ExecuteNonQuery();
            },
            "Inserting an invalid ENUM value should be rejected by MySQL");

        // Verify the column type is an ENUM via information_schema
        cmd.CommandText = @"SELECT COLUMN_TYPE FROM information_schema.COLUMNS
                            WHERE TABLE_SCHEMA = DATABASE()
                              AND TABLE_NAME   = 'orders_my'
                              AND COLUMN_NAME  = 'status'";
        object? colType = cmd.ExecuteScalar();
        AssertTrue(colType?.ToString()?.StartsWith("enum") == true,
            "information_schema should report status column as enum type");
    }

    enum OrderStatus
    {
        pending,
        active,
        closed
    }
    // https://www.npgsql.org/doc/types/enums_and_composites.html?tabs=datasource
    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        NpgsqlDataSourceBuilder dataSourceBuilder = new NpgsqlDataSourceBuilder(_config.ConnectionString);
        dataSourceBuilder.MapEnum<OrderStatus>("order_status");
        using NpgsqlDataSource dataSource = dataSourceBuilder.Build();
        NpgsqlConnection newConnection = dataSource.OpenConnection();

        /// Setup
        using DbCommand cmd = newConnection.CreateCommand();

        cmd.CommandText = "CREATE TYPE order_status AS ENUM ('pending', 'active', 'closed')";
        cmd.ExecuteNonQuery();

        newConnection.ReloadTypes();


        dataSourceBuilder.MapEnum<OrderStatus>("order_status");
        using NpgsqlDataSource dataSource2 = dataSourceBuilder.Build();
        NpgsqlConnection newConnection1 = dataSource2.OpenConnection();

        using DbCommand cmd2 = newConnection1.CreateCommand();
        cmd2.CommandText = @"CREATE TABLE orders_pg (
            id     SERIAL PRIMARY KEY,
            status order_status NOT NULL
        )";
        cmd2.ExecuteNonQuery();

        string[] statuses = ["'pending'", "'active'", "'active'", "'closed'"];
        foreach (string s in statuses)
        {
            cmd2.CommandText = $"INSERT INTO orders_pg (status) VALUES ({s})";
            //cmd.Parameters.Clear();
            //cmd.Parameters.Add(new NpgsqlParameter("status", Enum.Parse<OrderStatus>(s.Trim('\''))));
            cmd2.ExecuteNonQuery();
        }
        ///

        cmd2.CommandText = "SELECT COUNT(*) FROM orders_pg WHERE status = 'active'";
        long activeCount = Convert.ToInt64(cmd2.ExecuteScalar()!);
        AssertEqual(2L, activeCount, "Should find 2 active orders");

        cmd2.CommandText = "SELECT COUNT(*) FROM orders_pg WHERE status = 'pending'";
        long pendingCount = Convert.ToInt64(cmd2.ExecuteScalar()!);
        AssertEqual(1L, pendingCount, "Should find 1 pending order");

        // ENUM order is declaration order: 'pending' < 'active' < 'closed'
        cmd2.CommandText = "SELECT status FROM orders_pg ORDER BY status LIMIT 1";
        using DbDataReader reader = cmd2.ExecuteReader();

        reader.Read();
        OrderStatus first = reader.GetFieldValue<OrderStatus>(0);
        AssertEqual(OrderStatus.pending, first, "Lowest ENUM value by declaration order should be 'pending'");
        reader.Close();


        // ENUM rejects unlisted values
        AssertThrows<Exception>(
            () =>
            {
                using DbCommand badCmd = newConnection1.CreateCommand();
                badCmd.CommandText = "INSERT INTO orders_pg (status) VALUES ('unknown')";
                badCmd.ExecuteNonQuery();
            },
            "Inserting an invalid enum label should be rejected by PostgreSQL");

        // Verify the type is registered in pg_type with typtype = 'e' (enum)
        cmd2.CommandText = "SELECT typtype FROM pg_type WHERE typname = 'order_status'";
        object? typtype = cmd2.ExecuteScalar();
        AssertEqual("e", typtype?.ToString(), "pg_type should show typtype 'e' for an enum type");

        // Verify enum labels count
        cmd2.CommandText = "SELECT COUNT(*) FROM pg_enum WHERE enumtypid = 'order_status'::regtype";
        long labelCount = Convert.ToInt64(cmd2.ExecuteScalar()!);
        AssertEqual(3L, labelCount, "order_status enum should have exactly 3 labels");
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
