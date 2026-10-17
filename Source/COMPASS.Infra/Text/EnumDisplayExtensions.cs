using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace COMPASS.Infra.Text;

public static class EnumDisplayExtensions
{
    private static readonly ConcurrentDictionary<Enum, string> _displayNameCache = new();

    /// <summary>
    /// Returns the name set via <see cref="DisplayAttribute"/> on the enum member,
    /// falling back to <see cref="Enum.ToString()"/> when the value has no attribute
    /// (e.g. a missing attribute or a combination of flags).
    /// </summary>
    public static string GetDisplayName(this Enum value) =>
        _displayNameCache.GetOrAdd(value, enumValue =>
        {
            FieldInfo? enumMember = enumValue.GetType().GetField(enumValue.ToString());
            return enumMember?.GetCustomAttribute<DisplayAttribute>()?.GetName() ?? enumValue.ToString();
        });
}
