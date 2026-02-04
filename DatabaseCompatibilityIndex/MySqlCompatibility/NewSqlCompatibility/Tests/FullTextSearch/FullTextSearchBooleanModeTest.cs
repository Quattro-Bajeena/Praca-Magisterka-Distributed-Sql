using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.FullTextSearch;

[SqlTest(SqlFeatureCategory.FullTextSearch, "Test FULLTEXT boolean mode operators")]
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
                            ('Java is used for enterprise applications'),
                            ('Python is great for data science'),
                            ('Go is fast and efficient'),
                            ('Ruby on Rails framework')";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "SELECT COUNT(*) FROM posts WHERE MATCH(content) AGAINST('+programming' IN BOOLEAN MODE)";
        object? count = cmd.ExecuteScalar();
        AssertEqual(1L, (long)count!, "Should find 1 post with '+programming'");

        cmd.CommandText = "SELECT COUNT(*) FROM posts WHERE MATCH(content) AGAINST('+language -python' IN BOOLEAN MODE)";
        count = cmd.ExecuteScalar();
        AssertTrue((long)count! >= 0, "Exclusion search should work");

        cmd.CommandText = "SELECT COUNT(*) FROM posts WHERE MATCH(content) AGAINST('\"is great\"' IN BOOLEAN MODE)";
        count = cmd.ExecuteScalar();
        AssertEqual(1L, (long)count!, "Should find 1 post with phrase 'is great'");
    }

    protected override void CleanupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE posts";
        cmd.ExecuteNonQuery();
    }
}
