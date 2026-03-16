using NSCI.Configuration;
using NSCI.Testing;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NSCI.Reporting;

public static class JsonReportGenerator
{
    public static void GenerateReport(
        string outputPath,
        List<(DatabaseConfiguration, List<TestResult>)> results)
    {
        JsonReport report = BuildReport(results);

        JsonSerializerOptions options = new()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        string json = JsonSerializer.Serialize(report, options);
        File.WriteAllText(outputPath, json);

        Console.WriteLine($"\n✓ Report generated: {outputPath}");

        UpdateMasterReport(outputPath, report, options);
    }

    private static JsonReport BuildReport(List<(DatabaseConfiguration, List<TestResult>)> results)
    {
        return new JsonReport(
            GeneratedAt: DateTime.Now,
            Reports: results.Select(r => BuildDatabaseReport(r.Item1, r.Item2)).ToDictionary(r => r.ConfigurationName)
        );
    }

    private static JsonDatabaseReport BuildDatabaseReport(DatabaseConfiguration configuration, List<TestResult> results)
    {
        JsonReportSummary summary = new(
            Total: results.Count,
            Passed: results.Count(r => r.Passed),
            Failed: results.Count(r => !r.Passed)
        );

        Dictionary<string, JsonReportCategory> resultsByCategory = BuildCategoryGroups(results);

        return new JsonDatabaseReport(
            GeneratedAt: DateTime.UtcNow.ToString("O"),
            DatabaseType: configuration.Type.ToString(),
            ConfigurationName: configuration.Name,
            DatabaseName: configuration.DatabaseName ?? "unknown",
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
                        Category: r.Category.ToString(),
                        Description: r.Description,
                        Passed: r.Passed,
                        Duration: $"{r.Duration:hh\\:mm\\:ss\\.fff}",
                        Error: r.ErrorMessage
                    )).ToList()
                )
            );
    }

    private static void UpdateMasterReport(string outputPath, JsonReport currentReport, JsonSerializerOptions options)
    {
        string directory = Path.GetDirectoryName(outputPath) ?? Directory.GetCurrentDirectory();
        string masterReportPath = Path.Combine(directory, "master_report.json");

        Dictionary<string, JsonDatabaseReport> masterReports;

        if (File.Exists(masterReportPath))
        {
            string existingJson = File.ReadAllText(masterReportPath);
            JsonReport? existingMasterReport = JsonSerializer.Deserialize<JsonReport>(existingJson, options);
            masterReports = existingMasterReport?.Reports ?? new Dictionary<string, JsonDatabaseReport>();
        }
        else
        {
            masterReports = new Dictionary<string, JsonDatabaseReport>();
        }

        foreach (KeyValuePair<string, JsonDatabaseReport> report in currentReport.Reports)
        {
            masterReports[report.Key] = report.Value;
        }

        JsonReport updatedMasterReport = new(
            GeneratedAt: DateTime.Now,
            Reports: masterReports
        );

        string masterJson = JsonSerializer.Serialize(updatedMasterReport, options);
        File.WriteAllText(masterReportPath, masterJson);

        Console.WriteLine($"✓ Master report updated: {masterReportPath}");
    }
}
