using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Aggregations;

[SqlTest(SqlFeatureCategory.Aggregations, "Test DISTINCT", DatabaseType.MySql)]
public class DistinctTest : SqlTest
{
    public override void Execute(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "CREATE TABLE products_distinct (id INT PRIMARY KEY, category VARCHAR(20))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO products_distinct VALUES (1, 'Electronics'), (2, 'Electronics'), (3, 'Books'), (4, 'Books'), (5, 'Clothing')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(DISTINCT category) FROM products_distinct";
        object? distinctCount = cmd.ExecuteScalar();
        AssertEqual(3L, (long)distinctCount!, "DISTINCT should count 3 unique categories");

        cmd.CommandText = "DROP TABLE products_distinct";
        cmd.ExecuteNonQuery();
    }
}
