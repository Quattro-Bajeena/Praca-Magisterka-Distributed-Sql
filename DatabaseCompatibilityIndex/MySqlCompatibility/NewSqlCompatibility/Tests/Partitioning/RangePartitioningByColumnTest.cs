using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Partitioning;

[SqlTest(SqlFeatureCategory.Partitioning, "Test RANGE partitioning by column")]
public class RangePartitioningByColumnTest : SqlTest
{
    protected override void SetupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = @"CREATE TABLE employees (
                            id INT,
                            name VARCHAR(100),
                            salary INT
                        ) PARTITION BY RANGE (salary) (
                            PARTITION low VALUES LESS THAN (30000),
                            PARTITION medium VALUES LESS THAN (60000),
                            PARTITION high VALUES LESS THAN MAXVALUE
                        )";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "INSERT INTO employees VALUES (1, 'Alice', 25000)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO employees VALUES (2, 'Bob', 45000)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO employees VALUES (3, 'Charlie', 75000)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM employees WHERE salary < 30000";
        object? lowCount = cmd.ExecuteScalar();
        AssertEqual(1L, (long)lowCount!, "Should have 1 employee in low salary partition");

        cmd.CommandText = "SELECT COUNT(*) FROM employees WHERE salary >= 30000 AND salary < 60000";
        object? medCount = cmd.ExecuteScalar();
        AssertEqual(1L, (long)medCount!, "Should have 1 employee in medium salary partition");

        cmd.CommandText = "SELECT COUNT(*) FROM employees WHERE salary >= 60000";
        object? highCount = cmd.ExecuteScalar();
        AssertEqual(1L, (long)highCount!, "Should have 1 employee in high salary partition");
    }

    protected override void CleanupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE employees";
        cmd.ExecuteNonQuery();
    }

    protected override void SetupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = @"CREATE TABLE employees (
                            id INT,
                            name VARCHAR(100),
                            salary INT
                        ) PARTITION BY RANGE (salary)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"CREATE TABLE employees_low PARTITION OF employees 
                            FOR VALUES FROM (MINVALUE) TO (30000)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"CREATE TABLE employees_medium PARTITION OF employees 
                            FOR VALUES FROM (30000) TO (60000)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"CREATE TABLE employees_high PARTITION OF employees 
                            FOR VALUES FROM (60000) TO (MAXVALUE)";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "INSERT INTO employees VALUES (1, 'Alice', 25000)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO employees VALUES (2, 'Bob', 45000)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO employees VALUES (3, 'Charlie', 75000)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM employees WHERE salary < 30000";
        object? lowCount = cmd.ExecuteScalar();
        AssertEqual(1L, (long)lowCount!, "Should have 1 employee in low salary partition");

        cmd.CommandText = "SELECT COUNT(*) FROM employees WHERE salary >= 30000 AND salary < 60000";
        object? medCount = cmd.ExecuteScalar();
        AssertEqual(1L, (long)medCount!, "Should have 1 employee in medium salary partition");

        cmd.CommandText = "SELECT COUNT(*) FROM employees WHERE salary >= 60000";
        object? highCount = cmd.ExecuteScalar();
        AssertEqual(1L, (long)highCount!, "Should have 1 employee in high salary partition");
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS employees CASCADE";
        cmd.ExecuteNonQuery();
    }
}
