using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.FullTextSearch;

[SqlTest(SqlFeatureCategory.FullTextSearch, "Test FULLTEXT index creation and search", Configuration.DatabaseType.MySql)]
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
                            ('SQL Guide', 'SQL is a powerful database query language')";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "SELECT COUNT(*) FROM articles WHERE MATCH(content) AGAINST('database' IN BOOLEAN MODE)";
        object? count = cmd.ExecuteScalar();
        AssertTrue(Convert.ToInt64(count!) >= 1, "Should find articles about database");
    }

    protected override void CleanupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE articles";
        cmd.ExecuteNonQuery();
    }
}
