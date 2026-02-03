namespace NSCI.Configuration;

public class DatabaseConfiguration
{
    public string Name { get; set; } = "";
    public bool Enabled { get; set; } = true;
    public DatabaseType Type { get; set; } = DatabaseType.MySql;
    public string ConnectionString { get; set; } = "";
}
