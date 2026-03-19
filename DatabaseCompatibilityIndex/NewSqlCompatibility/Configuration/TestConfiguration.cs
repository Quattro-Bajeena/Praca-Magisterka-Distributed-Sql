using System.Text.Json;
using System.Text.Json.Serialization;

namespace NSCI.Configuration;

public class TestConfiguration
{
    public GeneralConfiguration General { get; set; } = new();

    public List<DatabaseConfiguration> Databases { get; set; } = new();

    public static TestConfiguration Load(string path, string? databaseRootPath = null)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"Configuration file not found: {path}");

        string json = File.ReadAllText(path);
        JsonSerializerOptions options = CreateJsonOptions();

        TestConfiguration config = JsonSerializer.Deserialize<TestConfiguration>(json, options)
            ?? throw new InvalidOperationException("Failed to deserialize configuration");

        if (!string.IsNullOrWhiteSpace(databaseRootPath))
            config.Databases.AddRange(LoadDatabaseConfigurations(databaseRootPath));

        return config;
    }

    private static List<DatabaseConfiguration> LoadDatabaseConfigurations(string databaseRootPath)
    {
        if (!Directory.Exists(databaseRootPath))
        {
            Console.WriteLine($"Warning: Database root directory not found: {databaseRootPath}");
            return new List<DatabaseConfiguration>();
        }

        List<string> configFiles = Directory
            .EnumerateFiles(databaseRootPath, "db.config.json", SearchOption.AllDirectories)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (configFiles.Count == 0)
        {
            Console.WriteLine($"Warning: No db.config.json files found under: {databaseRootPath}");
            return new List<DatabaseConfiguration>();
        }

        List<DatabaseConfiguration> databases = new();

        foreach (string configFile in configFiles)
        {
            DatabaseFolderConfiguration folderConfiguration = DatabaseFolderConfiguration.Load(configFile);
            databases.AddRange(folderConfiguration.ToDatabaseConfigurations());
        }

        return databases;
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
