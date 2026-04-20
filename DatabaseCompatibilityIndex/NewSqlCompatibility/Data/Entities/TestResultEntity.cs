namespace NSCI.Data.Entities;

public class TestResultEntity
{
    public int Id { get; set; }
    public int DatabaseId { get; set; }
    public string Name { get; set; } = "";
    public string ClassName { get; set; } = "";
    public string Category { get; set; } = "";
    public string? Description { get; set; }
    public bool Passed { get; set; }
    public string Duration { get; set; } = "";
    public string? Error { get; set; }

    /// <summary>
    /// Manual classification of the failure type. Null if not yet classified or test passed.
    /// Stored as an integer in the database.
    /// </summary>
    public FailureCategory? FailureCategory { get; set; }

    public DatabaseEntity Database { get; set; } = null!;
}
