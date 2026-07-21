using System.Reflection;

namespace FIGBacktestOptimizer;

public static class ParameterReflection
{
    private static readonly Dictionary<string, PropertyInfo> Properties =
        typeof(AdxBaseParameters)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.CanRead && property.CanWrite)
            .ToDictionary(property => property.Name, StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyDictionary<string, double> GetParameterValues(AdxBaseParameters parameters)
    {
        var values = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        foreach (var property in Properties.Values)
        {
            var value = property.GetValue(parameters);
            values[property.Name] = Convert.ToDouble(value);
        }

        return values;
    }

    public static void SetParameter(AdxBaseParameters parameters, string name, double value)
    {
        if (!Properties.TryGetValue(name, out var property))
        {
            throw new InvalidOperationException($"Unknown ADXBase parameter '{name}'.");
        }

        if (property.PropertyType == typeof(int))
        {
            property.SetValue(parameters, Convert.ToInt32(Math.Round(value)));
        }
        else if (property.PropertyType == typeof(double))
        {
            property.SetValue(parameters, value);
        }
        else
        {
            throw new InvalidOperationException($"Unsupported parameter type for '{name}'.");
        }
    }

    public static string ToCsvHeader()
    {
        return string.Join(",", Properties.Keys.OrderBy(name => name, StringComparer.OrdinalIgnoreCase));
    }

    public static string ToCsvRow(AdxBaseParameters parameters)
    {
        var values = Properties.Keys
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .Select(name => Convert.ToString(Properties[name].GetValue(parameters), System.Globalization.CultureInfo.InvariantCulture) ?? "");
        return string.Join(",", values);
    }
}
