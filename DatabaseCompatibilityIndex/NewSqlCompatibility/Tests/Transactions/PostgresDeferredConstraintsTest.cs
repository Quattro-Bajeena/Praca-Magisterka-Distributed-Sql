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
                            CONSTRAINT fk_parent FOREIGN KEY (parent_id)
                                REFERENCES parent_deferred(id)
                                DEFERRABLE INITIALLY DEFERRED
                        )";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        // Insert child before parent — allowed because constraint is INITIALLY DEFERRED
        cmd.CommandText = "BEGIN";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO child_deferred (id, parent_id) VALUES (1, 100)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO parent_deferred (id, name) VALUES (100, 'Parent')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "COMMIT";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM child_deferred WHERE parent_id = 100";
        object? count = cmd.ExecuteScalar();
        AssertEqual(1L, Convert.ToInt64(count!), "Deferred constraint should allow child to be inserted before parent");
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
