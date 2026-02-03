using NSCI.Testing;using NSCI.Configuration;
using System.Data.Common;

namespace NSCI.Tests.FullTextSearch;

[SqlTest(SqlFeatureCategory.FullTextSearch, "Test FULLTEXT with multiple columns", DatabaseType.MySql)]
public class FullTextSearchMultiColumnTest : SqlTest
{
    public override void Setup(DbConnection connection)
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

    public override void Execute(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        // Search in multiple columns
        cmd.CommandText = "SELECT COUNT(*) FROM documents WHERE MATCH(title, body) AGAINST('performance' IN BOOLEAN MODE)";
        object? count = cmd.ExecuteScalar();
        AssertEqual(1L, (long)count!, "Should find 1 document matching 'performance'");

        cmd.CommandText = "SELECT COUNT(*) FROM documents WHERE MATCH(title, body) AGAINST('SQL' IN BOOLEAN MODE)";
        count = cmd.ExecuteScalar();
        AssertEqual(2L, (long)count!, "Should find 2 documents matching 'SQL'");
    }

    public override void Cleanup(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE documents";
        cmd.ExecuteNonQuery();
    }
}
