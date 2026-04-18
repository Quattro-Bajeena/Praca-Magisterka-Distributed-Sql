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
                            content TEXT NOT NULL
                        )";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"INSERT INTO articles_pg (content) VALUES
                            ('Learn how to use database effectively'),
                            ('SQL is a powerful database query language'),
                            ('Building websites with modern frameworks')";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "SELECT COUNT(*) FROM articles_pg WHERE to_tsvector('english', content) @@ to_tsquery('english', 'database')";
        object? count = cmd.ExecuteScalar();
        AssertEqual(2L, Convert.ToInt64(count!), "Should find 2 articles about database");

        cmd.CommandText = "SELECT COUNT(*) FROM articles_pg WHERE to_tsvector('english', content) @@ to_tsquery('english', 'powerful')";
        count = cmd.ExecuteScalar();
        AssertEqual(1L, Convert.ToInt64(count!), "Should find 1 article with 'powerful'");
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS articles_pg CASCADE";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());
    }
}
