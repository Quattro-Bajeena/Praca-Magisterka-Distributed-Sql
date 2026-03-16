using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.FullTextSearch;

[SqlTest(SqlFeatureCategory.FullTextSearch, "Test PostgreSQL full-text search with ranking", DatabaseType.PostgreSql)]
public class PostgresFullTextSearchRankingTest : SqlTest
{
    protected override void SetupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        
        cmd.CommandText = @"CREATE TABLE blog_posts_pg (
                            id SERIAL PRIMARY KEY,
                            title VARCHAR(255),
                            content TEXT
                        )";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"INSERT INTO blog_posts_pg (title, content) VALUES 
                            ('Getting Started with PostgreSQL', 'PostgreSQL is an open-source relational database with advanced features'),
                            ('Advanced PostgreSQL Techniques', 'Learn optimization and performance tuning in PostgreSQL database systems'),
                            ('NoSQL Databases', 'MongoDB and other NoSQL alternatives to PostgreSQL and MySQL databases')";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = @"SELECT COUNT(*) FROM blog_posts_pg 
                           WHERE to_tsvector('english', title || ' ' || content) @@ to_tsquery('english', 'PostgreSQL')";
        object? count = cmd.ExecuteScalar();
        AssertEqual(3L, Convert.ToInt64(count!), "Should find 3 posts about PostgreSQL");

        cmd.CommandText = @"SELECT title, 
                           ts_rank(to_tsvector('english', title || ' ' || content), to_tsquery('english', 'PostgreSQL')) as rank
                           FROM blog_posts_pg
                           WHERE to_tsvector('english', title || ' ' || content) @@ to_tsquery('english', 'PostgreSQL')
                           ORDER BY rank DESC
                           LIMIT 1";
        using (DbDataReader reader = cmd.ExecuteReader())
        {
            AssertTrue(reader.Read(), "Should have top-ranked result");
            string title = reader.GetString(0);
            double rank = reader.GetDouble(1);
            AssertTrue(rank > 0, "Rank should be positive");
            AssertTrue(title.Length > 0, "Title should not be empty");
        }

        cmd.CommandText = @"SELECT COUNT(*) FROM blog_posts_pg 
                           WHERE to_tsvector('english', content) @@ to_tsquery('english', 'optimization | performance')";
        count = cmd.ExecuteScalar();
        AssertTrue(Convert.ToInt64(count!) >= 1, "Should find posts about optimization or performance");

        cmd.CommandText = @"SELECT title,
                           ts_rank_cd(to_tsvector('english', title || ' ' || content), to_tsquery('english', 'database & PostgreSQL')) as rank
                           FROM blog_posts_pg
                           WHERE to_tsvector('english', title || ' ' || content) @@ to_tsquery('english', 'database & PostgreSQL')
                           ORDER BY rank DESC";
        int resultCount = 0;
        using (DbDataReader reader = cmd.ExecuteReader())
        {
            while (reader.Read())
            {
                resultCount++;
                double rank = reader.GetDouble(1);
                AssertTrue(rank > 0, "Rank should be positive for matching documents");
            }
        }
        AssertTrue(resultCount >= 2, "Should find at least 2 posts with both database and PostgreSQL");
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS blog_posts_pg CASCADE";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());
    }
}
