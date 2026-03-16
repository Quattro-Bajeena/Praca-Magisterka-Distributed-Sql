using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Subqueries;

[SqlTest(SqlFeatureCategory.Subqueries, "Test NOT EXISTS subquery")]
public class NotExistsSubqueryTest : SqlTest
{
    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "CREATE TABLE parent_records (id INT PRIMARY KEY, name VARCHAR(50))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "CREATE TABLE child_records (id INT PRIMARY KEY, parent_id INT)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO parent_records VALUES (1, 'P1'), (2, 'P2'), (3, 'P3')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO child_records VALUES (1, 1), (2, 2)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM parent_records p WHERE NOT EXISTS (SELECT 1 FROM child_records c WHERE c.parent_id = p.id)";
        object? count = cmd.ExecuteScalar();
        AssertEqual(1L, Convert.ToInt64(count!), "NOT EXISTS subquery should find 1 parent without children");

        cmd.CommandText = "DROP TABLE child_records";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "DROP TABLE parent_records";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "CREATE TABLE parent_records (id INT PRIMARY KEY, name VARCHAR(50))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "CREATE TABLE child_records (id INT PRIMARY KEY, parent_id INT)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO parent_records VALUES (1, 'P1'), (2, 'P2'), (3, 'P3')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO child_records VALUES (1, 1), (2, 2)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM parent_records p WHERE NOT EXISTS (SELECT 1 FROM child_records c WHERE c.parent_id = p.id)";
        object? count = cmd.ExecuteScalar();
        AssertEqual(1L, Convert.ToInt64(count!), "NOT EXISTS subquery should find 1 parent without children");

        cmd.CommandText = "DROP TABLE child_records";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "DROP TABLE parent_records";
        cmd.ExecuteNonQuery();
    }
}
