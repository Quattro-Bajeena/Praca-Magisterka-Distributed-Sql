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

    public DatabaseEntity Database { get; set; } = null!;
}
