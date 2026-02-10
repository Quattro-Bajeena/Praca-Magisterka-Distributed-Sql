using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.FullTextSearch;

[SqlTest(SqlFeatureCategory.FullTextSearch, "Test PostgreSQL full-text search basics with tsvector and tsquery", DatabaseType.PostgreSql)]
public class PostgresFullTextSearchBasicTest : SqlTest
{
    protected override void SetupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        
        cmd.CommandText = @"CREATE TABLE articles_pg (
                            id SERIAL PRIMARY KEY,
                            title VARCHAR(255) NOT NULL,
                            content TEXT NOT NULL,
                            content_tsvector tsvector
                        )";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"INSERT INTO articles_pg (title, content, content_tsvector) VALUES 
                            ('Database Tutorial', 'Learn how to use database effectively', to_tsvector('english', 'Learn how to use database effectively')),
                            ('SQL Guide', 'SQL is a powerful database query language', to_tsvector('english', 'SQL is a powerful database query language')),
                            ('Web Development', 'Building websites with modern frameworks and SQL', to_tsvector('english', 'Building websites with modern frameworks and SQL'))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "CREATE INDEX idx_content_fts ON articles_pg USING GIN (content_tsvector)";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "SELECT COUNT(*) FROM articles_pg WHERE content_tsvector @@ to_tsquery('english', 'database')";
        object? count = cmd.ExecuteScalar();
        AssertEqual(3L, Convert.ToInt64(count!), "Should find 3 articles about database");

        cmd.CommandText = "SELECT COUNT(*) FROM articles_pg WHERE content_tsvector @@ to_tsquery('english', 'database & SQL')";
        count = cmd.ExecuteScalar();
        AssertTrue(Convert.ToInt64(count!) >= 2, "Should find articles with both database and SQL");

        cmd.CommandText = "SELECT COUNT(*) FROM articles_pg WHERE to_tsvector('english', content) @@ to_tsquery('english', 'powerful')";
        count = cmd.ExecuteScalar();
        AssertEqual(1L, Convert.ToInt64(count!), "Should find 1 article with 'powerful'");

        cmd.CommandText = "SELECT title FROM articles_pg WHERE content_tsvector @@ to_tsquery('english', 'SQL') ORDER BY title";
        using (DbDataReader reader = cmd.ExecuteReader())
        {
            int resultCount = 0;
            while (reader.Read())
            {
                resultCount++;
                string title = reader.GetString(0);
                AssertTrue(title.Length > 0, "Title should not be empty");
            }
            AssertTrue(resultCount >= 2, "Should find at least 2 articles with SQL");
        }

        cmd.CommandText = @"SELECT COUNT(*) FROM articles_pg 
                           WHERE to_tsvector('english', title || ' ' || content) @@ to_tsquery('english', 'Web')";
        count = cmd.ExecuteScalar();
        AssertEqual(1L, Convert.ToInt64(count!), "Should find 1 article about Web");
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS articles_pg CASCADE";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());
    }
}
