using NSCI.Testing;using NSCI.Configuration;
using System.Data.Common;

namespace NSCI.Tests.FullTextSearch;

[SqlTest(SqlFeatureCategory.FullTextSearch, "Test FULLTEXT index creation and search", DatabaseType.MySql)]
public class FullTextSearchBasicTest : SqlTest
{
    public override void Setup(DbConnection connection)
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
                            ('Database Tutorial', 'Learn how to use databases effectively'),
                            ('SQL Guide', 'SQL is a powerful database query language'),
                            ('Web Development', 'Building websites with modern frameworks')";
        cmd.ExecuteNonQuery();
    }

    public override void Execute(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        // Full-text search
        cmd.CommandText = "SELECT id, title FROM articles WHERE MATCH(content) AGAINST('database' IN BOOLEAN MODE)";
        using DbDataReader reader = cmd.ExecuteReader();
        AssertTrue(reader.Read(), "Should find at least one article about database");
        AssertEqual(1, reader.GetInt32(0), "First result should be article id 1");
    }

    public override void Cleanup(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE articles";
        cmd.ExecuteNonQuery();
    }
}
