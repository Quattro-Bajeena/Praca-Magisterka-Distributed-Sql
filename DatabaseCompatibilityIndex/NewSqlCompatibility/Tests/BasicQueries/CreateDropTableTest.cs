using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.BasicQueries;

[SqlTest(SqlFeatureCategory.BasicQueries, "Create and drop simple table", 1)]
public class CreateDropTableTest : SqlTest
{
    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "CREATE TABLE test_table (id INT PRIMARY KEY, name VARCHAR(100))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO test_table (id, name) VALUES (1, 'test')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "DROP TABLE test_table";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "CREATE TABLE test_table (id INT PRIMARY KEY, name VARCHAR(100))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO test_table (id, name) VALUES (1, 'test')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "DROP TABLE test_table";
        cmd.ExecuteNonQuery();
    }
}
