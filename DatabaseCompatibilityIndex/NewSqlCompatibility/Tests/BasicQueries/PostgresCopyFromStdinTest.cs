using Npgsql;
using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.BasicQueries;

[SqlTest(SqlFeatureCategory.BasicQueries, "Bulk insert rows using COPY FROM STDIN with CSV format", DatabaseType.PostgreSql)]
public class PostgresCopyFromStdinTest : SqlTest
{
    private const int RowCount = 1000;

    protected override void SetupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = @"CREATE TABLE bulk_copy_data (
            id INT NOT NULL,
            name VARCHAR(100) NOT NULL,
            value DECIMAL(10,2) NOT NULL
        )";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        NpgsqlConnection npgsqlConnection = (NpgsqlConnection)connection;

        using (TextWriter writer = npgsqlConnection.BeginTextImport(
            "COPY bulk_copy_data (id, name, value) FROM STDIN (FORMAT CSV)"))
        {
            for (int i = 1; i <= RowCount; i++)
            {
                writer.WriteLine($"{i},Item{i},{(i * 1.5m):F2}");
            }
        }

        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "SELECT COUNT(*) FROM bulk_copy_data";
        object? count = cmd.ExecuteScalar();
        AssertEqual((long)RowCount, Convert.ToInt64(count!), $"Should have {RowCount} rows after COPY FROM STDIN");

        cmd.CommandText = "SELECT name FROM bulk_copy_data WHERE id = 500";
        object? name = cmd.ExecuteScalar();
        AssertEqual("Item500", (string)name!, "Row 500 should have name 'Item500'");

        cmd.CommandText = "SELECT value FROM bulk_copy_data WHERE id = 1";
        object? firstValue = cmd.ExecuteScalar();
        AssertEqual(1.50m, Convert.ToDecimal(firstValue!), "First row value should be 1.50");
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS bulk_copy_data";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());
    }
}
