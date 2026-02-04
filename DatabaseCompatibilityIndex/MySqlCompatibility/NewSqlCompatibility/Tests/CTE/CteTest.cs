using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.CTE;

[SqlTest(SqlFeatureCategory.CTE, "Test Common Table Expression (CTE)")]
public class CteTest : SqlTest
{
    protected override void SetupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "CREATE TABLE employees_cte (id INT PRIMARY KEY, name VARCHAR(50), salary DECIMAL(10,2))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO employees_cte VALUES (1, 'Alice', 50000), (2, 'Bob', 60000), (3, 'Charlie', 55000)";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = @"
            WITH high_earners AS (
                SELECT name, salary FROM employees_cte WHERE salary > 55000
            )
            SELECT COUNT(*) FROM high_earners";

        object? count = cmd.ExecuteScalar();
        AssertEqual(1L, (long)count!, "CTE should work correctly");
    }

    protected override void CleanupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE employees_cte";
        cmd.ExecuteNonQuery();
    }
}
