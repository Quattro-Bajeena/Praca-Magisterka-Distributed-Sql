using NSCI.Configuration;

namespace NSCI.Database;

public static class DatabaseProviderFactory
{
    public static IDatabaseProvider Create(DatabaseType databaseType)
    {
        return databaseType switch
        {
            DatabaseType.MySql => new MySqlDatabaseProvider(),
            DatabaseType.PostgreSql => new PostgreSqlDatabaseProvider(),
            _ => throw new ArgumentException($"Unknown database type: {databaseType}")
        };
    }
}
