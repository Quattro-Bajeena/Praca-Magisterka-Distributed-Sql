using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.FullTextSearch;

[SqlTest(SqlFeatureCategory.FullTextSearch, "Test FULLTEXT boolean mode operators", Configuration.DatabaseType.MySql)]
public class FullTextSearchBooleanModeTest : SqlTest
{
    protected override void SetupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = @"CREATE TABLE posts (
                            id INT PRIMARY KEY AUTO_INCREMENT,
                            content TEXT,
                            FULLTEXT INDEX ft_posts (content)
                        )";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"INSERT INTO posts (content) VALUES
                            ('C++ programming is powerful'),
                            ('Python is great for data science'),
                            ('Ruby on Rails framework')";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "SELECT COUNT(*) FROM posts WHERE MATCH(content) AGAINST('+programming' IN BOOLEAN MODE)";
        object? count = cmd.ExecuteScalar();
        AssertEqual(1L, (long)count!, "Should find 1 post with '+programming'");

        cmd.CommandText = "SELECT COUNT(*) FROM posts WHERE MATCH(content) AGAINST('Python | Ruby' IN BOOLEAN MODE)";
        count = cmd.ExecuteScalar();
        AssertTrue((long)count! >= 2, "OR operator should find Python or Ruby posts");
    }

    protected override void CleanupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE posts";
        cmd.ExecuteNonQuery();
    }
}
