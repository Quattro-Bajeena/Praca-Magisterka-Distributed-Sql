using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.BasicQueries;

[SqlTest(SqlFeatureCategory.BasicQueries, "Test PostgreSQL DO blocks (anonymous code blocks)", DatabaseType.PostgreSql)]
public class PostgresDoBlockTest : SqlTest
{
    protected override void SetupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        
        cmd.CommandText = @"CREATE TABLE do_test (
                            id SERIAL PRIMARY KEY,
                            counter INT DEFAULT 0
                        )";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO do_test (counter) VALUES (0)";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = @"DO $$
                           BEGIN
                               UPDATE do_test SET counter = counter + 10;
                           END;
                           $$";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT counter FROM do_test LIMIT 1";
        object? counter = cmd.ExecuteScalar();
        AssertEqual(10, Convert.ToInt32(counter!), "Counter should be incremented to 10");

        cmd.CommandText = @"DO $$
                           DECLARE
                               i INT;
                           BEGIN
                               FOR i IN 1..5 LOOP
                                   UPDATE do_test SET counter = counter + i;
                               END LOOP;
                           END;
                           $$";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT counter FROM do_test LIMIT 1";
        counter = cmd.ExecuteScalar();
        AssertEqual(25, Convert.ToInt32(counter!), "Counter should be 25 (10 + 1+2+3+4+5)");

        cmd.CommandText = @"DO $$
                           BEGIN
                               IF (SELECT counter FROM do_test LIMIT 1) > 20 THEN
                                   INSERT INTO do_test (counter) VALUES (100);
                               END IF;
                           END;
                           $$";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM do_test";
        object? count = cmd.ExecuteScalar();
        AssertEqual(2L, Convert.ToInt64(count!), "Should have 2 rows after conditional insert");

        cmd.CommandText = "SELECT counter FROM do_test WHERE counter = 100";
        object? newCounter = cmd.ExecuteScalar();
        AssertEqual(100, Convert.ToInt32(newCounter!), "New row should have counter = 100");

        cmd.CommandText = @"DO $body$
                           DECLARE
                               total INT;
                           BEGIN
                               SELECT SUM(counter) INTO total FROM do_test;
                               INSERT INTO do_test (counter) VALUES (total);
                           END;
                           $body$";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM do_test";
        count = cmd.ExecuteScalar();
        AssertEqual(3L, Convert.ToInt64(count!), "Should have 3 rows");

        cmd.CommandText = "SELECT MAX(counter) FROM do_test";
        object? maxCounter = cmd.ExecuteScalar();
        AssertEqual(125, Convert.ToInt32(maxCounter!), "Max counter should be 125 (25 + 100)");
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS do_test CASCADE";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());
    }
}
