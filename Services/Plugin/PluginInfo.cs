using System;
using System.Linq;
using Newtonsoft.Json;

namespace TewiMP.Services.Plugin;

/// <summary>
/// 插件信息类。用于描述插件的基本信息。会被以 json 格式保存到配置文件中，程序在运行时读取这些信息以识别和获取对应插件。
/// </summary>
public class PluginInfo
{
    public string Name { set; get; }
    public string Author { set; get; }
    public string Version { set; get; }
    public string Describe { set; get; }
    public Guid ID { set; get; } = Guid.NewGuid();

    /// <summary>
    /// 获取拼接后的插件名称和作者字符串。
    /// </summary>
    [JsonIgnore] public string NameAndAuthor => $"{Name} - {Author}";

    /// <summary>
    /// 通过此实例储存的信息获取对应插件实例。
    /// </summary>
    /// <param name="throwError"></param>
    /// <returns></returns>
    /// <exception cref="PluginNotFoundException"></exception>
    public Plugin GetPlugin(bool throwError = true)
    {
        var matched = PluginService.Plugins.Where(p => p.PluginInfo == this);
        if (!matched.Any())
        {
            if (throwError)
                throw new PluginNotFoundException($"找不到插件：{NameAndAuthor}");
            else return null;
        }
        return matched.First();
    }

    /// <summary>
    /// 通过此实例储存的信息获取对应音乐源插件实例。
    /// </summary>
    /// <param name="throwError"></param>
    /// <returns></returns>
    /// <exception cref="PluginNotFoundException"></exception>
    public MusicSourcePlugin GetMusicSourcePlugin(bool throwError = true)
    {
        var matched = PluginService.MusicSourcePlugins.Where(p => p.PluginInfo == this);
        if (!matched.Any())
        {
            if (throwError)
                throw new PluginNotFoundException($"找不到插件：{NameAndAuthor} / {ID}");
            else return null;
        }
        return matched.First();
    }

    public override string ToString()
    {
        return $"{Name}{Author}";
    }

    public static bool operator ==(PluginInfo left, PluginInfo right)
    {
        if (left is null && right is null) return true;
        if (left is null || right is null) return false;
        return left.NameAndAuthor == right.NameAndAuthor;
    }

    public static bool operator !=(PluginInfo left, PluginInfo right)
    {
        if (left is null && right is null) return false;
        if (left is null || right is null) return true;
        return !(left.NameAndAuthor == right.NameAndAuthor);
    }

    public override bool Equals(object other)
    {
        if (!(other is PluginInfo)) return false;
        return string.Equals(NameAndAuthor, (other as PluginInfo).NameAndAuthor, StringComparison.InvariantCulture);
    }

    public override int GetHashCode() => NameAndAuthor?.GetHashCode() ?? 0;
}
