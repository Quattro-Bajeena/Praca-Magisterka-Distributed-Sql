using NSCI.Configuration;

namespace NSCI.Testing;

[AttributeUsage(AttributeTargets.Class)]
public class SqlTestAttribute : Attribute
{
    public SqlFeatureCategory Category { get; }
    public string Description { get; }
    public DatabaseType[] DatabaseTypes { get; }

    public SqlTestAttribute(SqlFeatureCategory category, string description, DatabaseType databaseType)
        : this(category, description, [databaseType])
    {
    }

    public SqlTestAttribute(SqlFeatureCategory category, string description, DatabaseType[] databaseTypes)
    {
        Category = category;
        Description = description;
        DatabaseTypes = databaseTypes.Length == 0 ? new[] { DatabaseType.MySql } : databaseTypes;
    }
}
