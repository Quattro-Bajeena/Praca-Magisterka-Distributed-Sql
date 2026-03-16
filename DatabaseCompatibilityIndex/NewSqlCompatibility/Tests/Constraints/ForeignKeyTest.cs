using MySqlConnector;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Constraints;

[SqlTest(SqlFeatureCategory.Constraints, "Test FOREIGN KEY constraint")]
public class ForeignKeyTest : SqlTest
{
    protected override void SetupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "CREATE TABLE parent_table (id INT PRIMARY KEY, name VARCHAR(50))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "CREATE TABLE child_table (id INT PRIMARY KEY, parent_id INT, FOREIGN KEY (parent_id) REFERENCES parent_table(id))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO parent_table VALUES (1, 'Parent')";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "INSERT INTO child_table VALUES (1, 1)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO child_table VALUES (2, 999)";
        AssertThrows<MySqlException>(() => cmd.ExecuteNonQuery(), "Should throw exception for foreign key constraint violation");
    }

    protected override void CleanupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE child_table";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "DROP TABLE parent_table";
        cmd.ExecuteNonQuery();
    }

    protected override void SetupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "CREATE TABLE parent_table (id INT PRIMARY KEY, name VARCHAR(50))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "CREATE TABLE child_table (id INT PRIMARY KEY, parent_id INT, FOREIGN KEY (parent_id) REFERENCES parent_table(id))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO parent_table VALUES (1, 'Parent')";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "INSERT INTO child_table VALUES (1, 1)";
        cmd.ExecuteNonQuery();

        bool errorOccurred = false;
        try
        {
            cmd.CommandText = "INSERT INTO child_table VALUES (2, 999)";
            cmd.ExecuteNonQuery();
        }
        catch
        {
            errorOccurred = true;
        }

        AssertTrue(errorOccurred, "Should throw exception for foreign key constraint violation");
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE child_table";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "DROP TABLE parent_table";
        cmd.ExecuteNonQuery();
    }
}
