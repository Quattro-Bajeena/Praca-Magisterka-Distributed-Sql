using NSCI.Configuration;
using NSCI.Reporting;
using NSCI.Testing;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Globalization;

namespace NSCI;

internal static class Program
{
    private const string DefaultConfigFileName = "appsettings.json";

    internal static int Main(string[] args)
    {
        var configOption = new Option<FileInfo?>("--config", new[] { "-c" })
        {
            Description = "Path to the JSON configuration file."
        };

        var dbRootOption = new Option<DirectoryInfo>("--db-root", Array.Empty<string>())
        {
            Description = "Root folder that contains database subfolders with db.config.json.",
            Required = true
        };

        var rootCommand = new RootCommand("Distributed SQL Compatibility Tester")
        {
            configOption,
            dbRootOption
        };

        var parseResult = rootCommand.Parse(args);

        if (parseResult.Errors.Count > 0)
        {
            foreach (var error in parseResult.Errors)
            {
                Console.Error.WriteLine(error.Message);
            }

            return 1;
        }

        FileInfo? configFile = parseResult.GetValue(configOption);
        DirectoryInfo dbRoot = parseResult.GetRequiredValue(dbRootOption);

        try
        {
            Run(configFile, dbRoot);
            return 0;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Fatal error: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            Console.ResetColor();
            return 1;
        }
    }

    private static void Run(FileInfo? configFile, DirectoryInfo dbRoot)
    {
        Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║   Distributed SQL Compatibility Tester                        ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════════╝\n");

        Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");

        configFile ??= new FileInfo(Path.Combine(AppContext.BaseDirectory, DefaultConfigFileName));

        Console.WriteLine($"Configuration file: {configFile.FullName}");
        Console.WriteLine($"Database root: {dbRoot.FullName}");

        TestConfiguration config = TestConfiguration.Load(configFile.FullName, dbRoot.FullName);

        Console.WriteLine("Configuration loaded:");

        DatabaseReporter? databaseReporter = null;
        if (!string.IsNullOrEmpty(config.General.StatDbConnectionString))
        {
            databaseReporter = new DatabaseReporter(config.General.StatDbConnectionString);
            databaseReporter.EnsureTablesExist();
            Console.WriteLine("Database reporter initialized\n");
        }

        List<(DatabaseConfiguration, List<TestResult>)> databaseResults = new();
        List<(Type Type, SqlTestAttribute Attribute)> discoveredTests = TestDiscovery.DiscoverTests();

        foreach (DatabaseConfiguration dbConfig in config.Databases)
        {
            Console.WriteLine($"=== Database: {dbConfig.Name} ===");
            Console.WriteLine($" Database Type: {dbConfig.Type}");
            Console.WriteLine($" Connection String: {dbConfig.ConnectionString}");
            Console.WriteLine($" Enabled: {dbConfig.Enabled}");

            ConsoleReporter consoleReporter = new(config.General);

            if (!dbConfig.Enabled)
            {
                Console.WriteLine("Skipping disabled database.\n");
                continue;
            }

            List<(Type Type, SqlTestAttribute Attribute)> filteredTests = discoveredTests
                .Where(t => t.Attribute.DatabaseTypes.Contains(dbConfig.Type))
                .ToList();

            Console.WriteLine($"Discovered {filteredTests.Count} tests\n");

            if (filteredTests.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Warning: No tests discovered for this database type. Make sure test classes inherit from SqlTest and have [SqlTest] attribute with matching database types.");
                Console.ResetColor();
                Console.WriteLine();
                continue;
            }

            TestRunner testRunner = new(dbConfig, consoleReporter);
            List<TestResult> results = testRunner.RunAllTests(filteredTests);

            Console.WriteLine();

            int passedCount = results.Count(r => r.Passed);
            int failedCount = results.Count(r => !r.Passed);
            consoleReporter.ReportSummary(results.Count, passedCount, failedCount);

            databaseResults.Add((dbConfig, results));
            Console.WriteLine();
        }

        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string reportPath = $"report_{timestamp}.json";
        JsonReportGenerator.GenerateReport(reportPath, databaseResults);

        if (databaseReporter != null && databaseResults.Count > 0)
        {
            databaseReporter.SaveResults(databaseResults);
            Console.WriteLine("✓ Results saved to database");
        }
    }
}
