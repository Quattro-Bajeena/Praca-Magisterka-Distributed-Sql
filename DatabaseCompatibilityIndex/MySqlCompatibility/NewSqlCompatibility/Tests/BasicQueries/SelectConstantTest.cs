using NSCI.Testing;
namespace NSCI.Tests.BasicQueries;

[SqlTest(SqlFeatureCategory.BasicQueries, "SELECT constant value 1")]
public class SelectConstantTest : SqlTest
{
    protected override string CommandMy => "SELECT 1";
}
