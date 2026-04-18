namespace NSCI.Data.Entities;

public class DatabaseEntity
{
    public int Id { get; set; }
    public string DatabaseId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public string? Product { get; set; }
    public string? Version { get; set; }
    public int? ReleaseYear { get; set; }
    public decimal? Result { get; set; }

    public List<TestResultEntity> TestResults { get; set; } = [];
}
