using NSCI.Database;
using NSCI.Reporting;
using System.Data.Common;
using System.Diagnostics;

namespace NSCI.Testing;

public class TestRunner
{
    private readonly IDatabaseProvider _databaseProvider;
    private readonly string _connectionString;
    private string _testDatabaseName = "";
    private readonly ConsoleReporter _consoleReporter;

    public string TestDatabaseName => _testDatabaseName;

    public TestRunner(IDatabaseProvider databaseProvider, string connectionString, ConsoleReporter consoleReporter)
    {
        _databaseProvider = databaseProvider;
        _connectionString = connectionString;
        _consoleReporter = consoleReporter;
    }

    /// <summary>
    /// Runs all discovered tests and returns their results.
    /// </summary>
    public List<TestResult> RunAllTests(List<(Type Type, SqlTestAttribute Attribute)> discoveredTests)
    {
        List<TestResult> results = new List<TestResult>();
        // Create test database
        _testDatabaseName = CreateTestDatabase();

        // Open connection to test database
        using DbConnection connection = _databaseProvider.CreateConnection(_connectionString);
        connection.Open();

        // Execute USE/SELECT database command if needed
        if (!string.IsNullOrEmpty(_testDatabaseName) && _databaseProvider is MySqlDatabaseProvider)
        {
            using DbCommand useCmd = connection.CreateCommand();
            useCmd.CommandText = _databaseProvider.GenerateUseDatabaseSql(_testDatabaseName);
            useCmd.ExecuteNonQuery();
        }

        // Run each test
        foreach ((Type? testType, SqlTestAttribute? attribute) in discoveredTests)
        {
            _consoleReporter.ReportTestStart(testType.Name, attribute.Description, attribute.Category.ToString());
            TestResult result = RunTest(testType, attribute, connection);
            results.Add(result);
            _consoleReporter.ReportTestEnd(result);
        }

        return results;
    }

    /// <summary>
    /// Creates a test database with a random name.
    /// </summary>
    private string CreateTestDatabase()
    {
        using DbConnection connection = _databaseProvider.CreateConnection(_connectionString);
        connection.Open();

        string dbName = GenerateTestDatabaseName();
        string createDbSql = _databaseProvider.GenerateCreateDatabaseSql(dbName);

        using DbCommand command = connection.CreateCommand();
        command.CommandText = createDbSql;
        command.ExecuteNonQuery();

        Console.WriteLine($"✓ Test database created: {dbName}");
        return dbName;
    }

    /// <summary>
    /// Runs a single test and returns its result.
    /// </summary>
    private static TestResult RunTest(Type testType, SqlTestAttribute attribute, DbConnection connection)
    {
        Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
        string testName = testType.Name;
        string? errorMessage = null;
        bool passed = false;

        try
        {
            SqlTest testInstance = Activator.CreateInstance(testType) as SqlTest
                ?? throw new InvalidOperationException($"Failed to instantiate test {testName}");

            try
            {
                testInstance.Setup(connection);
                testInstance.Execute(connection);
                passed = true;
            }
            finally
            {
                testInstance.Cleanup(connection);
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

    /// <summary>
    /// Generates a random test database name.
    /// </summary>
    private static string GenerateTestDatabaseName()
    {
        Random random = new Random();
        int suffix = random.Next(10000, 99999);
        return $"test_compat_{suffix}";
    }
}
