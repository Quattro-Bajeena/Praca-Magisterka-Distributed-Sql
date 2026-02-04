namespace NSCI.Reporting;

public record JsonReport(
    List<JsonDatabaseReport> Reports);

public record JsonDatabaseReport(
    string GeneratedAt,
    string DatabaseType,
    string ConfigurationName,
    string DatabaseName,
    string ConnectionString,
    JsonReportSummary Summary,
    Dictionary<string, JsonReportCategory> ResultsByCategory);

public record JsonReportSummary(
    int Total,
    int Passed,
    int Failed);

public record JsonReportCategory(
    int Total,
    int Passed,
    List<JsonReportTest> Tests);

public record JsonReportTest(
    string Name,
    string ClassName,
    string Description,
    bool Passed,
    string Duration,
    string? Error);
