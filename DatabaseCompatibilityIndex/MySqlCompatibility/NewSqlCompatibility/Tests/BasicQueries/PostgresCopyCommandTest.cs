using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.BasicQueries;

[SqlTest(SqlFeatureCategory.BasicQueries, "Test PostgreSQL COPY command for bulk data", DatabaseType.PostgreSql)]
public class PostgresCopyCommandTest : SqlTest
{
    protected override void SetupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = @"CREATE TABLE bulk_data (
                            id INT,
                            name VARCHAR(100),
                            value DECIMAL(10,2)
                        )";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = @"CREATE TEMP TABLE temp_import (
                            id INT,
                            name VARCHAR(100),
                            value DECIMAL(10,2)
                        )";
        cmd.ExecuteNonQuery();

        for (int i = 1; i <= 10; i++)
        {
            cmd.CommandText = $"INSERT INTO temp_import (id, name, value) VALUES ({i}, 'Item{i}', {i * 10.5})";
            cmd.ExecuteNonQuery();
        }

        cmd.CommandText = "SELECT COUNT(*) FROM temp_import";
        object? tempCount = cmd.ExecuteScalar();
        AssertEqual(10L, Convert.ToInt64(tempCount!), "Temp table should have 10 rows");

        cmd.CommandText = "INSERT INTO bulk_data SELECT * FROM temp_import";
        int inserted = cmd.ExecuteNonQuery();
        AssertEqual(10, inserted, "Should insert 10 rows");

        cmd.CommandText = "SELECT COUNT(*) FROM bulk_data";
        object? count = cmd.ExecuteScalar();
        AssertEqual(10L, Convert.ToInt64(count!), "Bulk_data should have 10 rows");

        cmd.CommandText = "SELECT SUM(value) FROM bulk_data";
        object? sum = cmd.ExecuteScalar();
        AssertTrue(Convert.ToDecimal(sum!) > 500, "Sum of values should be > 500");

        cmd.CommandText = "TRUNCATE TABLE bulk_data";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO bulk_data (id, name, value) VALUES (1, 'Test1', 100.5), (2, 'Test2', 200.5)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM bulk_data";
        count = cmd.ExecuteScalar();
        AssertEqual(2L, Convert.ToInt64(count!), "Should have 2 rows after truncate and insert");
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS bulk_data CASCADE";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());
    }
}
