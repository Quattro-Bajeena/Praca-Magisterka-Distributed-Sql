using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.BasicQueries;

[SqlTest(SqlFeatureCategory.BasicQueries, "Create and drop simple table", DatabaseType.MySql)]
public class CreateDropTableTest : SqlTest
{
    public override void Execute(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        // Create table
        cmd.CommandText = "CREATE TABLE test_table (id INT PRIMARY KEY, name VARCHAR(100))";
        cmd.ExecuteNonQuery();

        // Verify table exists by inserting data
        cmd.CommandText = "INSERT INTO test_table (id, name) VALUES (1, 'test')";
        cmd.ExecuteNonQuery();

        // Drop table
        cmd.CommandText = "DROP TABLE test_table";
        cmd.ExecuteNonQuery();
    }
}
