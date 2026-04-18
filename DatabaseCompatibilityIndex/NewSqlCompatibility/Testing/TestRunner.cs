using NSCI.Configuration;
using NSCI.Database;
using NSCI.Reporting;
using System.Data.Common;
using System.Diagnostics;

namespace NSCI.Testing;

public class TestRunner
{
    private readonly DatabaseConfiguration _config;
    private string _testDatabaseName = "";
    private readonly ConsoleReporter _consoleReporter;
    private readonly IDatabaseProvider _databaseProvider;

    public TestRunner(DatabaseConfiguration config, ConsoleReporter consoleReporter)
    {
        _config = config;
        _consoleReporter = consoleReporter;
        _databaseProvider = DatabaseProviderFactory.Create(_config.Type);
    }

    public List<TestResult>? RunAllTests(List<(Type Type, SqlTestAttribute Attribute)> discoveredTests)
    {
        List<TestResult> results = new List<TestResult>();
        _testDatabaseName = CreateTestDatabase();
        if (_testDatabaseName == null)
        {
            return null;
        }
        _config.DatabaseName = _testDatabaseName;

        foreach ((Type testType, SqlTestAttribute attribute) in discoveredTests)
        {
            TestResult result = RunTest(testType, attribute);
            results.Add(result);
            _consoleReporter.ReportTestFull(result);
        }

        return results;
    }

    private DbConnection CreateConnectionToTestDatabase()
    {
        DbConnection connection = _databaseProvider.CreateConnection(_config.ConnectionString);
        connection.Open();
        using DbCommand useCmd = connection.CreateCommand();
        useCmd.CommandText = _databaseProvider.GenerateSetDatabaseSql(_testDatabaseName);
        useCmd.ExecuteNonQuery();
        return connection;
    }

    private string? CreateTestDatabase()
    {
        try
        {
            using DbConnection connection = _databaseProvider.CreateConnection(_config.ConnectionString);
            connection.Open();

            string dbName = GenerateTestDatabaseName();
            string createDbSql = _databaseProvider.GenerateCreateDatabaseSql(dbName);

            using DbCommand command = connection.CreateCommand();
            command.CommandText = createDbSql;
            command.ExecuteNonQuery();

            Console.WriteLine($"✓ Test database created: {dbName}");
            return dbName;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Could not create a test database: " + ex.Message);
            return null;
        }
    }

    private TestResult RunTest(Type testType, SqlTestAttribute attribute)
    {
        Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
        string testName = testType.Name;
        string? errorMessage = null;
        bool passed = false;

        try
        {

            SqlTest testInstance = Activator.CreateInstance(testType) as SqlTest
                ?? throw new InvalidOperationException($"Failed to instantiate test {testName}");

            using DbConnection connection = CreateConnectionToTestDatabase();
            using DbConnection connectionSecond = CreateConnectionToTestDatabase();
            try
            {

                testInstance.Initialize(_config);
                testInstance.Setup(connection);
                testInstance.Execute(connection, connectionSecond);
                passed = true;
            }
            finally
            {
                if (_config.Cleanup)
                {
                    try
                    {
                        testInstance.Cleanup(connection);
                    }
                    catch (Exception cleanupEx)
                    {
                        Console.WriteLine($"! Cleanup failed for test {testName}: {cleanupEx.Message}");
                    }
                }
            }
        }
        catch (AssertionException ex)
        {
            errorMessage = $"Assertion: {ex.Message}";
        }
        catch (Exception ex)
        {
            errorMessage = $"{ex.GetType().Name}: {ex.Message}";
        }
        finally
        {
            stopwatch.Stop();
        }

        return new TestResult(
            TestName: testName,
            ClassName: testType.FullName ?? testName,
            Category: attribute.Category,
            Description: attribute.Description,
            Passed: passed,
            ErrorMessage: errorMessage,
            Duration: stopwatch.Elapsed
        );
    }

    private static string GenerateTestDatabaseName()
    {
        string suffix = DateTime.Now.ToString("s");
        return $"test_{suffix}";
    }
}
