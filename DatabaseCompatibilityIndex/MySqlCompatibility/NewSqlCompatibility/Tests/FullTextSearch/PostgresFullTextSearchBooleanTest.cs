using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.FullTextSearch;

[SqlTest(SqlFeatureCategory.FullTextSearch, "Test PostgreSQL full-text search with boolean operators", DatabaseType.PostgreSql)]
public class PostgresFullTextSearchBooleanTest : SqlTest
{
    protected override void SetupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        
        cmd.CommandText = @"CREATE TABLE posts_pg (
                            id SERIAL PRIMARY KEY,
                            content TEXT,
                            content_vector tsvector
                        )";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"INSERT INTO posts_pg (content, content_vector) VALUES 
                            ('C++ programming is powerful', to_tsvector('english', 'C++ programming is powerful')),
                            ('Java is used for enterprise applications', to_tsvector('english', 'Java is used for enterprise applications')),
                            ('Python is great for data science', to_tsvector('english', 'Python is great for data science')),
                            ('Go is fast and efficient', to_tsvector('english', 'Go is fast and efficient')),
                            ('Ruby on Rails framework', to_tsvector('english', 'Ruby on Rails framework'))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "CREATE INDEX idx_posts_fts ON posts_pg USING GIN (content_vector)";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "SELECT COUNT(*) FROM posts_pg WHERE content_vector @@ to_tsquery('english', 'programming')";
        object? count = cmd.ExecuteScalar();
        AssertEqual(1L, Convert.ToInt64(count!), "Should find 1 post with 'programming'");

        cmd.CommandText = "SELECT COUNT(*) FROM posts_pg WHERE content_vector @@ to_tsquery('english', 'great & science')";
        count = cmd.ExecuteScalar();
        AssertEqual(1L, Convert.ToInt64(count!), "AND operator: should find 1 post with both 'great' and 'science'");

        cmd.CommandText = "SELECT COUNT(*) FROM posts_pg WHERE content_vector @@ to_tsquery('english', 'Python | Java')";
        count = cmd.ExecuteScalar();
        AssertEqual(2L, Convert.ToInt64(count!), "OR operator: should find 2 posts with Python or Java");

        cmd.CommandText = "SELECT COUNT(*) FROM posts_pg WHERE content_vector @@ to_tsquery('english', '!Python')";
        count = cmd.ExecuteScalar();
        AssertTrue(Convert.ToInt64(count!) >= 4, "NOT operator: should find posts without Python");

        cmd.CommandText = "SELECT COUNT(*) FROM posts_pg WHERE content_vector @@ to_tsquery('english', 'fast & efficient')";
        count = cmd.ExecuteScalar();
        AssertEqual(1L, Convert.ToInt64(count!), "Should find post with both 'fast' and 'efficient'");

        cmd.CommandText = @"SELECT content FROM posts_pg 
                           WHERE content_vector @@ to_tsquery('english', 'enterprise | science')
                           ORDER BY content";
        int resultCount = 0;
        using (DbDataReader reader = cmd.ExecuteReader())
        {
            while (reader.Read())
            {
                resultCount++;
            }
        }
        AssertEqual(2, resultCount, "Should find 2 posts matching 'enterprise' or 'science'");
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS posts_pg CASCADE";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());
    }
}
