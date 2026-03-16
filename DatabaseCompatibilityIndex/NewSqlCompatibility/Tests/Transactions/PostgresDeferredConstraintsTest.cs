using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Transactions;

[SqlTest(SqlFeatureCategory.Transactions, "Test PostgreSQL deferred constraints", DatabaseType.PostgreSql)]
public class PostgresDeferredConstraintsTest : SqlTest
{
    protected override void SetupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = @"CREATE TABLE parent_deferred (
                            id INT PRIMARY KEY,
                            name VARCHAR(100)
                        )";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"CREATE TABLE child_deferred (
                            id INT PRIMARY KEY,
                            parent_id INT,
                            name VARCHAR(100),
                            CONSTRAINT fk_parent FOREIGN KEY (parent_id) 
                                REFERENCES parent_deferred(id) 
                                DEFERRABLE INITIALLY DEFERRED
                        )";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "BEGIN";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO child_deferred (id, parent_id, name) VALUES (1, 100, 'Child First')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO parent_deferred (id, name) VALUES (100, 'Parent After')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "COMMIT";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM child_deferred WHERE parent_id = 100";
        object? childCount = cmd.ExecuteScalar();
        AssertEqual(1L, Convert.ToInt64(childCount!), "Deferred constraint should allow child before parent");

        cmd.CommandText = "BEGIN";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SET CONSTRAINTS fk_parent IMMEDIATE";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO parent_deferred (id, name) VALUES (200, 'Parent Immediate')";
        cmd.ExecuteNonQuery();

        bool exceptionThrown = false;
        try
        {
            cmd.CommandText = "INSERT INTO child_deferred (id, parent_id, name) VALUES (2, 999, 'Invalid Child')";
            cmd.ExecuteNonQuery();
        }
        catch
        {
            exceptionThrown = true;
        }

        if (exceptionThrown)
        {
            cmd.CommandText = "ROLLBACK";
            cmd.ExecuteNonQuery();
        }
        else
        {
            cmd.CommandText = "COMMIT";
            cmd.ExecuteNonQuery();
        }

        AssertTrue(exceptionThrown, "Immediate constraint should fail on invalid foreign key");

        cmd.CommandText = "BEGIN";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SET CONSTRAINTS ALL DEFERRED";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO child_deferred (id, parent_id, name) VALUES (3, 300, 'Child 3')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO parent_deferred (id, name) VALUES (300, 'Parent 3')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "COMMIT";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM parent_deferred";
        object? parentCount = cmd.ExecuteScalar();
        AssertTrue(Convert.ToInt64(parentCount!) >= 2, "Should have multiple parents");
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS child_deferred CASCADE";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "DROP TABLE IF EXISTS parent_deferred CASCADE";
        cmd.ExecuteNonQuery();
    }
}
