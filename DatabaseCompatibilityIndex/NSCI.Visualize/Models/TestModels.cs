namespace NSCI.Visualize.Models;

public class DatabaseInfo
{
    public int Id { get; set; }
    public string DatabaseId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? Product { get; set; }
    public string? Version { get; set; }
    public int? ReleaseYear { get; set; }
    public decimal? Result { get; set; }
    public int TotalTests { get; set; }
    public int PassedTests { get; set; }
    public int FailedTests { get; set; }
}

public class TestResultInfo
{
    public int Id { get; set; }
    public int DatabaseId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool Passed { get; set; }
    public string Duration { get; set; } = string.Empty;
    public string? Error { get; set; }
}

public class CategoryStats
{
    public string Category { get; set; } = string.Empty;
    public int Total { get; set; }
    public int Passed { get; set; }
    public int Failed { get; set; }
    public decimal PassRate => Total > 0 ? (decimal)Passed / Total * 100 : 0;
}

public class ComparisonData
{
    public List<string> Categories { get; set; } = new();
    public Dictionary<string, List<decimal>> DatabasePassRates { get; set; } = new();
}
