using NSCI.Configuration;
using NSCI.Reporting;
using NSCI.Testing;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Diagnostics;
using System.Globalization;

namespace NSCI;

internal static class Program
{
    private const string DefaultConfigFileName = "appsettings.json";
    private const string DefaultDbRootFolderPath = "C:\\Coding\\Studia\\Praca-Magisterka-Distributed-Sql";

    internal static int Main(string[] args)
    {
        Option<FileInfo?> configOption = new Option<FileInfo?>("--config", new[] { "-c" })
        {
            Description = "Path to the JSON configuration file."
        };

        Option<DirectoryInfo?> dbRootOption = new Option<DirectoryInfo?>("--db-root", Array.Empty<string>())
        {
            Description = "Root folder that contains database subfolders with db.config.json.",
        };

        RootCommand rootCommand = new RootCommand("Distributed SQL Compatibility Tester")
        {
            configOption,
            dbRootOption
        };

        ParseResult parseResult = rootCommand.Parse(args);

        if (parseResult.Errors.Count > 0)
        {
            foreach (ParseError error in parseResult.Errors)
            {
                Console.Error.WriteLine(error.Message);
            }

            return 1;
        }

        FileInfo configFile = parseResult.GetValue(configOption) ?? new FileInfo(Path.Combine(AppContext.BaseDirectory, DefaultConfigFileName));
        DirectoryInfo dbRoot = parseResult.GetValue(dbRootOption) ?? new DirectoryInfo(DefaultDbRootFolderPath);

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

    private static void Run(FileInfo configFile, DirectoryInfo dbRoot)
    {
        Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");

        Console.WriteLine($"Configuration file: {configFile.FullName}");

        Console.WriteLine($"Database root: {dbRoot.FullName}");

        TestConfiguration config = TestConfiguration.Load(configFile.FullName, dbRoot?.FullName);

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


        Console.WriteLine($"Discovered enabled databases:");
        foreach (DatabaseConfiguration dbConfig in config.Databases)
        {
            if (dbConfig.Enabled)
            {
                Console.WriteLine($"=== Database: {dbConfig.Name} ===");
                Console.WriteLine($" Database Type: {dbConfig.Type}");
                Console.WriteLine($" Connection String: {dbConfig.ConnectionString}");
                Console.WriteLine($" Enabled: {dbConfig.Enabled}");
            }
        }

        Console.WriteLine($"Running tests:");
        foreach (DatabaseConfiguration dbConfig in config.Databases)
        {
            if (!dbConfig.Enabled)
            {
                continue;
            }

            Console.WriteLine($"=== Running test for: {dbConfig.Name} ===");

            ConsoleReporter consoleReporter = new(config.General);

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

            Stopwatch stopwatch = Stopwatch.StartNew();
            TestRunner testRunner = new(dbConfig, consoleReporter);
            List<TestResult>? results = testRunner.RunAllTests(filteredTests);
            stopwatch.Stop();

            Console.WriteLine();

            if (results != null)
            {
                int passedCount = results.Count(r => r.Passed);
                int failedCount = results.Count(r => !r.Passed);
                consoleReporter.ReportSummary(results.Count, passedCount, failedCount, stopwatch.Elapsed);

                (DatabaseConfiguration dbConfig, List<TestResult> results) resultToSave = (dbConfig, results);
                databaseResults.Add(resultToSave);

                if (databaseReporter != null)
                {
                    databaseReporter.SaveResult(resultToSave);
                    Console.WriteLine("✓ Results saved to database");
                }
            }

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
