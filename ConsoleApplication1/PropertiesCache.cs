using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ConsoleApplication1
{
    public static class PropertiesCache
    {
        private static readonly ConcurrentDictionary<RuntimeTypeHandle, IEnumerable<PropertyInfo>> TypeProperties = new ConcurrentDictionary<RuntimeTypeHandle, IEnumerable<PropertyInfo>>();

        public static List<PropertyInfo> TypePropertiesCache(Type type)
        {
            var ignoredProperties = new[] {"RowVersion"};
            var forcedProperties = new[] {"RowVersion"};
            var properties = type.GetProperties().ToArray();
            TypeProperties[type.TypeHandle] = properties;
            return properties.Where(p => (p.CanWrite && !ignoredProperties.Contains(p.Name) && (p.PropertyType.IsValueType || p.PropertyType == typeof (string))) ||
                                         forcedProperties.Contains(p.Name)).ToList();
        }
    }
}