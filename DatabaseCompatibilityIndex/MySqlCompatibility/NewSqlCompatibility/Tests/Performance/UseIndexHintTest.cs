using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Performance;

[SqlTest(SqlFeatureCategory.PerformanceHints, "Test USE INDEX hint", DatabaseType.MySql)]
public class UseIndexHintTest : SqlTest
{
    protected override void SetupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = @"CREATE TABLE users (
                            id INT PRIMARY KEY,
                            email VARCHAR(100),
                            username VARCHAR(50),
                            INDEX idx_email (email),
                            INDEX idx_username (username)
                        )";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO users VALUES (1, 'alice@example.com', 'alice')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO users VALUES (2, 'bob@example.com', 'bob')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO users VALUES (3, 'charlie@example.com', 'charlie')";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "SELECT * FROM users USE INDEX (idx_email) WHERE email = 'alice@example.com'";
        using (DbDataReader reader = cmd.ExecuteReader())
        {
            AssertTrue(reader.Read(), "Should find user with USE INDEX hint");
            AssertEqual("alice", reader.GetString(2), "Should retrieve correct username");
        }

        cmd.CommandText = "SELECT COUNT(*) FROM users USE INDEX (idx_email) WHERE email LIKE '%@example.com'";
        object? count = cmd.ExecuteScalar();
        AssertEqual(3L, Convert.ToInt64(count!), "USE INDEX should still return correct count");
    }

    protected override void CleanupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE users";
        cmd.ExecuteNonQuery();
    }
}
