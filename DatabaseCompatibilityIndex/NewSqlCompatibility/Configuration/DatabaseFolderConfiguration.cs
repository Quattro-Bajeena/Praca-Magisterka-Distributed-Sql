using System.Text.Json;
using System.Text.Json.Serialization;

namespace NSCI.Configuration;

public class DatabaseFolderConfiguration
{
    public string Name { get; set; } = string.Empty;
    public string StartupType { get; set; } = string.Empty;
    public DatabaseType DatabaseType { get; set; } = DatabaseType.MySql;
    public bool Enabled { get; set; } = true;
    public List<DatabaseInstanceDefinition> Instances { get; set; } = new();

    public static DatabaseFolderConfiguration Load(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"Database configuration file not found: {path}");

        string json = File.ReadAllText(path);
        DatabaseFolderConfiguration config = JsonSerializer.Deserialize<DatabaseFolderConfiguration>(json, CreateJsonOptions())
            ?? throw new InvalidOperationException($"Failed to deserialize database configuration: {path}");

        Validate(config, path);
        return config;
    }

    public IEnumerable<DatabaseConfiguration> ToDatabaseConfigurations()
    {
        foreach (DatabaseInstanceDefinition instance in Instances)
        {
            yield return new DatabaseConfiguration
            {
                Name = instance.DisplayName,
                Enabled = Enabled && instance.Enabled,
                Type = DatabaseType,
                ConnectionString = instance.ConnectionString,
                Cleanup = true
            };
        }
    }

    private static void Validate(DatabaseFolderConfiguration config, string path)
    {
        if (string.IsNullOrWhiteSpace(config.Name))
            throw new InvalidOperationException($"Missing 'name' in database configuration: {path}");

        if (string.IsNullOrWhiteSpace(config.StartupType))
            throw new InvalidOperationException($"Missing 'startupType' in database configuration: {path}");

        if (config.Instances.Count == 0)
            throw new InvalidOperationException($"Database configuration must contain at least one instance: {path}");

        foreach (DatabaseInstanceDefinition instance in config.Instances)
        {
            if (string.IsNullOrWhiteSpace(instance.Id))
                throw new InvalidOperationException($"Instance is missing 'id' in database configuration: {path}");

            if (string.IsNullOrWhiteSpace(instance.DisplayName))
                throw new InvalidOperationException($"Instance '{instance.Id}' is missing 'displayName' in database configuration: {path}");

            if (string.IsNullOrWhiteSpace(instance.Version))
                throw new InvalidOperationException($"Instance '{instance.Id}' is missing 'version' in database configuration: {path}");

            if (string.IsNullOrWhiteSpace(instance.ConnectionString))
                throw new InvalidOperationException($"Instance '{instance.Id}' is missing 'connectionString' in database configuration: {path}");

            if (string.IsNullOrWhiteSpace(instance.HealthCheck.Host))
                throw new InvalidOperationException($"Instance '{instance.Id}' is missing healthCheck.host in database configuration: {path}");

            if (instance.HealthCheck.Port <= 0)
                throw new InvalidOperationException($"Instance '{instance.Id}' has invalid healthCheck.port in database configuration: {path}");
        }
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        JsonSerializerOptions options = new()
        {
            PropertyNameCaseInsensitive = true,
            Converters =
            {
                new JsonStringEnumConverter()
            }
        };

        return options;
    }
}

public class DatabaseInstanceDefinition
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public string ConnectionString { get; set; } = string.Empty;
    public DatabaseHealthCheckDefinition HealthCheck { get; set; } = new();
}

public class DatabaseHealthCheckDefinition
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
}
