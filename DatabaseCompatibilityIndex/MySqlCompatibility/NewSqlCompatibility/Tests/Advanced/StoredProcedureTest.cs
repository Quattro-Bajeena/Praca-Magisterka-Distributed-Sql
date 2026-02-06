using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Advanced;

[SqlTest(SqlFeatureCategory.StoredProcedures, "Test STORED PROCEDURE")]
public class StoredProcedureTest : SqlTest
{
    protected override void SetupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "CREATE TABLE proc_test (id INT PRIMARY KEY, value INT)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO proc_test VALUES (1, 10), (2, 20), (3, 30)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"CREATE PROCEDURE GetSum(OUT total INT)
            BEGIN
                SELECT SUM(value) INTO total FROM proc_test;
            END";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "CALL GetSum(@total)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT @total";
        object? result = cmd.ExecuteScalar();
        AssertEqual(60, Convert.ToInt32(result!), "Stored procedure should return sum of 60");
    }

    protected override void CleanupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP PROCEDURE IF EXISTS GetSum";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "DROP TABLE IF EXISTS proc_test";
        cmd.ExecuteNonQuery();
    }
}

