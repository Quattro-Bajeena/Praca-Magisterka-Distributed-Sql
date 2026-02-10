using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.FullTextSearch;

[SqlTest(SqlFeatureCategory.FullTextSearch, "Test PostgreSQL full-text search with generated columns and triggers", DatabaseType.PostgreSql)]
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

        cmd.CommandText = "CREATE INDEX idx_search_vector ON articles_auto USING GIN (search_vector)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"INSERT INTO articles_auto (title, content) VALUES 
                            ('Machine Learning Basics', 'Introduction to machine learning algorithms and neural networks'),
                            ('Deep Learning Tutorial', 'Understanding deep neural networks and backpropagation'),
                            ('AI in Healthcare', 'Applications of artificial intelligence in medical diagnosis')";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "SELECT COUNT(*) FROM articles_auto WHERE search_vector @@ to_tsquery('english', 'machine & learning')";
        object? count = cmd.ExecuteScalar();
        AssertEqual(1L, Convert.ToInt64(count!), "Should find 1 article about machine learning");

        cmd.CommandText = "SELECT COUNT(*) FROM articles_auto WHERE search_vector @@ to_tsquery('english', 'neural')";
        count = cmd.ExecuteScalar();
        AssertEqual(2L, Convert.ToInt64(count!), "Should find 2 articles mentioning neural");

        cmd.CommandText = "INSERT INTO articles_auto (title, content) VALUES ('Neural Network Optimization', 'Techniques for optimizing neural network training')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM articles_auto WHERE search_vector @@ to_tsquery('english', 'neural & network')";
        count = cmd.ExecuteScalar();
        AssertEqual(3L, Convert.ToInt64(count!), "Should find 3 articles with neural network (auto-updated search_vector)");

        cmd.CommandText = @"SELECT title, 
                           ts_rank(search_vector, to_tsquery('english', 'learning | intelligence')) as rank
                           FROM articles_auto
                           WHERE search_vector @@ to_tsquery('english', 'learning | intelligence')
                           ORDER BY rank DESC";
        int resultCount = 0;
        using (DbDataReader reader = cmd.ExecuteReader())
        {
            while (reader.Read())
            {
                resultCount++;
                string title = reader.GetString(0);
                double rank = reader.GetDouble(1);
                AssertTrue(rank > 0, "Rank should be positive");
            }
        }
        AssertTrue(resultCount >= 2, "Should find multiple articles about learning or intelligence");

        cmd.CommandText = "UPDATE articles_auto SET content = 'Advanced machine learning techniques and algorithms' WHERE title = 'Machine Learning Basics'";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM articles_auto WHERE search_vector @@ to_tsquery('english', 'advanced')";
        count = cmd.ExecuteScalar();
        AssertEqual(1L, Convert.ToInt64(count!), "Updated article should be searchable with new content");
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS articles_auto CASCADE";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());
    }
}
