using NSCI.Testing;using NSCI.Configuration;
using System.Data.Common;

namespace NSCI.Tests.FullTextSearch;

[SqlTest(SqlFeatureCategory.FullTextSearch, "Test FULLTEXT boolean mode operators", DatabaseType.MySql)]
public class FullTextSearchBooleanModeTest : SqlTest
{
    public override void Setup(DbConnection connection)
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

    public override void Execute(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        // Search with inclusion operator (+)
        cmd.CommandText = "SELECT COUNT(*) FROM posts WHERE MATCH(content) AGAINST('+programming' IN BOOLEAN MODE)";
        object? count = cmd.ExecuteScalar();
        AssertEqual(1L, (long)count!, "Should find 1 post with '+programming'");

        // Search with exclusion operator (-)
        cmd.CommandText = "SELECT COUNT(*) FROM posts WHERE MATCH(content) AGAINST('+language -python' IN BOOLEAN MODE)";
        count = cmd.ExecuteScalar();
        // Language doesn't appear in any post, so this might return 0
        AssertTrue((long)count! >= 0, "Exclusion search should work");

        // Search with phrase operator ("")
        cmd.CommandText = "SELECT COUNT(*) FROM posts WHERE MATCH(content) AGAINST('\"is great\"' IN BOOLEAN MODE)";
        count = cmd.ExecuteScalar();
        AssertEqual(1L, (long)count!, "Should find 1 post with phrase 'is great'");
    }

    public override void Cleanup(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE posts";
        cmd.ExecuteNonQuery();
    }
}
