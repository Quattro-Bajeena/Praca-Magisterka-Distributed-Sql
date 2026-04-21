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
        if (_configuration.Product == "ShardingSphere"
            || _configuration.Product == "CrateDB" && _configuration.Version != null && Version.Parse(_configuration.Version) == new Version(3, 3, 5))
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

    public string GenerateTestDatabaseName()
    {
        if (_configuration.Product == "CrateDB")
        {
            int rand = new Random().Next(0, 100000);
            return "test_" + rand;
        }
        else
        {
            string suffix = DateTime.Now.ToString("s");
            return $"test_{suffix}";
        }

    }
}
