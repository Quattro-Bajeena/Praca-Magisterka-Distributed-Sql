using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.BasicQueries;

// TODO
//[SqlTest(SqlFeatureCategory.BasicQueries, "Test DISTINCT ON", DatabaseType.PostgreSql)]
public class PostgresDistinctOnTest : SqlTest
{
    protected override void SetupPg(DbConnection connection)
    {

    }
}
