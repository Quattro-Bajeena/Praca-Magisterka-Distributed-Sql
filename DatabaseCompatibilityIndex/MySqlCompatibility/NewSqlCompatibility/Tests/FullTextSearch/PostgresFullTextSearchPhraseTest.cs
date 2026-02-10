using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.FullTextSearch;

[SqlTest(SqlFeatureCategory.FullTextSearch, "Test PostgreSQL full-text search with phrase search and proximity", DatabaseType.PostgreSql)]
public class PostgresFullTextSearchPhraseTest : SqlTest
{
    protected override void SetupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        
        cmd.CommandText = @"CREATE TABLE documents_pg (
                            doc_id SERIAL PRIMARY KEY,
                            title VARCHAR(255),
                            body TEXT,
                            author VARCHAR(100)
                        )";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"INSERT INTO documents_pg (title, body, author) VALUES 
                            ('Database Performance', 'Tips for optimizing database queries and improving performance', 'John'),
                            ('Query Optimization Guide', 'Advanced techniques in query optimization for databases', 'Jane'),
                            ('Data Science with Databases', 'Using databases for data analysis and machine learning', 'Bob')";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = @"SELECT COUNT(*) FROM documents_pg 
                           WHERE to_tsvector('english', title || ' ' || body) @@ phraseto_tsquery('english', 'query optimization')";
        object? count = cmd.ExecuteScalar();
        AssertEqual(1L, Convert.ToInt64(count!), "Should find 1 document with phrase 'query optimization'");

        cmd.CommandText = @"SELECT COUNT(*) FROM documents_pg 
                           WHERE to_tsvector('english', title || ' ' || body) @@ to_tsquery('english', 'database <-> queries')";
        count = cmd.ExecuteScalar();
        AssertTrue(Convert.ToInt64(count!) >= 1, "Proximity operator: should find documents with 'database' followed by 'queries'");

        cmd.CommandText = @"SELECT COUNT(*) FROM documents_pg 
                           WHERE to_tsvector('english', body) @@ to_tsquery('english', 'performance | optimization')";
        count = cmd.ExecuteScalar();
        AssertTrue(Convert.ToInt64(count!) >= 2, "Should find documents with 'performance' or 'optimization'");

        cmd.CommandText = @"SELECT title FROM documents_pg 
                           WHERE to_tsvector('english', title || ' ' || body) @@ plainto_tsquery('english', 'data science')
                           ORDER BY title";
        using (DbDataReader reader = cmd.ExecuteReader())
        {
            AssertTrue(reader.Read(), "Should find document about data science");
            string title = reader.GetString(0);
            AssertTrue(title.Contains("Data"), "Title should contain 'Data'");
        }

        cmd.CommandText = @"SELECT COUNT(*) FROM documents_pg 
                           WHERE to_tsvector('english', title) @@ to_tsquery('english', 'Database')
                           AND to_tsvector('english', body) @@ to_tsquery('english', 'optimizing | analysis')";
        count = cmd.ExecuteScalar();
        AssertTrue(Convert.ToInt64(count!) >= 1, "Should find documents matching title and body criteria");

        cmd.CommandText = @"SELECT ts_headline('english', body, to_tsquery('english', 'database & optimization'))
                           FROM documents_pg
                           WHERE to_tsvector('english', body) @@ to_tsquery('english', 'database & optimization')
                           LIMIT 1";
        using (DbDataReader reader = cmd.ExecuteReader())
        {
            if (reader.Read())
            {
                string headline = reader.GetString(0);
                AssertTrue(headline.Length > 0, "Headline should be generated");
            }
        }
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS documents_pg CASCADE";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());
    }
}
