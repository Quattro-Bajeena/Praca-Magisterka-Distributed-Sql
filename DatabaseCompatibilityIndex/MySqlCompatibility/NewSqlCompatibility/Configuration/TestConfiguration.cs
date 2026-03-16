using System.Text.Json;
using System.Text.Json.Serialization;

namespace NSCI.Configuration;

public class TestConfiguration
{
    public GeneralConfiguration General { get; set; } = new();
    public List<DatabaseConfiguration> Databases { get; set; } = new();

    public static TestConfiguration Load(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"Configuration file not found: {path}");

        string json = File.ReadAllText(path);
        JsonSerializerOptions options = new()
        {
            PropertyNameCaseInsensitive = true,
            Converters =
            {
                new JsonStringEnumConverter()
            }
        };

        return JsonSerializer.Deserialize<TestConfiguration>(json, options)
            ?? throw new InvalidOperationException("Failed to deserialize configuration");
    }
}
