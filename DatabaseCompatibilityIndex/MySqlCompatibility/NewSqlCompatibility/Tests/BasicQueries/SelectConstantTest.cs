using NSCI.Configuration;
using NSCI.Testing;
namespace NSCI.Tests.BasicQueries;

[SqlTest(SqlFeatureCategory.BasicQueries, "SELECT constant value 1", DatabaseType.MySql)]
public class SelectConstantTest : SqlTest
{
    public override string Command => "SELECT 1";
}
