using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.TableDefinition;

[SqlTest(SqlFeatureCategory.DDL, "Test ALTER TABLE data type conversions")]
public class AlterTableDataTypeConversionTest : SqlTest
{
    protected override void SetupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "CREATE TABLE type_conversion (id INT PRIMARY KEY, num_value INT, text_value VARCHAR(50), decimal_value DECIMAL(10,2))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO type_conversion VALUES (1, 42, '123', 99.99)";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "ALTER TABLE type_conversion MODIFY COLUMN num_value BIGINT";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "ALTER TABLE type_conversion MODIFY COLUMN text_value TEXT";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT num_value FROM type_conversion WHERE id = 1";
        object? numValue = cmd.ExecuteScalar();
        AssertEqual(42L, Convert.ToInt64(numValue!), "INT to BIGINT conversion should preserve value");

        cmd.CommandText = "ALTER TABLE type_conversion MODIFY COLUMN decimal_value DOUBLE";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT decimal_value FROM type_conversion WHERE id = 1";
        object? decimalValue = cmd.ExecuteScalar();
        AssertTrue(Math.Abs(Convert.ToDouble(decimalValue!) - 99.99) < 0.01, "DECIMAL to DOUBLE conversion should preserve value");

        cmd.CommandText = "ALTER TABLE type_conversion ADD COLUMN json_data JSON";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "UPDATE type_conversion SET json_data = '{\"key\": \"value\"}' WHERE id = 1";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT json_data FROM type_conversion WHERE id = 1";
        object? jsonData = cmd.ExecuteScalar();
        AssertTrue(jsonData?.ToString()?.Contains("key") == true, "JSON column should work");
    }

    protected override void CleanupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS type_conversion";
        cmd.ExecuteNonQuery();
    }

    protected override void SetupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "CREATE TABLE type_conversion (id INT PRIMARY KEY, num_value INT, text_value VARCHAR(50), decimal_value DECIMAL(10,2))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO type_conversion VALUES (1, 42, '123', 99.99)";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "ALTER TABLE type_conversion ALTER COLUMN num_value TYPE BIGINT";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "ALTER TABLE type_conversion ALTER COLUMN text_value TYPE TEXT";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT num_value FROM type_conversion WHERE id = 1";
        object? numValue = cmd.ExecuteScalar();
        AssertEqual(42L, Convert.ToInt64(numValue!), "INT to BIGINT conversion should preserve value");

        cmd.CommandText = "ALTER TABLE type_conversion ALTER COLUMN decimal_value TYPE DOUBLE PRECISION";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT decimal_value FROM type_conversion WHERE id = 1";
        object? decimalValue = cmd.ExecuteScalar();
        AssertTrue(Math.Abs(Convert.ToDouble(decimalValue!) - 99.99) < 0.01, "DECIMAL to DOUBLE conversion should preserve value");

        cmd.CommandText = "ALTER TABLE type_conversion ADD COLUMN json_data JSONB";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "UPDATE type_conversion SET json_data = '{\"key\": \"value\"}'::jsonb WHERE id = 1";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT json_data FROM type_conversion WHERE id = 1";
        object? jsonData = cmd.ExecuteScalar();
        AssertTrue(jsonData?.ToString()?.Contains("key") == true, "JSON column should work");
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS type_conversion";
        cmd.ExecuteNonQuery();
    }
}
