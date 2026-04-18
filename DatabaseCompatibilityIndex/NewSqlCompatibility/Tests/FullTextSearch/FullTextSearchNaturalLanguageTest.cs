using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.FullTextSearch;

[SqlTest(SqlFeatureCategory.FullTextSearch, "Test FULLTEXT natural language search", Configuration.DatabaseType.MySql)]
public class FullTextSearchNaturalLanguageTest : SqlTest
{
    protected override void SetupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = @"CREATE TABLE blog_posts (
                            id INT PRIMARY KEY AUTO_INCREMENT,
                            title VARCHAR(255),
                            content TEXT,
                            FULLTEXT INDEX ft_blog (title, content)
                        )";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"INSERT INTO blog_posts (title, content) VALUES
                            ('Getting Started with MySQL', 'MySQL is an open-source relational database'),
                            ('Advanced MySQL Techniques', 'Learn optimization and performance tuning'),
                            ('NoSQL Databases', 'MongoDB and other NoSQL alternatives to MySQL')";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "SELECT COUNT(*) FROM blog_posts WHERE MATCH(title, content) AGAINST('MySQL')";
        object? count = cmd.ExecuteScalar();
        AssertTrue((long)count! >= 1, "Natural language search should find posts about MySQL");
    }

    protected override void CleanupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE blog_posts";
        cmd.ExecuteNonQuery();
    }
}
