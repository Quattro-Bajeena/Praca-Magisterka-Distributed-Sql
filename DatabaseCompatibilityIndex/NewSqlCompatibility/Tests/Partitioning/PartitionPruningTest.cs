using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Partitioning;

[SqlTest(SqlFeatureCategory.Partitioning, "Test partition pruning in queries")]
public class PartitionPruningTest : SqlTest
{
    protected override void SetupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = @"CREATE TABLE logs (
                            id INT,
                            log_date DATE,
                            message VARCHAR(255)
                        ) PARTITION BY RANGE (YEAR(log_date)) (
                            PARTITION p2022 VALUES LESS THAN (2023),
                            PARTITION p2023 VALUES LESS THAN (2024),
                            PARTITION p2024 VALUES LESS THAN MAXVALUE
                        )";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO logs VALUES (1, '2022-01-15', 'Old log')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO logs VALUES (2, '2023-06-20', 'Recent log')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO logs VALUES (3, '2024-02-10', 'Latest log')";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "SELECT COUNT(*) FROM logs WHERE log_date >= '2023-01-01' AND log_date < '2024-01-01'";
        object? count = cmd.ExecuteScalar();
        AssertEqual(1L, (long)count!, "Should find 1 log from 2023");

        cmd.CommandText = "SELECT COUNT(*) FROM logs WHERE log_date >= '2022-06-01'";
        count = cmd.ExecuteScalar();
        AssertEqual(2L, (long)count!, "Should find 2 logs from 2023 and 2024");

        cmd.CommandText = "SELECT COUNT(*) FROM logs";
        count = cmd.ExecuteScalar();
        AssertEqual(3L, (long)count!, "Should find all 3 logs");
    }

    protected override void CleanupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE logs";
        cmd.ExecuteNonQuery();
    }

    protected override void SetupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = @"CREATE TABLE logs (
                            id INT,
                            log_date DATE,
                            message VARCHAR(255)
                        ) PARTITION BY RANGE (log_date)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"CREATE TABLE logs_p2022 PARTITION OF logs 
                            FOR VALUES FROM ('2022-01-01') TO ('2023-01-01')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"CREATE TABLE logs_p2023 PARTITION OF logs 
                            FOR VALUES FROM ('2023-01-01') TO ('2024-01-01')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"CREATE TABLE logs_p2024 PARTITION OF logs 
                            FOR VALUES FROM ('2024-01-01') TO ('2025-01-01')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO logs VALUES (1, '2022-01-15', 'Old log')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO logs VALUES (2, '2023-06-20', 'Recent log')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO logs VALUES (3, '2024-02-10', 'Latest log')";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "SELECT COUNT(*) FROM logs WHERE log_date >= '2023-01-01' AND log_date < '2024-01-01'";
        object? count = cmd.ExecuteScalar();
        AssertEqual(1L, (long)count!, "Should find 1 log from 2023");

        cmd.CommandText = "SELECT COUNT(*) FROM logs WHERE log_date >= '2022-06-01'";
        count = cmd.ExecuteScalar();
        AssertEqual(2L, (long)count!, "Should find 2 logs from 2023 and 2024");

        cmd.CommandText = "SELECT COUNT(*) FROM logs";
        count = cmd.ExecuteScalar();
        AssertEqual(3L, (long)count!, "Should find all 3 logs");
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS logs CASCADE";
        cmd.ExecuteNonQuery();
    }
}
