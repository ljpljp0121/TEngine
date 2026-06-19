using System;
using System.Collections.Generic;
using System.Reflection;

namespace PFDebugger
{
    internal struct ManagerRegistration
    {
        public Type Type;
        public int Priority;
    }

    /// <summary>
    /// 反射扫描所有带 [SubManager] 的 SubManagerBase 子类。
    /// </summary>
    internal static class ManagerDiscovery
    {
        public static List<ManagerRegistration> DiscoverManagers()
        {
            var results = new List<ManagerRegistration>();

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (var assembly in assemblies)
            {
                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch
                {
                    continue;
                }

                foreach (var type in types)
                {
                    if (type.IsAbstract || type.IsInterface)
                        continue;
                    if (!typeof(SubManagerBase).IsAssignableFrom(type))
                        continue;

                    var attr = type.GetCustomAttribute<SubManagerAttribute>();
                    if (attr == null)
                        continue;

                    results.Add(new ManagerRegistration
                    {
                        Type = type,
                        Priority = attr.Priority,
                    });
                }
            }

            results.Sort((a, b) => a.Priority.CompareTo(b.Priority));
            return results;
        }
    }
}