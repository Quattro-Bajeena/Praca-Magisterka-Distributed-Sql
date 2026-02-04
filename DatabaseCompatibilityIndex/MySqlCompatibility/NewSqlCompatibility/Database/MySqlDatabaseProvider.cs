using MySqlConnector;
using System.Data.Common;

namespace NSCI.Database;

public class MySqlDatabaseProvider : IDatabaseProvider
{
    public DbConnection CreateConnection(string connectionString)
    {
        return new MySqlConnection(connectionString);
    }

    public string GenerateCreateDatabaseSql(string databaseName)
    {
        return $"CREATE DATABASE `{databaseName}` DEFAULT CHARSET utf8mb4 COLLATE utf8mb4_unicode_ci";
    }

    public string GenerateSetDatabaseSql(string databaseName)
    {
        return $"USE `{databaseName}`";
    }
}
