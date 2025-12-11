namespace TewiMP.Core.Models;

using System;
using Newtonsoft.Json;

public abstract class OnlyClass
{
    string md5;
    [JsonIgnore]
    public string MD5
    {
        get
        {
            if (md5 is null)
                md5 = GetMD5();
            return md5;
        }
    }

    public abstract string GetMD5();

    public static bool operator ==(OnlyClass left, OnlyClass right)
    {
        if (left is null && right is null) return true;
        if (left is null || right is null) return false;
        return left.MD5 == right.MD5;
    }

    public static bool operator !=(OnlyClass left, OnlyClass right)
    {
        if (left is null && right is null) return false;
        if (left is null || right is null) return true;
        return !(left.MD5 == right.MD5);
    }

    public override bool Equals(object other)
    {
        if (!(other is OnlyClass)) return false;
        return string.Equals(MD5, (other as OnlyClass).MD5, StringComparison.InvariantCulture);
    }

    public override int GetHashCode()
    {
        return (MD5 != null ? StringComparer.InvariantCulture.GetHashCode(MD5) : 0);
    }
}
