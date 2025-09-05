using System;
using UnityEngine;

/// <summary>
/// 可序列化的颜色
/// </summary>
[Serializable]
public struct SerializedColor
{
    public float r, g, b, a;

    public SerializedColor(float r, float g, float b, float a)
    {
        this.r = r;
        this.g = g;
        this.b = b;
        this.a = a;
    }

    public override string ToString()
    {
        return $"({r},{g},{b},{a})";
    }

    public override int GetHashCode()
    {
        return this.ConvertToUnityColor().GetHashCode();
    }

    public static implicit operator SerializedColor(Color color)
    {
        return new SerializedColor(color.r, color.g, color.b, color.a);
    }

    public static implicit operator Color(SerializedColor color)
    {
        return new Color(color.r, color.g, color.b, color.a);
    }
}

public static class SerializationColorExtensions
{
    public static Color ConvertToUnityColor(this SerializedColor color)
    {
        return new Color(color.r, color.g, color.b, color.a);
    }

    public static SerializedColor ConvertToSerializationColor(this Color color)
    {
        return new SerializedColor(color.r, color.g, color.b, color.a);
    }

}