using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Subqueries;

[SqlTest(SqlFeatureCategory.Subqueries, "Test NOT IN subquery")]
public class NotInSubqueryTest : SqlTest
{
    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "CREATE TABLE allowed_ids (id INT PRIMARY KEY)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "CREATE TABLE all_records (id INT PRIMARY KEY, value INT)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO allowed_ids VALUES (1), (2), (3)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO all_records VALUES (1, 100), (2, 200), (3, 300), (4, 400), (5, 500)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM all_records WHERE id NOT IN (SELECT id FROM allowed_ids)";
        object? count = cmd.ExecuteScalar();
        AssertEqual(2L, Convert.ToInt64(count!), "NOT IN subquery should return 2 records");

        cmd.CommandText = "DROP TABLE all_records";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "DROP TABLE allowed_ids";
        cmd.ExecuteNonQuery();
    }
}
