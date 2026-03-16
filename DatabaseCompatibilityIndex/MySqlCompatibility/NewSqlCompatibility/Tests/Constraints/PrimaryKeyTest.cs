using MySqlConnector;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Constraints;

[SqlTest(SqlFeatureCategory.Constraints, "Test PRIMARY KEY constraint")]
public class PrimaryKeyTest : SqlTest
{
    protected override string? SetupCommandMy => "CREATE TABLE pk_test (id INT PRIMARY KEY, name VARCHAR(50))";

    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "INSERT INTO pk_test VALUES (1, 'Alice')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO pk_test VALUES (1, 'Bob')";
        AssertThrows<MySqlException>(() => cmd.ExecuteNonQuery(), "Should throw exception for primary key constraint violation");
    }

    protected override string? CleanupCommandMy => "DROP TABLE pk_test";

    protected override string? SetupCommandPg => "CREATE TABLE pk_test (id INT PRIMARY KEY, name VARCHAR(50))";

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "INSERT INTO pk_test VALUES (1, 'Alice')";
        cmd.ExecuteNonQuery();

        bool errorOccurred = false;
        try
        {
            cmd.CommandText = "INSERT INTO pk_test VALUES (1, 'Bob')";
            cmd.ExecuteNonQuery();
        }
        catch
        {
            errorOccurred = true;
        }

        AssertTrue(errorOccurred, "Should throw exception for primary key constraint violation");
    }

    protected override string? CleanupCommandPg => "DROP TABLE pk_test";
}
