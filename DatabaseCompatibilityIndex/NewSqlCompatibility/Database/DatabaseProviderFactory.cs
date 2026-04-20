using NSCI.Configuration;

namespace NSCI.Database;

public static class DatabaseProviderFactory
{
    public static IDatabaseProvider Create(DatabaseConfiguration configuration)
    {
        return configuration.Type switch
        {
            DatabaseType.MySql => new MySqlDatabaseProvider(configuration),
            DatabaseType.PostgreSql => new PostgreSqlDatabaseProvider(configuration),
            _ => throw new ArgumentException($"Unknown database type: {configuration.Type}")
        };
    }
}
