namespace NSCI.Configuration;

public class GeneralConfiguration
{
    public bool DisplayFailedTests { get; set; } = false;
    public bool DisplayPassedTests { get; set; } = false;
    public string StatDbConnectionString { get; set; } = string.Empty;
}
