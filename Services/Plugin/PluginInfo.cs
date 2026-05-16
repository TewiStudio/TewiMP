namespace TewiMP.Services.Plugin;

using Newtonsoft.Json;
using System;
using System.Linq;

/// <summary>
/// 插件信息类。用于描述插件的基本信息。会被以 json 格式保存到配置文件中，程序在运行时读取这些信息以识别和获取对应插件。
/// </summary>
public class PluginInfo : IEquatable<PluginInfo>
{
    [JsonIgnore] public string Name { set; get; }
    [JsonIgnore] public string Author { set; get; }
    [JsonIgnore] public string Version { set; get; }
    [JsonIgnore] public string Describe { set; get; }
    [JsonIgnore] public string path { get; internal set; }
    public string GUID { get; set; } = null;

    /// <summary>
    /// 获取拼接后的插件名称和作者字符串。
    /// </summary>
    [JsonIgnore] public string NameAndAuthor => $"{Name} - {Author}";

    public override string ToString()
    {
        return GUID;
    }

    public override int GetHashCode()
    {
        return GUID.GetHashCode();
    }

    public bool Equals(PluginInfo other)
    {
        if (other is null) return false;
        return GetHashCode() == other.GetHashCode();
    }

    public override bool Equals(object obj) => Equals(obj as PluginInfo);

    public static bool operator ==(PluginInfo left, PluginInfo right)
    {
        if (left is null) return right is null;
        return left.Equals(right);
    }

    public static bool operator !=(PluginInfo left, PluginInfo right) => !(left == right);

}

public static class PluginInfoExtension
{
    public static Plugin GetPlugin(this string pluginInfoGUID, bool throwError = true)
    {
        var matched = PluginService.Plugins.Where(p => string.Equals(p.PluginInfo.GUID, pluginInfoGUID));
        if (!matched.Any())
        {
            if (throwError)
                throw new PluginNotFoundException($"找不到插件：{pluginInfoGUID}");
            else return null;
        }
        return matched.First();
    }

    public static MusicSourcePlugin GetMusicSourcePlugin(this string pluginInfoGUID, bool throwError = true)
    {
        var matched = PluginService.MusicSourcePlugins.Where(p => string.Equals(p.PluginInfo.GUID, pluginInfoGUID));
        if (!matched.Any())
        {
            if (throwError)
                throw new PluginNotFoundException($"找不到插件：{pluginInfoGUID}");
            else return null;
        }
        return matched.First();
    }
}
