using Npgsql;
using System.Data.Common;

namespace NSCI.Database;

public class PostgreSqlDatabaseProvider : IDatabaseProvider
{
    public DbConnection CreateConnection(string connectionString)
    {
        return new NpgsqlConnection(connectionString);
    }

    public string GenerateCreateDatabaseSql(string databaseName)
    {
        return $"CREATE SCHEMA \"{databaseName}\"";
    }

    public string GenerateSetDatabaseSql(string databaseName)
    {
        return $"SET search_path TO \"{databaseName}\",public";
    }
}
