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
                            content TEXT
                        )";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"INSERT INTO posts_pg (content) VALUES
                            ('Python is great for data science'),
                            ('Java is used for enterprise applications'),
                            ('Ruby on Rails framework')";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "SELECT COUNT(*) FROM posts_pg WHERE to_tsvector('english', content) @@ to_tsquery('english', 'Python | Java')";
        object? count = cmd.ExecuteScalar();
        AssertEqual(2L, Convert.ToInt64(count!), "OR operator: should find 2 posts with Python or Java");

        cmd.CommandText = "SELECT COUNT(*) FROM posts_pg WHERE to_tsvector('english', content) @@ to_tsquery('english', 'Python & science')";
        count = cmd.ExecuteScalar();
        AssertEqual(1L, Convert.ToInt64(count!), "AND operator: should find 1 post with both Python and science");
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS posts_pg CASCADE";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());
    }
}
