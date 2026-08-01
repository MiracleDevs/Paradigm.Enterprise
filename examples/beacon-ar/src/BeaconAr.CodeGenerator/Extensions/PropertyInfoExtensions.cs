using System.Reflection;

namespace BeaconAr.CodeGenerator.Extensions;
internal static class PropertyInfoExtensions
{
    public static bool IsNullable(this PropertyInfo property)
    {
        if (property.PropertyType.IsValueType)
        {
            // Check if the property type is a nullable type
            return Nullable.GetUnderlyingType(property.PropertyType) is not null;
        }
        else
        {
            // Reference types (except string) are considered nullable
            return property.PropertyType != typeof(string);
        }
    }
}
