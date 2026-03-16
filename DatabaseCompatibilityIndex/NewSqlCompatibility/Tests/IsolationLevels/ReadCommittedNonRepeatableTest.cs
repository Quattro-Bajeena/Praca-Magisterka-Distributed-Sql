using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.IsolationLevels;

[SqlTest(SqlFeatureCategory.Transactions, "Test READ COMMITTED allows non-repeatable reads")]
public class ReadCommittedNonRepeatableTest : SqlTest
{
    protected override void SetupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "CREATE TABLE rc_nonrepeatable (id INT PRIMARY KEY, value INT)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO rc_nonrepeatable VALUES (1, 100)";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd1 = connection.CreateCommand();
        using DbCommand cmd2 = connectionSecond.CreateCommand();

        cmd1.CommandText = "SET SESSION TRANSACTION ISOLATION LEVEL READ COMMITTED";
        cmd1.ExecuteNonQuery();

        cmd1.CommandText = "START TRANSACTION";
        cmd1.ExecuteNonQuery();

        cmd1.CommandText = "SELECT value FROM rc_nonrepeatable WHERE id = 1";
        object? firstRead = cmd1.ExecuteScalar();
        AssertEqual(100, Convert.ToInt32(firstRead!), "First read should see initial value");

        cmd2.CommandText = "START TRANSACTION";
        cmd2.ExecuteNonQuery();

        cmd2.CommandText = "UPDATE rc_nonrepeatable SET value = 200 WHERE id = 1";
        cmd2.ExecuteNonQuery();

        cmd2.CommandText = "COMMIT";
        cmd2.ExecuteNonQuery();

        cmd1.CommandText = "SELECT value FROM rc_nonrepeatable WHERE id = 1";
        object? secondRead = cmd1.ExecuteScalar();
        AssertEqual(200, Convert.ToInt32(secondRead!), "Second read should see committed change");

        cmd2.CommandText = "UPDATE rc_nonrepeatable SET value = 300 WHERE id = 1";
        cmd2.ExecuteNonQuery();

        cmd1.CommandText = "SELECT value FROM rc_nonrepeatable WHERE id = 1";
        object? thirdRead = cmd1.ExecuteScalar();
        AssertEqual(300, Convert.ToInt32(thirdRead!), "Third read should see latest committed value");

        cmd1.CommandText = "COMMIT";
        cmd1.ExecuteNonQuery();
    }

    protected override void CleanupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS rc_nonrepeatable";
        cmd.ExecuteNonQuery();
    }

    protected override void SetupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "CREATE TABLE rc_nonrepeatable (id INT PRIMARY KEY, value INT)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO rc_nonrepeatable VALUES (1, 100)";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd1 = connection.CreateCommand();
        using DbCommand cmd2 = connectionSecond.CreateCommand();

        cmd1.CommandText = "SET TRANSACTION ISOLATION LEVEL READ COMMITTED";
        cmd1.ExecuteNonQuery();

        cmd1.CommandText = "BEGIN";
        cmd1.ExecuteNonQuery();

        cmd1.CommandText = "SELECT value FROM rc_nonrepeatable WHERE id = 1";
        object? firstRead = cmd1.ExecuteScalar();
        AssertEqual(100, Convert.ToInt32(firstRead!), "First read should see initial value");

        cmd2.CommandText = "BEGIN";
        cmd2.ExecuteNonQuery();

        cmd2.CommandText = "UPDATE rc_nonrepeatable SET value = 200 WHERE id = 1";
        cmd2.ExecuteNonQuery();

        cmd2.CommandText = "COMMIT";
        cmd2.ExecuteNonQuery();

        cmd1.CommandText = "SELECT value FROM rc_nonrepeatable WHERE id = 1";
        object? secondRead = cmd1.ExecuteScalar();
        AssertEqual(200, Convert.ToInt32(secondRead!), "Second read should see committed change");

        cmd2.CommandText = "UPDATE rc_nonrepeatable SET value = 300 WHERE id = 1";
        cmd2.ExecuteNonQuery();

        cmd1.CommandText = "SELECT value FROM rc_nonrepeatable WHERE id = 1";
        object? thirdRead = cmd1.ExecuteScalar();
        AssertEqual(300, Convert.ToInt32(thirdRead!), "Third read should see latest committed value");

        cmd1.CommandText = "COMMIT";
        cmd1.ExecuteNonQuery();
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS rc_nonrepeatable";
        cmd.ExecuteNonQuery();
    }
}
