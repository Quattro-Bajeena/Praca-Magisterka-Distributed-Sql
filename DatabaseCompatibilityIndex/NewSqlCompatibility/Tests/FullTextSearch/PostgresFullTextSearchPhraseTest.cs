using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.FullTextSearch;

[SqlTest(SqlFeatureCategory.FullTextSearch, "Test PostgreSQL full-text phrase search with phraseto_tsquery", DatabaseType.PostgreSql)]
public class PostgresFullTextSearchPhraseTest : SqlTest
{
    protected override void SetupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = @"CREATE TABLE documents_pg (
                            doc_id SERIAL PRIMARY KEY,
                            body TEXT
                        )";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"INSERT INTO documents_pg (body) VALUES
                            ('Tips for optimizing database queries'),
                            ('Advanced techniques in query optimization'),
                            ('Using databases for data analysis')";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = @"SELECT COUNT(*) FROM documents_pg
                           WHERE to_tsvector('english', body) @@ phraseto_tsquery('english', 'query optimization')";
        object? count = cmd.ExecuteScalar();
        AssertEqual(1L, Convert.ToInt64(count!), "Should find 1 document with phrase 'query optimization'");
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS documents_pg CASCADE";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());
    }
}
