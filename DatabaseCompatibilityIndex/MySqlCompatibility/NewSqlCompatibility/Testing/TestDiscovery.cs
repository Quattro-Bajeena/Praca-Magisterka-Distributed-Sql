using System.Reflection;

namespace NSCI.Testing;

public static class TestDiscovery
{
    public static List<(Type Type, SqlTestAttribute Attribute)> DiscoverTests()
    {
        List<(Type, SqlTestAttribute)> result = new List<(Type, SqlTestAttribute)>();
        Assembly assembly = Assembly.GetExecutingAssembly();

        Type sqlTestBaseType = typeof(SqlTest);
        IEnumerable<Type> testClasses = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && sqlTestBaseType.IsAssignableFrom(t));

        foreach (Type? testClass in testClasses)
        {
            SqlTestAttribute? attribute = testClass.GetCustomAttribute<SqlTestAttribute>();
            if (attribute != null)
            {
                result.Add((testClass, attribute));
            }
        }

        return result.OrderBy(x => x.Item2.Category).ThenBy(x => x.Item1.Name).ToList();
    }
}
