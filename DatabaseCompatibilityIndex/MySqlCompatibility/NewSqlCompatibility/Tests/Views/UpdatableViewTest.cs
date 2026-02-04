using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Views;

[SqlTest(SqlFeatureCategory.Views, "Test Updatable Views (not updatable in TiDB)", DatabaseType.MySql)]
public class UpdatableViewTest : SqlTest
{
    protected override void SetupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "CREATE TABLE view_base (id INT PRIMARY KEY, name VARCHAR(50), salary DECIMAL(10,2))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO view_base VALUES (1, 'Alice', 50000), (2, 'Bob', 60000), (3, 'Charlie', 55000)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "CREATE VIEW employee_view AS SELECT id, name, salary FROM view_base";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();


        cmd.CommandText = "UPDATE employee_view SET salary = 65000 WHERE id = 2";
        cmd.ExecuteNonQuery();


        cmd.CommandText = "SELECT salary FROM view_base WHERE id = 2";
        object? result = cmd.ExecuteScalar();
        AssertEqual(65000m, Convert.ToDecimal(result!), "View UPDATE should modify base table");


        cmd.CommandText = "INSERT INTO employee_view (id, name, salary) VALUES (4, 'David', 70000)";
        cmd.ExecuteNonQuery();


        cmd.CommandText = "SELECT COUNT(*) FROM view_base";
        object? count = cmd.ExecuteScalar();
        AssertEqual(4L, Convert.ToInt64(count!), "View INSERT should add to base table");


        cmd.CommandText = "DELETE FROM employee_view WHERE id = 4";
        cmd.ExecuteNonQuery();


        cmd.CommandText = "SELECT COUNT(*) FROM view_base";
        count = cmd.ExecuteScalar();
        AssertEqual(3L, Convert.ToInt64(count!), "View DELETE should remove from base table");
    }

    protected override void CleanupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP VIEW IF EXISTS employee_view";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "DROP TABLE IF EXISTS view_base";
        cmd.ExecuteNonQuery();
    }
}
