using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Advanced;

[SqlTest(SqlFeatureCategory.Triggers, "Test TRIGGER")]
public class TriggerTest : SqlTest
{
    protected override void SetupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "CREATE TABLE trigger_test_table (id INT PRIMARY KEY, value INT)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"CREATE TRIGGER tr_test 
            BEFORE INSERT ON trigger_test_table 
            FOR EACH ROW 
            BEGIN 
                SET NEW.value = NEW.value * 2; 
            END";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "INSERT INTO trigger_test_table (id, value) VALUES (1, 5)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT value FROM trigger_test_table WHERE id = 1";
        object? result = cmd.ExecuteScalar();
        AssertEqual(10, Convert.ToInt32(result!), "Trigger should have doubled the value");
    }

    protected override void CleanupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TRIGGER IF EXISTS tr_test";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "DROP TABLE IF EXISTS trigger_test_table";
        cmd.ExecuteNonQuery();
    }

    protected override void SetupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "CREATE TABLE trigger_test_table (id INT PRIMARY KEY, value INT)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"CREATE OR REPLACE FUNCTION tr_test_func() RETURNS TRIGGER AS $$
            BEGIN
                NEW.value := NEW.value * 2;
                RETURN NEW;
            END;
            $$ LANGUAGE plpgsql";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"CREATE TRIGGER tr_test 
            BEFORE INSERT ON trigger_test_table 
            FOR EACH ROW 
            EXECUTE FUNCTION tr_test_func()";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "INSERT INTO trigger_test_table (id, value) VALUES (1, 5)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT value FROM trigger_test_table WHERE id = 1";
        object? result = cmd.ExecuteScalar();
        AssertEqual(10, Convert.ToInt32(result!), "Trigger should have doubled the value");
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TRIGGER IF EXISTS tr_test ON trigger_test_table";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "DROP FUNCTION IF EXISTS tr_test_func()";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "DROP TABLE IF EXISTS trigger_test_table";
        cmd.ExecuteNonQuery();
    }
}
