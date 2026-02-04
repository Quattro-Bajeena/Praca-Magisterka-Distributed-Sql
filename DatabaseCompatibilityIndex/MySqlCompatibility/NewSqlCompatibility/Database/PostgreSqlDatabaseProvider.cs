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
        return $"CREATE DATABASE \"{databaseName}\"";
    }

    public string GenerateSetDatabaseSql(string databaseName)
    {
        throw new NotSupportedException("PostgreSQL doesn't support USE statement. Include database name in connection string.");
    }
}
