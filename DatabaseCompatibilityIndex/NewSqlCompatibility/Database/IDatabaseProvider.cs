using System.Data.Common;

namespace NSCI.Database;

public interface IDatabaseProvider
{
    DbConnection CreateConnection(string connectionString);
    string GenerateCreateDatabaseSql(string databaseName);
    string GenerateSetDatabaseSql(string databaseName);
    string GenerateTestDatabaseName();
}
