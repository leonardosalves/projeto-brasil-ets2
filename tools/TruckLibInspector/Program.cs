using System.Reflection;

var assembly = typeof(TruckLib.ScsMap.Map).Assembly;

foreach (var type in assembly.GetTypes()
             .Where(t =>
                 t.FullName?.Contains("HashFs") == true ||
                 t.FullName?.Contains("FileSystem") == true ||
                 t.FullName?.Contains("Prefab") == true)
             .OrderBy(t => t.FullName))
{
    Console.WriteLine($"TYPE {type.FullName}");
    foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
    {
        Console.WriteLine($"  {method}");
    }

    foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
    {
        Console.WriteLine($"  PROPERTY {property.PropertyType.Name} {property.Name}");
    }
}
