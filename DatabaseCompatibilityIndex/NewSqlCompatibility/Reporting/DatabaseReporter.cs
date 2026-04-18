using Microsoft.EntityFrameworkCore;
using NSCI.Configuration;
using NSCI.Data;
using NSCI.Data.Entities;
using NSCI.Testing;

namespace NSCI.Reporting;

public class DatabaseReporter
{
    private readonly DbContextOptions<NsciDbContext> _contextOptions;

    public DatabaseReporter(string connectionString)
    {
        _contextOptions = new DbContextOptionsBuilder<NsciDbContext>()
            .UseNpgsql(connectionString)
            .Options;
    }

    public void EnsureTablesExist()
    {
        using NsciDbContext context = new(_contextOptions);
        context.Database.EnsureCreated();
    }

    public void SaveResults(List<(DatabaseConfiguration, List<TestResult>)> results)
    {
        foreach ((DatabaseConfiguration dbConfig, List<TestResult> testResults) in results)
        {
            SaveResult((dbConfig, testResults));
        }
    }

    public void SaveResult((DatabaseConfiguration, List<TestResult>) testResult)
    {
        (DatabaseConfiguration dbConfig, List<TestResult> testResults) = testResult;

        using NsciDbContext context = new(_contextOptions);

        int passedCount = testResults.Count(r => r.Passed);
        int totalCount = testResults.Count;
        decimal result = totalCount > 0 ? (decimal)passedCount / totalCount : 0;

        DatabaseEntity? dbEntity = context.Databases
            .Include(d => d.TestResults)
            .FirstOrDefault(d => d.DatabaseId == dbConfig.DatabaseId);

        if (dbEntity == null)
        {
            dbEntity = new DatabaseEntity
            {
                DatabaseId = dbConfig.DatabaseId,
                Name = dbConfig.Name,
                Type = dbConfig.Type.ToString(),
                Product = dbConfig.Product,
                Version = dbConfig.Version,
                ReleaseYear = dbConfig.ReleaseYear,
                Result = result
            };
            context.Databases.Add(dbEntity);
            context.SaveChanges();
        }
        else
        {
            dbEntity.Type = dbConfig.Type.ToString();
            dbEntity.Product = dbConfig.Product;
            dbEntity.Version = dbConfig.Version;
            dbEntity.ReleaseYear = dbConfig.ReleaseYear;
            dbEntity.Result = result;
        }

        foreach (TestResult tr in testResults)
        {
            TestResultEntity? existing = dbEntity.TestResults
                .FirstOrDefault(e => e.Name == tr.TestName);

            if (existing == null)
            {
                dbEntity.TestResults.Add(new TestResultEntity
                {
                    Name = tr.TestName,
                    ClassName = tr.ClassName,
                    Category = tr.Category.ToString(),
                    Description = tr.Description,
                    Passed = tr.Passed,
                    Duration = $"{tr.Duration:hh\\:mm\\:ss\\.fff}",
                    Error = tr.ErrorMessage
                });
            }
            else
            {
                existing.ClassName = tr.ClassName;
                existing.Category = tr.Category.ToString();
                existing.Description = tr.Description;
                existing.Passed = tr.Passed;
                existing.Duration = $"{tr.Duration:hh\\:mm\\:ss\\.fff}";
                existing.Error = tr.ErrorMessage;
            }
        }

        context.SaveChanges();
    }
}
