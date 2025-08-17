using System;
using System.Reflection;
using TEngine;

public class InitOnLoadConfig
{
    private static bool loaded = false;

    public static void Init()
    {
        if (!loaded)
        {
            InitOnLoadMethod.ProcessInitOnLoadMethod(typeof(InitOnLoadConfig));
            Log.Info("InitOnLoad : Client_Config");
            loaded = true;
        }
    }
}

[AttributeUsage(AttributeTargets.Method)]
public class InitOnLoadAttribute : Attribute { }

public class InitOnLoadMethod
{
    public static void ProcessInitOnLoadMethod(Type assemblyClassType)
    {
        Type[] types = assemblyClassType.Assembly.GetTypes();
        foreach (Type type in types)
        {
            MethodInfo[] info = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            foreach (MethodInfo method in info)
            {
                foreach (var attribute in method.GetCustomAttributes(false))
                {
                    if (attribute.GetType() == typeof(InitOnLoadAttribute))
                    {
                        try
                        {
                            method.Invoke(null, null);
                        }
                        catch (Exception e)
                        {
                            Log.Error("InitOnLoadMethod.ProcessInitOnLoadMethod: {0} ,StackTrace: {1}", e, e.StackTrace);
                        }
                    }
                }
            }
        }
    }
}