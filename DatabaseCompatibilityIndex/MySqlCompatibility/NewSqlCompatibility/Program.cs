using NSCI.Configuration;
using NSCI.Database;
using NSCI.Reporting;
using NSCI.Testing;
namespace NSCI;

internal class Program
{
    static void Main(string[] args)
    {
        try
        {
            Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║   Distributed SQL Compatibility Tester                        ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════════╝\n");

            TestConfiguration config = TestConfiguration.Load("appsettings.json");
            Console.WriteLine($"Configuration loaded:");

            foreach (DatabaseConfiguration dbConfig in config.Databases)
            {
                Console.WriteLine($"  Database Type: {dbConfig.Type}");
                Console.WriteLine($"  Connection String: {dbConfig.ConnectionString}\n");
                Console.WriteLine($" Enabled: {dbConfig.Enabled}");

                ConsoleReporter consoleReporter = new();

                if (dbConfig.Enabled)
                {
                    List<(Type Type, SqlTestAttribute Attribute)> discoveredTests = TestDiscovery.DiscoverTests();
                    List<(Type Type, SqlTestAttribute Attribute)> filteredTests = discoveredTests
                        .Where(t => t.Attribute.DatabaseTypes.Contains(dbConfig.Type))
                        .ToList();

                    Console.WriteLine($"Discovered {filteredTests.Count} tests\n");

                    if (filteredTests.Count == 0)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("Warning: No tests discovered for this database type. Make sure test classes inherit from SqlTest and have [SqlTest] attribute with matching database types.");
                        Console.ResetColor();
                        return;
                    }

                    TestRunner testRunner = new(DatabaseProviderFactory.Create(dbConfig.Type), dbConfig.ConnectionString, consoleReporter);
                    List<TestResult> results = testRunner.RunAllTests(filteredTests);

                    Console.WriteLine("\n");

                    int passedCount = results.Count(r => r.Passed);
                    int failedCount = results.Count(r => !r.Passed);
                    consoleReporter.ReportSummary(results.Count, passedCount, failedCount);

                    // 6. Generate JSON report
                    string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    string reportPath = $"report_{timestamp}.json";
                    JsonReportGenerator.GenerateReport(reportPath, results, dbConfig, testRunner.TestDatabaseName);

                    Console.WriteLine($"\nTest database '{testRunner.TestDatabaseName}' remains available for manual inspection.");
                }
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Fatal error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            Console.ResetColor();
            Environment.Exit(1);
        }
    }
}
