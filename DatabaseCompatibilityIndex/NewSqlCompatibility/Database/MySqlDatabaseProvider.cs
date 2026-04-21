using MySqlConnector;
using NSCI.Configuration;
using System.Data.Common;

namespace NSCI.Database;

public class MySqlDatabaseProvider : IDatabaseProvider
{

    readonly DatabaseConfiguration _configuration;

    public MySqlDatabaseProvider(DatabaseConfiguration configuration)
    {
        _configuration = configuration;
    }

    public DbConnection CreateConnection(string connectionString)
    {
        return new MySqlConnection(connectionString);
    }

    public string GenerateCreateDatabaseSql(string databaseName)
    {
        if (_configuration.Product == "Vitess")
        {
            // https://github.com/vitessio/website/blob/prod/content/en/docs/25.0/reference/compatibility/mysql-compatibility.md#createdrop-database
            Console.WriteLine("Vitess does not support CREATE DATABASE, skipping database creation. ");
            return string.Empty;
        }

        return $"CREATE DATABASE `{databaseName}` DEFAULT CHARSET utf8mb4 COLLATE utf8mb4_unicode_ci";
    }

    public string GenerateSetDatabaseSql(string databaseName)
    {
        return $"USE `{databaseName}`";
    }

    public string GenerateTestDatabaseName()
    {
        string suffix = DateTime.Now.ToString("s");
        return $"test_{suffix}";
    }
}
