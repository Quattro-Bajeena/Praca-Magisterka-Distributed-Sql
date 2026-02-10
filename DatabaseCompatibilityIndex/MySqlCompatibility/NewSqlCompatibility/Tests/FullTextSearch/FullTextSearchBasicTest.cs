using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.FullTextSearch;

[SqlTest(SqlFeatureCategory.FullTextSearch, "Test FULLTEXT index creation and search")]
public class FullTextSearchBasicTest : SqlTest
{
    protected override void SetupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = @"CREATE TABLE articles (
                            id INT PRIMARY KEY AUTO_INCREMENT,
                            title VARCHAR(255) NOT NULL,
                            content TEXT NOT NULL,
                            FULLTEXT INDEX ft_content (content)
                        )";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"INSERT INTO articles (title, content) VALUES 
                            ('Database Tutorial', 'Learn how to use database effectively'),
                            ('SQL Guide', 'SQL is a powerful database query language'),
                            ('Web Development', 'Building websites with modern frameworks and SQL')";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "SELECT COUNT(*) FROM articles WHERE MATCH(content) AGAINST('database SQL')";
        object? count = cmd.ExecuteScalar();
        AssertEqual(3L, (long)count!, "Should find 3 articles about database");
    }

    protected override void CleanupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE articles";
        cmd.ExecuteNonQuery();
    }
}
