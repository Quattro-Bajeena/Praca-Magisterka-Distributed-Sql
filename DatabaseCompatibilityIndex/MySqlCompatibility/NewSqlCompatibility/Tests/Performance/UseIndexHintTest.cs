using NSCI.Testing;using NSCI.Configuration;
using System.Data.Common;

namespace NSCI.Tests.Performance;

[SqlTest(SqlFeatureCategory.Indexes, "Test USE INDEX hint", DatabaseType.MySql)]
public class UseIndexHintTest : SqlTest
{
    public override void Setup(DbConnection connection)
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

    public override void Execute(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "SELECT * FROM users USE INDEX (idx_email) WHERE email = 'alice@example.com'";
        using DbDataReader reader = cmd.ExecuteReader();
        AssertTrue(reader.Read(), "Should find user with USE INDEX hint");
        AssertEqual("alice", reader.GetString(2), "Should retrieve correct username");

        cmd.CommandText = "SELECT COUNT(*) FROM users USE INDEX (idx_email) WHERE email LIKE '%@example.com'";
        object? count = cmd.ExecuteScalar();
        AssertEqual(3L, (long)count!, "USE INDEX should still return correct count");
    }

    public override void Cleanup(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE users";
        cmd.ExecuteNonQuery();
    }
}
