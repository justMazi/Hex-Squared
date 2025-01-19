using System.Reflection;

namespace Domain;

public class InheritorHelpers
{
    public static List<Type> GetInheritors<T>()
    {
        return Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(T).IsAssignableFrom(t))
            .ToList();
    }
}
