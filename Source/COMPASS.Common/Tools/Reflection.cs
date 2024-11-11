using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace COMPASS.Common.Tools
{
    public static class Reflection
    {
        public static string Version { get; } = GetVersion();

        public static string GetVersion()
        {
            string? assemblyName = Process.GetCurrentProcess().MainModule?.FileName;
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assemblyName!);
            return fvi!.FileVersion![..5];
        }

        public static List<string> GetObsoleteProperties(Type type)
        {
            List<string> obsoleteProperties = new();

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
