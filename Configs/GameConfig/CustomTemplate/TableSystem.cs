using System.Collections.Generic;
using GameConfig;

/// <summary>
/// 配置加载器。
/// </summary>
public class TableSystem
{
    private static Dictionary<string, ITable> tables = new Dictionary<string, ITable>();

    public static T GetTable<T>() where T : ITable, new()
    {
        if (tables.ContainsKey(typeof(T).Name))
        {
            return (T)tables[typeof(T).Name];
        }
        else
        {
            var table = new T();
            table._LoadData();
            tables.Add(typeof(T).Name, table);
            return (T)table;
        }
    }
    
}