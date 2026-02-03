using NSCI.Configuration;
using NSCI.Testing;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NSCI.Reporting;

public static class JsonReportGenerator
{
    public static void GenerateReport(
        string outputPath,
        List<TestResult> results,
        DatabaseConfiguration databaseType,
        string databaseName)
    {
        JsonReport report = BuildReport(results, databaseType, databaseName);

        JsonSerializerOptions options = new()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        string json = JsonSerializer.Serialize(report, options);
        File.WriteAllText(outputPath, json);

        Console.WriteLine($"\n✓ Report generated: {outputPath}");
    }

    private static JsonReport BuildReport(List<TestResult> results, DatabaseConfiguration configuration, string databaseName)
    {
        JsonReportSummary summary = new(
            Total: results.Count,
            Passed: results.Count(r => r.Passed),
            Failed: results.Count(r => !r.Passed)
        );

        Dictionary<string, JsonReportCategory> resultsByCategory = BuildCategoryGroups(results);

        return new JsonReport(
            GeneratedAt: DateTime.UtcNow.ToString("O"),
            DatabaseType: configuration.Type.ToString(),
            ConfigurationName: configuration.Name,
            DatabaseName: databaseName,
            ConnectionString: configuration.ConnectionString,
            Summary: summary,
            ResultsByCategory: resultsByCategory
        );
    }

    private static Dictionary<string, JsonReportCategory> BuildCategoryGroups(List<TestResult> results)
    {
        return results
            .GroupBy(r => r.Category)
            .OrderBy(g => g.Key)
            .ToDictionary(
                g => g.Key.ToString(),
                g => new JsonReportCategory(
                    Total: g.Count(),
                    Passed: g.Count(r => r.Passed),
                    Tests: g.Select(r => new JsonReportTest(
                        Name: r.TestName,
                        ClassName: r.ClassName,
                        Description: r.Description,
                        Passed: r.Passed,
                        Duration: $"{r.Duration:hh\\:mm\\:ss\\.fff}",
                        Error: r.ErrorMessage
                    )).ToList()
                )
            );
    }
}
