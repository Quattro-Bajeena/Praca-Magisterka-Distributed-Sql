using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.IsolationLevels;

[SqlTest(SqlFeatureCategory.Transactions, "Test locking difference between REPEATABLE READ and READ COMMITTED")]
public class LockingBehaviorDifferenceTest : SqlTest
{
    protected override void SetupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "CREATE TABLE lock_diff (a INT NOT NULL, b INT) ENGINE = InnoDB";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO lock_diff VALUES (1,2),(2,3),(3,2),(4,3),(5,2)";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd1 = connection.CreateCommand();
        using DbCommand cmd2 = connectionSecond.CreateCommand();

        cmd1.CommandText = "SET SESSION TRANSACTION ISOLATION LEVEL REPEATABLE READ";
        cmd1.ExecuteNonQuery();

        cmd2.CommandText = "SET SESSION TRANSACTION ISOLATION LEVEL REPEATABLE READ";
        cmd2.ExecuteNonQuery();

        cmd1.CommandText = "START TRANSACTION";
        cmd1.ExecuteNonQuery();

        cmd1.CommandText = "UPDATE lock_diff SET b = 5 WHERE b = 3";
        cmd1.ExecuteNonQuery();

        cmd1.CommandText = "SELECT a, b FROM lock_diff WHERE b = 5 ORDER BY a";
        using (DbDataReader reader = cmd1.ExecuteReader())
        {
            AssertTrue(reader.Read(), "Should have updated rows");
            AssertEqual(2, reader.GetInt32(0), "First updated row");
            AssertTrue(reader.Read(), "Should have second row");
            AssertEqual(4, reader.GetInt32(0), "Second updated row");
        }

        cmd1.CommandText = "COMMIT";
        cmd1.ExecuteNonQuery();

        cmd1.CommandText = "SET SESSION TRANSACTION ISOLATION LEVEL READ COMMITTED";
        cmd1.ExecuteNonQuery();

        cmd2.CommandText = "SET SESSION TRANSACTION ISOLATION LEVEL READ COMMITTED";
        cmd2.ExecuteNonQuery();

        cmd1.CommandText = "UPDATE lock_diff SET b = 3 WHERE b = 5";
        cmd1.ExecuteNonQuery();

        cmd1.CommandText = "START TRANSACTION";
        cmd1.ExecuteNonQuery();

        cmd1.CommandText = "UPDATE lock_diff SET b = 5 WHERE b = 3";
        cmd1.ExecuteNonQuery();

        cmd2.CommandText = "START TRANSACTION";
        cmd2.ExecuteNonQuery();

        cmd2.CommandText = "UPDATE lock_diff SET b = 4 WHERE b = 2";
        cmd2.ExecuteNonQuery();

        cmd2.CommandText = "SELECT COUNT(*) FROM lock_diff WHERE b = 4";
        object? count = cmd2.ExecuteScalar();
        AssertEqual(3L, Convert.ToInt64(count!), "Should have updated 3 rows with b=2 to b=4");

        cmd2.CommandText = "COMMIT";
        cmd2.ExecuteNonQuery();

        cmd1.CommandText = "COMMIT";
        cmd1.ExecuteNonQuery();

        cmd1.CommandText = "SELECT a, b FROM lock_diff ORDER BY a";
        using (DbDataReader reader = cmd1.ExecuteReader())
        {
            int rowCount = 0;
            while (reader.Read())
            {
                rowCount++;
                int a = reader.GetInt32(0);
                int b = reader.GetInt32(1);
                if (a == 2 || a == 4)
                {
                    AssertEqual(5, b, "Rows with a=2 or a=4 should have b=5");
                }
                else
                {
                    AssertEqual(4, b, "Rows with a=1, a=3, or a=5 should have b=4");
                }
            }
            AssertEqual(5, rowCount, "Should have 5 rows");
        }
    }

    protected override void CleanupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS lock_diff";
        cmd.ExecuteNonQuery();
    }
}
