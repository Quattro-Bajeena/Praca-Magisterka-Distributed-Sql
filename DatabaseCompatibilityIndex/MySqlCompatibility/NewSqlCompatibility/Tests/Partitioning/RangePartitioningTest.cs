using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Partitioning;

[SqlTest(SqlFeatureCategory.Partitioning, "Test RANGE partitioning by date")]
public class RangePartitioningTest : SqlTest
{
    protected override void SetupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = @"CREATE TABLE sales_data (
                            id INT,
                            amount DECIMAL(10, 2),
                            sale_date DATE
                        ) PARTITION BY RANGE (YEAR(sale_date)) (
                            PARTITION p2020 VALUES LESS THAN (2021),
                            PARTITION p2021 VALUES LESS THAN (2022),
                            PARTITION p2022 VALUES LESS THAN (2023),
                            PARTITION p2023 VALUES LESS THAN MAXVALUE
                        )";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "INSERT INTO sales_data VALUES (1, 1000, '2020-06-15')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO sales_data VALUES (2, 1500, '2021-03-20')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO sales_data VALUES (3, 2000, '2022-11-10')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO sales_data VALUES (4, 2500, '2023-05-25')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM sales_data";
        object? count = cmd.ExecuteScalar();
        AssertEqual(4L, (long)count!, "Should have 4 rows across partitions");

        cmd.CommandText = "SELECT COUNT(*) FROM sales_data WHERE sale_date >= '2022-01-01' AND sale_date < '2023-01-01'";
        count = cmd.ExecuteScalar();
        AssertEqual(1L, (long)count!, "Should have 1 row in 2022 partition");
    }

    protected override string? CleanupCommandMy => "DROP TABLE sales_data";
}
