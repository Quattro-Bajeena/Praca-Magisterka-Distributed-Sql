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

    public string GenerateUseDatabaseSql(string databaseName)
    {
        // PostgreSQL doesn't have USE statement, connection string must specify the database
        throw new NotSupportedException("PostgreSQL doesn't support USE statement. Include database name in connection string.");
    }
}
