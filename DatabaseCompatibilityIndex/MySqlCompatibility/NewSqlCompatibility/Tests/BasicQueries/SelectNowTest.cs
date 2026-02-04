using NSCI.Testing;
namespace NSCI.Tests.BasicQueries;

[SqlTest(SqlFeatureCategory.BasicQueries, "SELECT current timestamp")]
public class SelectNowTest : SqlTest
{
    protected override string CommandMy => "SELECT NOW()";
}
