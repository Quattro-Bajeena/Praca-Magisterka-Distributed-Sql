using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.BasicQueries;

[SqlTest(SqlFeatureCategory.BasicQueries, "Test SELECT INTO @variable ")]
public class SelectIntoVariableTest : SqlTest
{
    protected override void SetupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "CREATE TABLE var_test (id INT PRIMARY KEY, value INT)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO var_test VALUES (1, 42), (2, 100)";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "SELECT value INTO @myvar FROM var_test WHERE id = 1";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT @myvar";
        object? result = cmd.ExecuteScalar();
        AssertEqual(42, Convert.ToInt32(result!), "Variable should contain value 42");

        cmd.CommandText = "SELECT id, value INTO @id_var, @val_var FROM var_test WHERE id = 2";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT @id_var, @val_var";
        using (DbDataReader reader = cmd.ExecuteReader())
        {
            AssertTrue(reader.Read(), "Should read variable values");
            AssertEqual(2, Convert.ToInt32(reader.GetValue(0)), "ID variable should be 2");
            AssertEqual(100, Convert.ToInt32(reader.GetValue(1)), "Value variable should be 100");
        }
    }

    protected override void CleanupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS var_test";
        cmd.ExecuteNonQuery();
    }
}
