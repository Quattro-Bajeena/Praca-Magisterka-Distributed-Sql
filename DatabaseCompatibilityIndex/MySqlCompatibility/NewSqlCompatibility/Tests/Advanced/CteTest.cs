using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Advanced;

[SqlTest(SqlFeatureCategory.CTE, "Test Common Table Expression (CTE)", DatabaseType.MySql)]
public class CteTest : SqlTest
{
    public override void Setup(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "CREATE TABLE employees_cte (id INT PRIMARY KEY, name VARCHAR(50), salary DECIMAL(10,2))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO employees_cte VALUES (1, 'Alice', 50000), (2, 'Bob', 60000), (3, 'Charlie', 55000)";
        cmd.ExecuteNonQuery();
    }

    public override void Execute(DbConnection connection)
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

    public override void Cleanup(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE employees_cte";
        cmd.ExecuteNonQuery();
    }
}
