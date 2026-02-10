using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Transactions;

[SqlTest(SqlFeatureCategory.Transactions, "Test PostgreSQL subtransactions with nested savepoints", DatabaseType.PostgreSql)]
public class PostgresNestedSavepointsTest : SqlTest
{
    protected override void SetupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = @"CREATE TABLE nested_savepoint_test (
                            id INT PRIMARY KEY,
                            level INT,
                            value VARCHAR(100)
                        )";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "BEGIN";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO nested_savepoint_test VALUES (1, 1, 'Level 1')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SAVEPOINT sp_level1";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO nested_savepoint_test VALUES (2, 2, 'Level 2')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SAVEPOINT sp_level2";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO nested_savepoint_test VALUES (3, 3, 'Level 3')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SAVEPOINT sp_level3";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO nested_savepoint_test VALUES (4, 4, 'Level 4')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "ROLLBACK TO SAVEPOINT sp_level3";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM nested_savepoint_test";
        object? count1 = cmd.ExecuteScalar();
        AssertEqual(3L, Convert.ToInt64(count1!), "Should have 3 rows after first rollback");

        cmd.CommandText = "ROLLBACK TO SAVEPOINT sp_level2";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM nested_savepoint_test";
        object? count2 = cmd.ExecuteScalar();
        AssertEqual(2L, Convert.ToInt64(count2!), "Should have 2 rows after second rollback");

        cmd.CommandText = "INSERT INTO nested_savepoint_test VALUES (5, 2, 'Level 2 New')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SAVEPOINT sp_level2_new";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO nested_savepoint_test VALUES (6, 3, 'Level 3 New')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "RELEASE SAVEPOINT sp_level2_new";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "COMMIT";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM nested_savepoint_test";
        object? finalCount = cmd.ExecuteScalar();
        AssertEqual(4L, Convert.ToInt64(finalCount!), "Should have 4 rows after commit");

        cmd.CommandText = "SELECT COUNT(*) FROM nested_savepoint_test WHERE level = 1";
        object? level1Count = cmd.ExecuteScalar();
        AssertEqual(1L, Convert.ToInt64(level1Count!), "Should have 1 level 1 row");

        cmd.CommandText = "SELECT COUNT(*) FROM nested_savepoint_test WHERE level = 2";
        object? level2Count = cmd.ExecuteScalar();
        AssertEqual(2L, Convert.ToInt64(level2Count!), "Should have 2 level 2 rows");

        cmd.CommandText = "SELECT COUNT(*) FROM nested_savepoint_test WHERE level = 3";
        object? level3Count = cmd.ExecuteScalar();
        AssertEqual(1L, Convert.ToInt64(level3Count!), "Should have 1 level 3 row");

        cmd.CommandText = "SELECT COUNT(*) FROM nested_savepoint_test WHERE level = 4";
        object? level4Count = cmd.ExecuteScalar();
        AssertEqual(0L, Convert.ToInt64(level4Count!), "Should have 0 level 4 rows (rolled back)");
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS nested_savepoint_test CASCADE";
        cmd.ExecuteNonQuery();
    }
}
