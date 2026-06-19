using System;
using System.Collections.Generic;
using System.Reflection;

namespace PFDebugger
{
    internal enum TabEntryType
    {
        Panel,
        Method,
    }

    internal struct TabEntry
    {
        public string Name;
        public int Order;
        public TabEntryType Type;

        // Panel 类型
        public Type PanelType;

        // Method 类型
        public MethodInfo Method;
    }

    internal static class TabDiscovery
    {
        public static List<TabEntry> DiscoverTabs()
        {
            var results = new List<TabEntry>();
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
                    // 扫描带 [DebuggerTab] 的类
                    var classAttr = type.GetCustomAttribute<DebuggerTabAttribute>();
                    if (classAttr != null && typeof(IDebuggerPanel).IsAssignableFrom(type) && !type.IsInterface)
                    {
                        results.Add(new TabEntry
                        {
                            Name = classAttr.Name,
                            Order = classAttr.Order,
                            Type = TabEntryType.Panel,
                            PanelType = type,
                        });
                    }

                    // 扫描带 [DebuggerTab] 的静态方法
                    foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
                    {
                        var methodAttr = method.GetCustomAttribute<DebuggerTabAttribute>();
                        if (methodAttr != null)
                        {
                            results.Add(new TabEntry
                            {
                                Name = methodAttr.Name,
                                Order = methodAttr.Order,
                                Type = TabEntryType.Method,
                                Method = method,
                            });
                        }
                    }
                }
            }

            results.Sort((a, b) => a.Order.CompareTo(b.Order));
            return results;
        }
    }
}
