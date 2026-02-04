namespace NSCI.Configuration;

public class DatabaseConfiguration
{
    public string Name { get; set; } = "";
    public bool Enabled { get; set; } = true;
    public DatabaseType Type { get; set; } = DatabaseType.MySql;
    public string ConnectionString { get; set; } = "";
    public bool Cleanup { get; set; } = true;
    public string? DatabaseName { get; set; } = null;
}
