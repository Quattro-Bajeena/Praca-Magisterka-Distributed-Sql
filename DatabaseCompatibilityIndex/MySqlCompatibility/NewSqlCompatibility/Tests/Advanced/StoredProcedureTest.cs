using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Advanced;

[SqlTest(SqlFeatureCategory.StoredProcedures, "Test STORED PROCEDURE (likely unsupported in distributed databases)", DatabaseType.MySql)]
public class StoredProcedureTest : SqlTest
{
    public override void Execute(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        // Expected to fail - most distributed SQL databases don't support stored procedures
        cmd.CommandText = @"
            CREATE PROCEDURE sp_test()
            BEGIN
                SELECT 1;
            END";

        cmd.ExecuteNonQuery();
    }

    public override string? CleanupCommand => "DROP PROCEDURE IF EXISTS sp_test";
}
