using System;
using System.Collections.Generic;
using System.Reflection;

namespace PFDebugger
{
    public class InfoBase
    {
        private static readonly Dictionary<Type, CachedInfoEntry> cache = new Dictionary<Type, CachedInfoEntry>();

        private CachedInfoEntry GetCacheEntry()
        {
            var type = GetType();
            if (cache.TryGetValue(type, out var entry)) return entry;

            var props = new List<PropEntry>();
            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var attr = prop.GetCustomAttribute<InfoItemAttribute>();
                if (attr == null || prop.GetMethod == null) continue;
                props.Add(new PropEntry(prop, attr.DisplayName));
            }

            entry = new CachedInfoEntry(props);
            cache[type] = entry;
            return entry;
        }

        private class CachedInfoEntry
        {
            public readonly List<PropEntry> Props;
            public CachedInfoEntry(List<PropEntry> props) => Props = props;
        }

        private struct PropEntry
        {
            public readonly PropertyInfo Prop;
            public readonly string DisplayName;

            public PropEntry(PropertyInfo prop, string displayName)
            {
                Prop = prop;
                DisplayName = displayName;
            }
        }
        
        public List<InfoItemData> GetInfoItems()
        {
            var entry = GetCacheEntry();
            var items = new List<InfoItemData>(entry.Props.Count);
            for (int i = 0; i < entry.Props.Count; i++)
            {
                var p = entry.Props[i];
                items.Add(new InfoItemData(p.DisplayName, p.Prop.GetValue(this)?.ToString() ?? ""));
            }
            return items;
        }
    }

    public struct InfoItemData
    {
        public string DisplayName;
        public string Value;

        public InfoItemData(string displayName, string value)
        {
            DisplayName = displayName;
            Value = value;
        }
    }
}
