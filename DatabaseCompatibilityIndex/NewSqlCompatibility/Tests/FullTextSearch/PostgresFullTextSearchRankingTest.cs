using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.FullTextSearch;

[SqlTest(SqlFeatureCategory.FullTextSearch, "Test PostgreSQL full-text search ranking with ts_rank", DatabaseType.PostgreSql)]
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
                            ('Getting Started with PostgreSQL', 'PostgreSQL is an open-source relational database'),
                            ('Advanced PostgreSQL Techniques', 'Optimization and performance in PostgreSQL'),
                            ('NoSQL Alternatives', 'MongoDB and other non-relational databases')";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = @"SELECT title,
                           ts_rank(to_tsvector('english', title || ' ' || content), to_tsquery('english', 'PostgreSQL')) as rank
                           FROM blog_posts_pg
                           WHERE to_tsvector('english', title || ' ' || content) @@ to_tsquery('english', 'PostgreSQL')
                           ORDER BY rank DESC
                           LIMIT 1";
        using DbDataReader reader = cmd.ExecuteReader();
        AssertTrue(reader.Read(), "Should have at least one ranked result");
        double rank = reader.GetDouble(1);
        AssertTrue(rank > 0, "Rank should be positive");
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS blog_posts_pg CASCADE";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());
    }
}
