using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Joins;

[SqlTest(SqlFeatureCategory.Joins, "Test SELF JOIN")]
public class SelfJoinTest : SqlTest
{
    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "CREATE TABLE employees_self (id INT PRIMARY KEY, name VARCHAR(50), manager_id INT)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO employees_self VALUES (1, 'Boss', NULL), (2, 'John', 1), (3, 'Jane', 1), (4, 'Bob', 2)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM employees_self e1 JOIN employees_self e2 ON e1.id = e2.manager_id";
        object? count = cmd.ExecuteScalar();
        AssertEqual(3L, (long)count!, "SELF JOIN should find all manager-employee relationships");

        cmd.CommandText = "DROP TABLE employees_self";
        cmd.ExecuteNonQuery();
    }
}
