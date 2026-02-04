using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.CTE;

[SqlTest(SqlFeatureCategory.CTE, "Test recursive CTE", DatabaseType.MySql)]
public class RecursiveCteTest : SqlTest
{
    protected override void SetupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "CREATE TABLE hierarchy (id INT PRIMARY KEY, parent_id INT, name VARCHAR(50))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO hierarchy VALUES (1, NULL, 'Root'), (2, 1, 'Child1'), (3, 1, 'Child2'), (4, 2, 'Grandchild')";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = @"
            WITH RECURSIVE cte AS (
                SELECT id, parent_id, name, 0 as level FROM hierarchy WHERE parent_id IS NULL
                UNION ALL
                SELECT h.id, h.parent_id, h.name, c.level + 1 FROM hierarchy h 
                JOIN cte c ON h.parent_id = c.id
            )
            SELECT COUNT(*) FROM cte";

        object? count = cmd.ExecuteScalar();
        AssertEqual(4L, Convert.ToInt64(count!), "Recursive CTE should work");
    }

    protected override void CleanupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE hierarchy";
        cmd.ExecuteNonQuery();
    }
}
