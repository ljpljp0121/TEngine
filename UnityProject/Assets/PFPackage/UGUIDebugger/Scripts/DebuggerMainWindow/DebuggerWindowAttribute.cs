/* 
****************************************************
* 文件：DebuggerWindowAttribute.cs
* 作者：PeiFeng
* 创建时间：2025/10/25 18:54:16 星期六
* 功能：窗口和方法注册特性
****************************************************
*/

using System;


[AttributeUsage(AttributeTargets.Class)]
public class DebuggerWindowAttribute  : Attribute
{
    public string Path;
    public int Order; //Order越小 按钮越靠后
    
    public DebuggerWindowAttribute(string path, int order = 0)
    {
        Path = path;
        Order = order;
    }
}

[AttributeUsage(AttributeTargets.Method)]
public class DebuggerBtnAttribute : Attribute
{
    public string Path;
    public int Order;
    public DebuggerBtnAttribute(string path,int order = 0)
    {
        Path = path;
        Order = order;
    }
}
