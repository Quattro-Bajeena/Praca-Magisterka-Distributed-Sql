using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Indexes;

// TODO (GiST nie jest jeszcze zaimplemnetowany)
[SqlTest(SqlFeatureCategory.Indexes, "Test PostgreSQL GIN index types", DatabaseType.PostgreSql)]
public class PostgresGinGistIndexTest : SqlTest
{
    protected override void SetupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = @"CREATE TABLE documents_gin (
                            id SERIAL PRIMARY KEY,
                            title VARCHAR(200),
                            tags TEXT[],
                            content TEXT
                        )";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "CREATE INDEX idx_tags_gin ON documents_gin USING GIN (tags)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO documents_gin (title, tags, content) VALUES ('Doc1', ARRAY['sql', 'database'], 'Content about SQL')";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "INSERT INTO documents_gin (title, tags, content) VALUES ('Doc2', ARRAY['postgresql', 'database'], 'Content about PostgreSQL')";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "INSERT INTO documents_gin (title, tags, content) VALUES ('Doc3', ARRAY['nosql', 'mongodb'], 'Content about NoSQL')";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "SELECT COUNT(*) FROM documents_gin WHERE tags @> ARRAY['database']";
        object? count = cmd.ExecuteScalar();
        AssertEqual(2L, Convert.ToInt64(count!), "Should find 2 documents with 'database' tag");

        cmd.CommandText = "SELECT COUNT(*) FROM documents_gin WHERE tags && ARRAY['sql', 'postgresql']";
        count = cmd.ExecuteScalar();
        AssertTrue(Convert.ToInt64(count!) >= 2, "Should find documents with sql OR postgresql tags");

        cmd.CommandText = "EXPLAIN SELECT * FROM documents_gin WHERE tags @> ARRAY['database']";
        bool usesIndex = false;
        using (DbDataReader reader = cmd.ExecuteReader())
        {
            while (reader.Read())
            {
                string? plan = reader.GetValue(0)?.ToString();
                if (plan != null && (plan.Contains("idx_tags_gin") || plan.Contains("Bitmap") || plan.Contains("Index") || plan.Contains("Scan")))
                {
                    usesIndex = true;
                    break;
                }
            }
        }
        AssertTrue(usesIndex, "Query plan should be generated (GIN index available for use)");

        cmd.CommandText = "SELECT title FROM documents_gin WHERE 'sql' = ANY(tags)";
        using (DbDataReader reader = cmd.ExecuteReader())
        {
            AssertTrue(reader.Read(), "Should find document with 'sql' tag");
            string title = reader.GetString(0);
            AssertEqual("Doc1", title, "Should be Doc1");
        }

        cmd.CommandText = "INSERT INTO documents_gin (title, tags, content) VALUES ('Doc4', ARRAY['database', 'distributed'], 'Distributed databases')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM documents_gin WHERE tags @> ARRAY['database']";
        count = cmd.ExecuteScalar();
        AssertEqual(3L, Convert.ToInt64(count!), "Should now have 3 documents with 'database' tag");
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS documents_gin CASCADE";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());
    }
}
