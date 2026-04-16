using Npgsql;
using NSCI.Visualize.Models;

namespace NSCI.Visualize.Services;

public class TestDataService
{
    private readonly string _connectionString;

    public TestDataService(string connectionString)
    {
        _connectionString = connectionString;
    }

    public List<DatabaseInfo> GetAllDatabases()
    {
        List<DatabaseInfo> databases = new();

        using NpgsqlConnection connection = new(_connectionString);
        connection.Open();

        using NpgsqlCommand command = connection.CreateCommand();
        command.CommandText = @"
            SELECT 
                d.id, 
                d.name, 
                d.type, 
                d.result,
                COUNT(tr.id) as total_tests,
                SUM(CASE WHEN tr.passed THEN 1 ELSE 0 END) as passed_tests
            FROM databases d
            LEFT JOIN test_results tr ON d.id = tr.database_id
            GROUP BY d.id, d.name, d.type, d.result
            ORDER BY d.name;
        ";

        using NpgsqlDataReader reader = command.ExecuteReader();
        while (reader.Read())
        {
            int totalTests = reader.IsDBNull(4) ? 0 : Convert.ToInt32(reader.GetInt64(4));
            int passedTests = reader.IsDBNull(5) ? 0 : Convert.ToInt32(reader.GetInt64(5));

            databases.Add(new DatabaseInfo
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Type = reader.GetString(2),
                Result = reader.IsDBNull(3) ? null : reader.GetDecimal(3),
                TotalTests = totalTests,
                PassedTests = passedTests,
                FailedTests = totalTests - passedTests
            });
        }

        return databases;
    }

    public DatabaseInfo? GetDatabase(int databaseId)
    {
        using NpgsqlConnection connection = new(_connectionString);
        connection.Open();

        using NpgsqlCommand command = connection.CreateCommand();
        command.CommandText = @"
            SELECT 
                d.id, 
                d.name, 
                d.type, 
                d.result,
                COUNT(tr.id) as total_tests,
                SUM(CASE WHEN tr.passed THEN 1 ELSE 0 END) as passed_tests
            FROM databases d
            LEFT JOIN test_results tr ON d.id = tr.database_id
            WHERE d.id = @database_id
            GROUP BY d.id, d.name, d.type, d.result;
        ";

        command.Parameters.AddWithValue("@database_id", databaseId);

        using NpgsqlDataReader reader = command.ExecuteReader();
        if (reader.Read())
        {
            int totalTests = reader.IsDBNull(4) ? 0 : Convert.ToInt32(reader.GetInt64(4));
            int passedTests = reader.IsDBNull(5) ? 0 : Convert.ToInt32(reader.GetInt64(5));

            return new DatabaseInfo
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Type = reader.GetString(2),
                Result = reader.IsDBNull(3) ? null : reader.GetDecimal(3),
                TotalTests = totalTests,
                PassedTests = passedTests,
                FailedTests = totalTests - passedTests
            };
        }

        return null;
    }

    public List<TestResultInfo> GetTestResultsForDatabase(int databaseId)
    {
        List<TestResultInfo> results = new();

        using NpgsqlConnection connection = new(_connectionString);
        connection.Open();

        using NpgsqlCommand command = connection.CreateCommand();
        command.CommandText = @"
            SELECT id, database_id, name, class_name, category, description, passed, duration, error
            FROM test_results
            WHERE database_id = @database_id
            ORDER BY category, name;
        ";

        command.Parameters.AddWithValue("@database_id", databaseId);

        using NpgsqlDataReader reader = command.ExecuteReader();
        while (reader.Read())
        {
            results.Add(new TestResultInfo
            {
                Id = reader.GetInt32(0),
                DatabaseId = reader.GetInt32(1),
                Name = reader.GetString(2),
                ClassName = reader.GetString(3),
                Category = reader.GetString(4),
                Description = reader.GetString(5),
                Passed = reader.GetBoolean(6),
                Duration = reader.GetString(7),
                Error = reader.IsDBNull(8) ? null : reader.GetString(8)
            });
        }

        return results;
    }

    public Dictionary<string, CategoryStats> GetCategoryStatsForDatabase(int databaseId)
    {
        Dictionary<string, CategoryStats> stats = new();

        using NpgsqlConnection connection = new(_connectionString);
        connection.Open();

        using NpgsqlCommand command = connection.CreateCommand();
        command.CommandText = @"
            SELECT 
                category,
                COUNT(*) as total,
                SUM(CASE WHEN passed THEN 1 ELSE 0 END) as passed
            FROM test_results
            WHERE database_id = @database_id
            GROUP BY category
            ORDER BY category;
        ";

        command.Parameters.AddWithValue("@database_id", databaseId);

        using NpgsqlDataReader reader = command.ExecuteReader();
        while (reader.Read())
        {
            string category = reader.GetString(0);
            int total = Convert.ToInt32(reader.GetInt64(1));
            int passed = Convert.ToInt32(reader.GetInt64(2));

            stats[category] = new CategoryStats
            {
                Category = category,
                Total = total,
                Passed = passed,
                Failed = total - passed
            };
        }

        return stats;
    }

    public ComparisonData GetComparisonData()
    {
        ComparisonData data = new();
        HashSet<string> allCategories = new();

        using NpgsqlConnection connection = new(_connectionString);
        connection.Open();

        using NpgsqlCommand categoryCommand = connection.CreateCommand();
        categoryCommand.CommandText = @"
            SELECT DISTINCT category FROM test_results ORDER BY category;
        ";

        using (NpgsqlDataReader reader = categoryCommand.ExecuteReader())
        {
            while (reader.Read())
            {
                string category = reader.GetString(0);
                allCategories.Add(category);
                data.Categories.Add(category);
            }
        }

        using NpgsqlCommand databaseCommand = connection.CreateCommand();
        databaseCommand.CommandText = @"
            SELECT id, name FROM databases ORDER BY name;
        ";

        List<(int id, string name)> databases = new();
        using (NpgsqlDataReader reader = databaseCommand.ExecuteReader())
        {
            while (reader.Read())
            {
                databases.Add((reader.GetInt32(0), reader.GetString(1)));
            }
        }

        foreach (var (dbId, dbName) in databases)
        {
            List<decimal> passRates = new();

            foreach (string category in data.Categories)
            {
                using NpgsqlCommand statsCommand = connection.CreateCommand();
                statsCommand.CommandText = @"
                    SELECT 
                        COUNT(*) as total,
                        SUM(CASE WHEN passed THEN 1 ELSE 0 END) as passed
                    FROM test_results
                    WHERE database_id = @database_id AND category = @category;
                ";

                statsCommand.Parameters.AddWithValue("@database_id", dbId);
                statsCommand.Parameters.AddWithValue("@category", category);

                using NpgsqlDataReader reader = statsCommand.ExecuteReader();
                if (reader.Read())
                {
                    int total = Convert.ToInt32(reader.GetInt64(0));
                    int passed = Convert.ToInt32(reader.GetFieldValue<int?>(1));
                    decimal passRate = total > 0 ? (decimal)passed / total * 100 : 0;
                    passRates.Add(passRate);
                }
                else
                {
                    passRates.Add(0);
                }
            }

            data.DatabasePassRates[dbName] = passRates;
        }

        return data;
    }
}
