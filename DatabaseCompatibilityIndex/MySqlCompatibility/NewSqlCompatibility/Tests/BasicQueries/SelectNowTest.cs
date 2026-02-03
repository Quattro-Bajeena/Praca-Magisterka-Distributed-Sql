using NSCI.Configuration;
using NSCI.Testing;
namespace NSCI.Tests.BasicQueries;

[SqlTest(SqlFeatureCategory.BasicQueries, "SELECT current timestamp", DatabaseType.MySql)]
public class SelectNowTest : SqlTest
{
    public override string Command => "SELECT NOW()";
}
