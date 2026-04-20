using Npgsql;
using NSCI.Configuration;
using System.Data.Common;

namespace NSCI.Database;

public class PostgreSqlDatabaseProvider : IDatabaseProvider
{
    readonly DatabaseConfiguration _configuration;

    public PostgreSqlDatabaseProvider(DatabaseConfiguration configuration)
    {
        _configuration = configuration;
    }

    public DbConnection CreateConnection(string connectionString)
    {
        NpgsqlDataSourceBuilder dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
        if (_configuration.Product == "ShardingSphere")
        {
            dataSourceBuilder.ConfigureTypeLoading(x => x.EnableTypeLoading(false));
        }

        // memeory leak bu whatever
        NpgsqlDataSource dataSource = dataSourceBuilder.Build();
        return dataSource.CreateConnection();
    }

    public string GenerateCreateDatabaseSql(string databaseName)
    {
        if (_configuration.Product == "CrateDB" && _configuration.Version != null && Version.Parse(_configuration.Version) < new Version(6, 2, 4))
        {
            // CrateDB uses schemas instead of databases
            // https://community.cratedb.com/t/create-new-schema/828/2
            Console.WriteLine("CrateDB versions before 6.2.4 do not support CREATE SCHEMA, skipping database creation");
            return string.Empty;
        }

        return $"CREATE SCHEMA \"{databaseName}\"";
    }

    public string GenerateSetDatabaseSql(string databaseName)
    {
        return $"SET search_path TO \"{databaseName}\",public";
    }
}
