using Npgsql;
using NSCI.Configuration;
using NSCI.Testing;

namespace NSCI.Reporting;

public class DatabaseReporter
{
    private readonly string _connectionString;

    public DatabaseReporter(string connectionString)
    {
        _connectionString = connectionString;
    }

    public void EnsureTablesExist()
    {
        using NpgsqlConnection connection = new(_connectionString);
        connection.Open();

        using NpgsqlCommand command = connection.CreateCommand();

        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS databases (
                id SERIAL PRIMARY KEY,
                name VARCHAR(255) UNIQUE NOT NULL,
                type VARCHAR(100) NOT NULL,
                result DECIMAL(5, 4) NULL
            );

            CREATE TABLE IF NOT EXISTS test_results (
                id SERIAL PRIMARY KEY,
                database_id INTEGER NOT NULL REFERENCES databases(id) ON DELETE CASCADE,
                name VARCHAR(500) NOT NULL,
                class_name VARCHAR(500) NOT NULL,
                category VARCHAR(100) NOT NULL,
                description TEXT,
                passed BOOLEAN NOT NULL,
                duration VARCHAR(50),
                error TEXT NULL,
                UNIQUE (database_id, name)
            );
        ";

        command.ExecuteNonQuery();
    }

    public void SaveResults(List<(DatabaseConfiguration, List<TestResult>)> results)
    {
        using NpgsqlConnection connection = new(_connectionString);
        connection.Open();

        foreach ((DatabaseConfiguration? dbConfig, List<TestResult>? testResults) in results)
        {
            int passedCount = testResults.Count(r => r.Passed);
            int totalCount = testResults.Count;
            decimal result = totalCount > 0 ? (decimal)passedCount / totalCount : 0;

            int databaseId = UpsertDatabase(connection, dbConfig.Name, dbConfig.Type.ToString(), result);

            UpsertTestResults(connection, databaseId, testResults);
        }
    }

    public void SaveResult((DatabaseConfiguration, List<TestResult>) testResult)
    {
        using NpgsqlConnection connection = new(_connectionString);
        connection.Open();

        (DatabaseConfiguration? dbConfig, List<TestResult>? testResults) = testResult;

        int passedCount = testResults.Count(r => r.Passed);
        int totalCount = testResults.Count;
        decimal result = totalCount > 0 ? (decimal)passedCount / totalCount : 0;

        int databaseId = UpsertDatabase(connection, dbConfig.Name, dbConfig.Type.ToString(), result);

        UpsertTestResults(connection, databaseId, testResults);
    }

    private int UpsertDatabase(NpgsqlConnection connection, string name, string type, decimal result)
    {
        using NpgsqlCommand command = connection.CreateCommand();

        command.CommandText = @"
            INSERT INTO databases (name, type, result)
            VALUES (@name, @type, @result)
            ON CONFLICT (name) 
            DO UPDATE SET 
                type = EXCLUDED.type,
                result = EXCLUDED.result
            RETURNING id;
        ";

        command.Parameters.AddWithValue("@name", name);
        command.Parameters.AddWithValue("@type", type);
        command.Parameters.AddWithValue("@result", result);

        object? resultObj = command.ExecuteScalar();
        return resultObj != null ? Convert.ToInt32(resultObj) : 0;
    }

    private void UpsertTestResults(NpgsqlConnection connection, int databaseId, List<TestResult> testResults)
    {
        using NpgsqlCommand command = connection.CreateCommand();

        command.CommandText = @"
            INSERT INTO test_results (database_id, name, class_name, category, description, passed, duration, error)
            VALUES (@database_id, @name, @class_name, @category, @description, @passed, @duration, @error)
            ON CONFLICT (database_id, name)
            DO UPDATE SET
                class_name = EXCLUDED.class_name,
                category = EXCLUDED.category,
                description = EXCLUDED.description,
                passed = EXCLUDED.passed,
                duration = EXCLUDED.duration,
                error = EXCLUDED.error;
        ";

        foreach (TestResult testResult in testResults)
        {
            command.Parameters.Clear();
            command.Parameters.AddWithValue("@database_id", databaseId);
            command.Parameters.AddWithValue("@name", testResult.TestName);
            command.Parameters.AddWithValue("@class_name", testResult.ClassName);
            command.Parameters.AddWithValue("@category", testResult.Category.ToString());
            command.Parameters.AddWithValue("@description", testResult.Description);
            command.Parameters.AddWithValue("@passed", testResult.Passed);
            command.Parameters.AddWithValue("@duration", $"{testResult.Duration:hh\\:mm\\:ss\\.fff}");
            command.Parameters.AddWithValue("@error", (object?)testResult.ErrorMessage ?? DBNull.Value);

            command.ExecuteNonQuery();
        }
    }
}
