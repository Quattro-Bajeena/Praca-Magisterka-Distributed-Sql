using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Indexes;

[SqlTest(SqlFeatureCategory.Indexes, "Test Descending Index ", DatabaseType.MySql)]
public class DescendingIndexTest : SqlTest
{
    protected override void SetupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "CREATE TABLE desc_idx_test (id INT PRIMARY KEY, value INT, INDEX idx_value_desc (value DESC))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO desc_idx_test VALUES (1, 100), (2, 200), (3, 50), (4, 150)";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "SELECT value FROM desc_idx_test ORDER BY value DESC LIMIT 1";
        object? maxValue = cmd.ExecuteScalar();
        AssertEqual(200, Convert.ToInt32(maxValue!), "Should find maximum value");

        cmd.CommandText = "EXPLAIN SELECT * FROM desc_idx_test WHERE value > 100 ORDER BY value DESC";
        using (DbDataReader reader = cmd.ExecuteReader())
        {
            bool foundIndex = false;
            while (reader.Read())
            {
                string columnValue = reader.GetString(0);
                if (columnValue.Contains("idx_value_desc"))
                {
                    foundIndex = true;
                    break;
                }
            }
            AssertTrue(foundIndex, "Descending index should be available for use");
        }
    }

    protected override void CleanupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS desc_idx_test";
        cmd.ExecuteNonQuery();
    }
}
