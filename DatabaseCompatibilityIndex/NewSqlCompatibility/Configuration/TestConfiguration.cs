using System.Text.Json;
using System.Text.Json.Serialization;

namespace NSCI.Configuration;

public class TestConfiguration
{
    public GeneralConfiguration General { get; set; } = new();

    // The test runner does not use the "Databases" section from appsettings.json.
    // Instead, it discovers database configurations under a provided root folder.
    [JsonIgnore]
    public List<DatabaseConfiguration> Databases { get; private set; } = new();

    public static TestConfiguration Load(string path, string databaseRootPath)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"Configuration file not found: {path}");

        if (string.IsNullOrWhiteSpace(databaseRootPath))
            throw new ArgumentException("Database root folder must be provided (e.g. --db-root).", nameof(databaseRootPath));

        string json = File.ReadAllText(path);
        JsonSerializerOptions options = CreateJsonOptions();

        TestConfiguration config = JsonSerializer.Deserialize<TestConfiguration>(json, options)
            ?? throw new InvalidOperationException("Failed to deserialize configuration");

        config.Databases = LoadDatabaseConfigurations(databaseRootPath);
        return config;
    }

    public static TestConfiguration Load(string path)
        => Load(path, string.Empty);

    private static List<DatabaseConfiguration> LoadDatabaseConfigurations(string databaseRootPath)
    {
        if (!Directory.Exists(databaseRootPath))
            throw new DirectoryNotFoundException($"Database root directory not found: {databaseRootPath}");

        List<string> configFiles = Directory
            .EnumerateFiles(databaseRootPath, "db.config.json", SearchOption.AllDirectories)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (configFiles.Count == 0)
            throw new InvalidOperationException($"No db.config.json files found under: {databaseRootPath}");

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
