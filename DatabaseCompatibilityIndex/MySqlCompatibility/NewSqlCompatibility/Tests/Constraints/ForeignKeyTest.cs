using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Constraints;

[SqlTest(SqlFeatureCategory.Constraints, "Test FOREIGN KEY constraint", DatabaseType.MySql)]
public class ForeignKeyTest : SqlTest
{
    public override void Setup(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "CREATE TABLE parent_table (id INT PRIMARY KEY, name VARCHAR(50))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "CREATE TABLE child_table (id INT PRIMARY KEY, parent_id INT, FOREIGN KEY (parent_id) REFERENCES parent_table(id))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO parent_table VALUES (1, 'Parent')";
        cmd.ExecuteNonQuery();
    }

    public override void Execute(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "INSERT INTO child_table VALUES (1, 1)";
        cmd.ExecuteNonQuery();

        // Try to insert invalid foreign key - expected to fail
        cmd.CommandText = "INSERT INTO child_table VALUES (2, 999)";
        cmd.ExecuteNonQuery();
    }

    public override void Cleanup(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE child_table";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "DROP TABLE parent_table";
        cmd.ExecuteNonQuery();
    }
}
