using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.FullTextSearch;

[SqlTest(SqlFeatureCategory.FullTextSearch, "Test PostgreSQL full-text search with generated tsvector column", DatabaseType.PostgreSql)]
public class PostgresFullTextSearchGeneratedTest : SqlTest
{
    protected override void SetupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = @"CREATE TABLE articles_auto (
                            id SERIAL PRIMARY KEY,
                            title VARCHAR(255) NOT NULL,
                            content TEXT NOT NULL,
                            search_vector tsvector GENERATED ALWAYS AS (to_tsvector('english', title || ' ' || content)) STORED
                        )";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"INSERT INTO articles_auto (title, content) VALUES
                            ('Machine Learning Basics', 'Introduction to machine learning algorithms'),
                            ('AI in Healthcare', 'Applications of artificial intelligence in medicine')";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "SELECT COUNT(*) FROM articles_auto WHERE search_vector @@ to_tsquery('english', 'machine & learning')";
        object? count = cmd.ExecuteScalar();
        AssertEqual(1L, Convert.ToInt64(count!), "Should find 1 article about machine learning");

        cmd.CommandText = "INSERT INTO articles_auto (title, content) VALUES ('Deep Learning', 'Neural network training techniques')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM articles_auto WHERE search_vector @@ to_tsquery('english', 'learning')";
        count = cmd.ExecuteScalar();
        AssertEqual(2L, Convert.ToInt64(count!), "Generated column should be auto-updated on insert");
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS articles_auto CASCADE";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());
    }
}
