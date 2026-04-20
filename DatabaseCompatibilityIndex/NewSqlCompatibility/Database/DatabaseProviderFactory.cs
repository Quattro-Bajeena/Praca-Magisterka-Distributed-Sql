using NSCI.Configuration;

namespace NSCI.Database;

public static class DatabaseProviderFactory
{
    public static IDatabaseProvider Create(DatabaseConfiguration config)
    {
        return config.Type switch
        {
            DatabaseType.MySql => new MySqlDatabaseProvider(),
            DatabaseType.PostgreSql => new PostgreSqlDatabaseProvider(config),
            _ => throw new ArgumentException($"Unknown database type: {config.Type}")
        };
    }
}
