using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Joins;

[SqlTest(SqlFeatureCategory.Joins, "Test INNER JOIN")]
public class InnerJoinTest : SqlTest
{
    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "CREATE TABLE authors (id INT PRIMARY KEY, name VARCHAR(50))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "CREATE TABLE books (id INT PRIMARY KEY, author_id INT, title VARCHAR(100))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO authors VALUES (1, 'Author A'), (2, 'Author B')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO books VALUES (1, 1, 'Book 1'), (2, 1, 'Book 2'), (3, 2, 'Book 3')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM authors a INNER JOIN books b ON a.id = b.author_id WHERE a.id = 1";
        object? count = cmd.ExecuteScalar();
        AssertEqual(2L, (long)count!, "INNER JOIN should return 2 books for Author A");

        cmd.CommandText = "DROP TABLE books";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "DROP TABLE authors";
        cmd.ExecuteNonQuery();
    }
}
