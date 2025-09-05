using System;
using UnityEngine;

[Serializable]
public class SerializedVector3 : IEquatable<SerializedVector3>
{
    public float x, y, z;

    public SerializedVector3(float x, float y, float z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    public bool Equals(SerializedVector3 other)
    {
        return this.x == other.x && this.y == other.y && this.z == other.z;
    }

    public override string ToString()
    {
        return $"({x},{y},{z})";
    }
    public override int GetHashCode()
    {
        return x.GetHashCode() ^ (y.GetHashCode() << 2) ^ (z.GetHashCode() >> 2);
    }

    public static implicit operator SerializedVector3(Vector3 vector3)
    {
        return new SerializedVector3(vector3.x, vector3.y, vector3.z);
    }
    public static implicit operator Vector3(SerializedVector3 vector3)
    {
        return new Vector3(vector3.x, vector3.y, vector3.z);
    }
    public static implicit operator SerializedVector3(Vector3Int vector3)
    {
        return new SerializedVector3(vector3.x, vector3.y, vector3.z);
    }
    public static implicit operator Vector3Int(SerializedVector3 vector3)
    {
        return new Vector3Int((int)vector3.x, (int)vector3.y, (int)vector3.z);
    }
}


[Serializable]
public struct SerializedVector2 : IEquatable<SerializedVector2>
{
    public float x, y;

    public SerializedVector2(float x, float y)
    {
        this.x = x;
        this.y = y;
    }
    public bool Equals(SerializedVector2 other)
    {
        return this.x == other.x && this.y == other.y;
    }

    public override string ToString()
    {
        return $"({x},{y})";
    }
    public override int GetHashCode()
    {
        return x.GetHashCode() ^ (y.GetHashCode() << 2);
    }
    public static implicit operator SerializedVector2(Vector2 vector2)
    {
        return new SerializedVector2(vector2.x, vector2.y);
    }
    public static implicit operator Vector2(SerializedVector2 vector2)
    {
        return new Vector2(vector2.x, vector2.y);
    }
    public static implicit operator SerializedVector2(Vector2Int vector2)
    {
        return new SerializedVector2(vector2.x, vector2.y);
    }
    public static implicit operator Vector2Int(SerializedVector2 vector2)
    {
        return new Vector2Int((int)vector2.x, (int)vector2.y);
    }
}


public static class SerializedVectorExtensions
{

    public static Vector3 ConverToVector3(this SerializedVector3 sv3)
    {
        return new Vector3(sv3.x, sv3.y, sv3.z);
    }

    public static SerializedVector3 ConverToSVector3(this Vector3 v3)
    {
        return new SerializedVector3(v3.x, v3.y, v3.z);
    }

    public static Vector3Int ConverToVector3Int(this SerializedVector3 sv3)
    {
        return new Vector3Int((int)sv3.x, (int)sv3.y, (int)sv3.z);
    }

    public static SerializedVector3 ConverToSVector3Int(this Vector3Int v3)
    {
        return new SerializedVector3(v3.x, v3.y, v3.z);
    }

    public static Vector2 ConverToSVector2(this SerializedVector2 sv2)
    {
        return new Vector2(sv2.x, sv2.y);
    }

    public static Vector2Int ConverToVector2Int(this SerializedVector2 sv2)
    {
        return new Vector2Int((int)sv2.x, (int)sv2.y);
    }

    public static SerializedVector2 ConverToSVector2(this Vector2 v2)
    {
        return new SerializedVector2(v2.x, v2.y);
    }

    public static SerializedVector2 ConverToSVector2(this Vector2Int v2)
    {
        return new SerializedVector2(v2.x, v2.y);
    }
}