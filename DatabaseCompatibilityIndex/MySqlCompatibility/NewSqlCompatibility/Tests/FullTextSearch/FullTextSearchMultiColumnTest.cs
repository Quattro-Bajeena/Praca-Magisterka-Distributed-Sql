using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.FullTextSearch;

[SqlTest(SqlFeatureCategory.FullTextSearch, "Test FULLTEXT with multiple columns")]
public class FullTextSearchMultiColumnTest : SqlTest
{
    protected override void SetupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = @"CREATE TABLE documents (
                            doc_id INT PRIMARY KEY AUTO_INCREMENT,
                            title VARCHAR(255),
                            body TEXT,
                            author VARCHAR(100),
                            FULLTEXT INDEX ft_search (title, body)
                        )";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"INSERT INTO documents (title, body, author) VALUES 
                            ('MySQL Performance', 'Tips for optimizing MySQL queries', 'John'),
                            ('PostgreSQL Features', 'Advanced features in PostgreSQL database', 'Jane'),
                            ('Data Science with SQL', 'Using SQL for data analysis', 'Bob')";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "SELECT COUNT(*) FROM documents WHERE MATCH(title, body) AGAINST('performance' IN BOOLEAN MODE)";
        object? count = cmd.ExecuteScalar();
        AssertEqual(1L, Convert.ToInt64(count!), "Should find 1 document matching 'performance'");

        cmd.CommandText = "SELECT COUNT(*) FROM documents WHERE MATCH(title, body) AGAINST('SQL' IN BOOLEAN MODE)";
        count = cmd.ExecuteScalar();
        AssertEqual(1L, Convert.ToInt64(count!), "Should find 1 document matching 'SQL'");
    }

    protected override void CleanupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE documents";
        cmd.ExecuteNonQuery();
    }
}
