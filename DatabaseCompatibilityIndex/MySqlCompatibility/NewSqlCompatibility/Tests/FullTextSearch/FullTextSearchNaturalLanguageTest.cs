using NSCI.Testing;using NSCI.Configuration;
using System.Data.Common;

namespace NSCI.Tests.FullTextSearch;

[SqlTest(SqlFeatureCategory.FullTextSearch, "Test FULLTEXT natural language search", DatabaseType.MySql)]
public class FullTextSearchNaturalLanguageTest : SqlTest
{
    public override void Setup(DbConnection connection)
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

    public override void Execute(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        // Natural language search (default mode)
        cmd.CommandText = "SELECT COUNT(*) FROM blog_posts WHERE MATCH(title, content) AGAINST('MySQL')";
        object? count = cmd.ExecuteScalar();
        AssertEqual(3L, (long)count!, "Natural language search should find 3 posts about MySQL");

        cmd.CommandText = "SELECT COUNT(*) FROM blog_posts WHERE MATCH(title, content) AGAINST('optimization')";
        count = cmd.ExecuteScalar();
        AssertEqual(1L, (long)count!, "Should find 1 post about optimization");
    }

    public override void Cleanup(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE blog_posts";
        cmd.ExecuteNonQuery();
    }
}
