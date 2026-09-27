using System.Reflection;

namespace COMPASS.Infra.Objects;

public static class ReflectionExtensions
{
    private static PropertyInfo? GetPropertyInfo(object obj, string propName)
    {
        if (obj is null) throw new ArgumentNullException(nameof(obj));
        if (String.IsNullOrWhiteSpace(propName)) throw new ArgumentNullException(nameof(propName), propName);

        Type? type = obj.GetType();

        //keep going up the inheritance tree to find it
        while (type is not null)
        {
            try
            {
                PropertyInfo? info = type.GetProperty(propName);
                if (info is not null) return info;
            }
            catch (AmbiguousMatchException) { }

            type = type.BaseType;
        }
        return null;
    }

    extension(object obj)
    {
        public object? GetPropertyValue(string propName)
        {
            var propInfo = GetPropertyInfo(obj, propName) ?? throw new MissingFieldException(propName);
            return propInfo.GetValue(obj);
        }

        public object? GetDeepPropertyValue(string fullPropName)
        {
            if (String.IsNullOrEmpty(fullPropName))
            {
                return obj;
            }

            string[] propNames = fullPropName.Split('.');
            object? result = obj;
            foreach (string propName in propNames)
            {
                result = result?.GetPropertyValue(propName);
            }
            return result;
        }

        public void SetProperty(string propName, object? value)
        {
            var propInfo = GetPropertyInfo(obj, propName);

            if (propInfo is not null && propInfo.CanWrite)
            {
                propInfo.SetValue(obj, value);
            }
        }
    }

    extension(Type type)
    {
        public List<string> GetObsoleteProperties()
        {
            List<string> obsoleteProperties = [];

            // Check each property for the presence of the Obsolete attribute
            PropertyInfo[] properties = type.GetProperties();
            foreach (PropertyInfo property in properties)
            {
                if (Attribute.GetCustomAttribute(property, typeof(ObsoleteAttribute)) is ObsoleteAttribute)
                {
                    obsoleteProperties.Add(property.Name);
                }
            }

            return obsoleteProperties;
        }
    }
}
