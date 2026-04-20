using Microsoft.EntityFrameworkCore;
using NSCI.Data;
using NSCI.Data.Entities;
using NSCI.Visualize.Models;

namespace NSCI.Visualize.Services;

public class TestDataService
{
    private readonly IDbContextFactory<NsciDbContext> _contextFactory;

    public TestDataService(IDbContextFactory<NsciDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public List<DatabaseInfo> GetAllDatabases()
    {
        using NsciDbContext context = _contextFactory.CreateDbContext();

        return context.Databases
            .Select(d => new DatabaseInfo
            {
                Id = d.Id,
                DatabaseId = d.DatabaseId,
                Name = d.Name,
                Type = d.Type,
                Product = d.Product,
                Version = d.Version,
                ReleaseYear = d.ReleaseYear,
                Result = d.Result,
                TotalTests = d.TestResults.Count,
                PassedTests = d.TestResults.Count(tr => tr.Passed),
                FailedTests = d.TestResults.Count(tr => !tr.Passed)
            })
            .OrderBy(d => d.Name)
            .ToList();
    }

    public DatabaseInfo? GetDatabase(int databaseId)
    {
        using NsciDbContext context = _contextFactory.CreateDbContext();

        return context.Databases
            .Where(d => d.Id == databaseId)
            .Select(d => new DatabaseInfo
            {
                Id = d.Id,
                DatabaseId = d.DatabaseId,
                Name = d.Name,
                Type = d.Type,
                Product = d.Product,
                Version = d.Version,
                ReleaseYear = d.ReleaseYear,
                Result = d.Result,
                TotalTests = d.TestResults.Count,
                PassedTests = d.TestResults.Count(tr => tr.Passed),
                FailedTests = d.TestResults.Count(tr => !tr.Passed)
            })
            .FirstOrDefault();
    }

    public List<TestResultInfo> GetTestResultsForDatabase(int databaseId)
    {
        using NsciDbContext context = _contextFactory.CreateDbContext();

        return context.TestResults
            .Where(tr => tr.DatabaseId == databaseId)
            .OrderBy(tr => tr.Category)
            .ThenBy(tr => tr.Name)
            .Select(tr => new TestResultInfo
            {
                Id = tr.Id,
                DatabaseId = tr.DatabaseId,
                Name = tr.Name,
                ClassName = tr.ClassName,
                Category = tr.Category,
                Description = tr.Description ?? "",
                Passed = tr.Passed,
                Duration = tr.Duration,
                Error = tr.Error,
                FailureCategory = tr.FailureCategory
            })
            .ToList();
    }

    public Dictionary<string, CategoryStats> GetCategoryStatsForDatabase(int databaseId)
    {
        using NsciDbContext context = _contextFactory.CreateDbContext();

        return context.TestResults
            .Where(tr => tr.DatabaseId == databaseId)
            .GroupBy(tr => tr.Category)
            .OrderBy(g => g.Key)
            .Select(g => new CategoryStats
            {
                Category = g.Key,
                Total = g.Count(),
                Passed = g.Count(tr => tr.Passed),
                Failed = g.Count(tr => !tr.Passed)
            })
            .ToDictionary(cs => cs.Category);
    }

    public ComparisonData GetComparisonData()
    {
        using NsciDbContext context = _contextFactory.CreateDbContext();

        ComparisonData data = new();

        data.Categories = context.TestResults
            .Select(tr => tr.Category)
            .Distinct()
            .OrderBy(c => c)
            .ToList();

        List<DatabaseEntity> databases = context.Databases
            .Include(d => d.TestResults)
            .OrderBy(d => d.Name)
            .ToList();

        foreach (DatabaseEntity db in databases)
        {
            List<decimal> passRates = data.Categories
                .Select(category =>
                {
                    List<TestResultEntity> categoryResults = db.TestResults
                        .Where(tr => tr.Category == category)
                        .ToList();
                    int total = categoryResults.Count;
                    int passed = categoryResults.Count(tr => tr.Passed);
                    return total > 0 ? (decimal)passed / total * 100 : 0m;
                })
                .ToList();

            data.DatabasePassRates[db.Name] = passRates;
        }

        return data;
    }

    /// <summary>
    /// Updates the failure category for a single test result.
    /// Returns false if the test result was not found.
    /// </summary>
    public bool UpdateFailureCategory(int testResultId, FailureCategory? category)
    {
        using NsciDbContext context = _contextFactory.CreateDbContext();

        TestResultEntity? entity = context.TestResults.Find(testResultId);
        if (entity == null)
            return false;

        entity.FailureCategory = category;
        context.SaveChanges();
        return true;
    }
}
