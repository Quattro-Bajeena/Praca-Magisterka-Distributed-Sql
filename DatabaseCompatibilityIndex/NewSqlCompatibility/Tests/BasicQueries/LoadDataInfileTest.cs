using MySqlConnector;
using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.BasicQueries;

[SqlTest(SqlFeatureCategory.BasicQueries, "Bulk insert rows from CSV file using LOAD DATA LOCAL INFILE", DatabaseType.MySql)]
public class LoadDataInfileTest : SqlTest
{
    private const int RowCount = 1000;
    private string _tempFilePath = "";

    protected override void SetupMy(DbConnection connection)
    {
        using DbCommand setLocalInfile = connection.CreateCommand();
        setLocalInfile.CommandText = "SET GLOBAL local_infile = 'ON'";
        setLocalInfile.ExecuteNonQuery();

        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = @"CREATE TABLE bulk_load_data (
            id INT NOT NULL,
            name VARCHAR(100) NOT NULL,
            value DECIMAL(10,2) NOT NULL
        )";
        cmd.ExecuteNonQuery();

        _tempFilePath = Path.Combine(Path.GetTempPath(), $"nsci_bulk_{Guid.NewGuid():N}.csv");
        using StreamWriter writer = new StreamWriter(_tempFilePath) { NewLine = "\n" };
        for (int i = 1; i <= RowCount; i++)
        {
            writer.WriteLine($"{i},Item{i},{(i * 1.5m):F2}");
        }
    }

    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        MySqlConnectionStringBuilder builder = new MySqlConnectionStringBuilder(_config.ConnectionString)
        {
            AllowLoadLocalInfile = true
        };

        using MySqlConnection loadConnection = new MySqlConnection(builder.ConnectionString);
        loadConnection.Open();

        using MySqlCommand useCmd = loadConnection.CreateCommand();
        useCmd.CommandText = $"USE `{_config.DatabaseName}`";
        useCmd.ExecuteNonQuery();

        string filePath = _tempFilePath.Replace('\\', '/');

        // Using bulk loader because normal query requirires SSL connection
        MySqlBulkLoader bulkLoader = new MySqlBulkLoader(loadConnection)
        {
            TableName = "bulk_load_data",
            FieldTerminator = ",",
            LineTerminator = "\n",
            FileName = filePath,
            Local = true
        };
        bulkLoader.Columns.Add("id");
        bulkLoader.Columns.Add("name");
        bulkLoader.Columns.Add("value");

        var rowsLoadedObj = bulkLoader.Load();
        int rowsLoaded = Convert.ToInt32(rowsLoadedObj);
        AssertEqual(RowCount, rowsLoaded, $"LOAD DATA should report {RowCount} rows loaded");

        using DbCommand cmd = loadConnection.CreateCommand();

        cmd.CommandText = "SELECT COUNT(*) FROM bulk_load_data";
        object? count = cmd.ExecuteScalar();
        AssertEqual((long)RowCount, Convert.ToInt64(count!), $"Should have {RowCount} rows after LOAD DATA");

        cmd.CommandText = "SELECT name FROM bulk_load_data WHERE id = 500";
        object? name = cmd.ExecuteScalar();
        AssertEqual("Item500", (string)name!, "Row 500 should have name 'Item500'");

        cmd.CommandText = "SELECT value FROM bulk_load_data WHERE id = 1";
        object? firstValue = cmd.ExecuteScalar();
        AssertEqual(1.50m, Convert.ToDecimal(firstValue!), "First row value should be 1.50");
    }

    protected override void CleanupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS bulk_load_data";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());

        if (File.Exists(_tempFilePath))
            ExecuteIgnoringException(() => File.Delete(_tempFilePath));
    }
}
