using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.IsolationLevels;

[SqlTest(SqlFeatureCategory.Transactions, "Test READ COMMITTED releases non-matching row locks", DatabaseType.MySql)]
public class ReadCommittedLockReleaseTest : SqlTest
{
    protected override void SetupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "CREATE TABLE rc_lockrelease (id INT PRIMARY KEY, status VARCHAR(20), value INT)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO rc_lockrelease VALUES (1, 'active', 100), (2, 'inactive', 200), (3, 'active', 300)";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd1 = connection.CreateCommand();
        using DbCommand cmd2 = connectionSecond.CreateCommand();

        cmd1.CommandText = "SET SESSION TRANSACTION ISOLATION LEVEL READ COMMITTED";
        cmd1.ExecuteNonQuery();

        cmd2.CommandText = "SET SESSION TRANSACTION ISOLATION LEVEL READ COMMITTED";
        cmd2.ExecuteNonQuery();

        cmd1.CommandText = "START TRANSACTION";
        cmd1.ExecuteNonQuery();

        cmd1.CommandText = "UPDATE rc_lockrelease SET value = value + 10 WHERE status = 'active'";
        cmd1.ExecuteNonQuery();

        cmd1.CommandText = "SELECT value FROM rc_lockrelease WHERE status = 'active' ORDER BY id";
        using (DbDataReader reader = cmd1.ExecuteReader())
        {
            AssertTrue(reader.Read(), "Should have first active row");
            AssertEqual(110, reader.GetInt32(0), "First active row updated");
            AssertTrue(reader.Read(), "Should have second active row");
            AssertEqual(310, reader.GetInt32(0), "Second active row updated");
        }

        cmd2.CommandText = "START TRANSACTION";
        cmd2.ExecuteNonQuery();

        cmd2.CommandText = "UPDATE rc_lockrelease SET value = value + 50 WHERE status = 'inactive'";
        cmd2.ExecuteNonQuery();

        cmd2.CommandText = "SELECT value FROM rc_lockrelease WHERE id = 2";
        object? value = cmd2.ExecuteScalar();
        AssertEqual(250, Convert.ToInt32(value!), "Inactive row should be updated without blocking");

        cmd2.CommandText = "COMMIT";
        cmd2.ExecuteNonQuery();

        cmd1.CommandText = "COMMIT";
        cmd1.ExecuteNonQuery();

        cmd1.CommandText = "SELECT id, value FROM rc_lockrelease ORDER BY id";
        using (DbDataReader reader = cmd1.ExecuteReader())
        {
            AssertTrue(reader.Read(), "Row 1");
            AssertEqual(110, reader.GetInt32(1), "Row 1 value");
            AssertTrue(reader.Read(), "Row 2");
            AssertEqual(250, reader.GetInt32(1), "Row 2 value");
            AssertTrue(reader.Read(), "Row 3");
            AssertEqual(310, reader.GetInt32(1), "Row 3 value");
        }
    }

    protected override void CleanupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS rc_lockrelease";
        cmd.ExecuteNonQuery();
    }
}
