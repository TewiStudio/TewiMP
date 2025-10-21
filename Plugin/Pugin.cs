namespace TewiMP.Plugin;

using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;
using TewiMP.DataEditor;
using TewiMP.Pages.DialogPages;

/// <summary>
/// 插件基类。继承此类可被插件系统识别为插件。
/// </summary>
public abstract class Plugin
{
    [JsonIgnore] public bool IsEnable { get; private set; } = false;

    /// <summary>
    /// 设置插件信息。
    /// </summary>
    public abstract PluginInfo PluginInfo { get; }

    /// <summary>
    /// 插件设置字典。重写这里初始化插件的设置项。
    /// </summary>
    protected abstract Dictionary<string, object> PluginSettings { get; set; }

    /// <summary>
    /// 当插件被加载时调用。
    /// </summary>
    public virtual void OnEnable()
    {
        IsEnable = true;
    }

    /// <summary>
    /// 当插件被卸载时调用。
    /// </summary>
    public virtual void OnDisable()
    {
        IsEnable = false;
    }

    /// <summary>
    /// 当插件设置被修改时调用。
    /// </summary>
    protected virtual void OnSettingsChanged(string key, object value)
    {

    }

    /// <summary>
    /// 当插件设置字典被修改时调用。
    /// </summary>
    protected virtual void OnPluginSettingsChanged()
    {

    }

    /// <summary>
    /// 获取插件设置值，如果没有则添加一个默认值。
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="keyString"></param>
    /// <param name="defaultValue"></param>
    /// <returns></returns>
    public T GetSetting<T>(string keyString, T defaultValue = default)
    {
        if (PluginSettings is not null && PluginSettings.TryGetValue(keyString, out object value))
        {
            return (T)value;
        }
        else
        {
            PluginSettings?.Add(keyString, defaultValue);
            return defaultValue;
        }
    }

    /// <summary>
    /// 设置插件设置值，如果没有则添加设置的值。
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="keyString"></param>
    /// <param name="value"></param>
    public void SetSetting<T>(string keyString, T value)
    {
        if (PluginSettings.ContainsKey(keyString))
        {
            PluginSettings[keyString] = value;
        }
        else
        {
            PluginSettings.Add(keyString, value);
        }
        PluginManager.UpdatePluginInfoSettings();
        OnSettingsChanged(keyString, value);
    }

    /// <summary>
    /// 更改插件设置字典数据。
    /// </summary>
    /// <param name="settings"></param>
    public void SetPluginSettings(Dictionary<string, object> settings)
    {
        PluginSettings = settings;
        OnPluginSettingsChanged();
    }

    public Dictionary<string, object> GetPluginSettings()
    {
        return PluginSettings;
    }

    /// <summary>
    /// 通过 Key 返回向用户显示的插件设置名。用户界面通过此方法获取每个插件设置的名称。
    /// </summary>
    /// <param name="keyString"></param>
    /// <returns></returns>
    public virtual string GetUserViewPluginSettingName(string keyString)
    {
        return keyString;
    }

    /// <summary>
    /// 通过 Key 返回向用户显示的插件设置描述。用户界面通过此方法获取插件设置描述。
    /// </summary>
    /// <param name="keyString"></param>
    /// <returns></returns>
    public virtual string GetUserViewPluginSettingDescribe(string keyString)
    {
        return null;
    }

    /// <summary>
    /// 调用 <see cref="MainWindow"/> 显示此插件的设置对话框。
    /// </summary>
    /// <returns></returns>
    public async Task ShowSettingsDialog()
    {
        await App.MainWindowInstance.ShowDialog(PluginInfo.Name, new PluginSetter() { Plugin = this }, "返回");
    }

    public static bool operator ==(Plugin left, Plugin right)
    {
        if (left is null && right is null) return true;
        if (left is null || right is null) return false;
        return left.PluginInfo == right.PluginInfo;
    }

    public static bool operator !=(Plugin left, Plugin right)
    {
        if (left is null && right is null) return false;
        if (left is null || right is null) return true;
        return !(left.PluginInfo == right.PluginInfo);
    }

    public override bool Equals(object other)
    {
        if (other is not Plugin) return false;
        return PluginInfo.Equals(PluginInfo, (other as Plugin).PluginInfo);
    }

    public override int GetHashCode()
    {
        return (PluginInfo != null ? PluginInfo.GetHashCode() : 0);
    }
}

/// <summary>
/// 音乐源插件基类。继承此类可被插件系统识别为音乐源插件，会被系统用于加载音乐数据。
/// </summary>
public abstract class MusicSourcePlugin : Plugin
{
    /// <summary>
    /// 通过 <paramref name="id"/> 获取歌曲音频文件地址。
    /// </summary>
    /// <param name="id"></param>
    /// <param name="br">音频文件质量</param>
    /// <returns></returns>
    public abstract Task<string> GetUrl(string id, int br);

    /// <summary>
    /// 通过 <paramref name="id"/> 获取歌曲歌词，返回值为歌词内容和翻译内容的元组。
    /// </summary>
    /// <param name="id"></param>
    /// <returns><see cref="Tuple{Lyric, LyricTranslate}"/> (Lyric, LyricTranslate)</returns>
    public abstract Task<Tuple<string, string>> GetLyric(string id);

    /// <summary>
    /// 通过 <paramref name="id"/> 获取歌曲专辑封面图片地址。
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public abstract Task<string> GetPic(string id);

    /// <summary>
    /// 通过 <paramref name="musicData"/> 获取歌曲专辑封面图片地址。
    /// </summary>
    /// <param name="musicData"></param>
    /// <returns></returns>
    public abstract Task<string> GetPicFromMusicData(MusicData musicData);

    /// <summary>
    /// 通过 <paramref name="keyword"/> 搜索音乐，返回搜索结果对象。
    /// TODO: 规范返回对象类型。
    /// </summary>
    /// <param name="keyword"></param>
    /// <param name="pageNumber"></param>
    /// <param name="pageSize"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    public abstract Task<object> GetSearch(string keyword, int pageNumber = 1, int pageSize = 30, int type = 0);

    /// <summary>
    /// 通过 <paramref name="id"/> 获取歌单信息。
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public abstract Task<MusicListData> GetPlayList(string id);

    /// <summary>
    /// 通过 <paramref name="id"/> 获取艺术家信息。
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public abstract Task<Artist> GetArtist(string id);

    /// <summary>
    /// 通过 <paramref name="id"/> 获取专辑信息。
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public abstract Task<Album> GetAlbum(string id);

    /// <summary>
    /// 通过 <paramref name="id"/> 获取歌曲信息。
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public abstract Task<MusicData> GetMusicData(string id);

    public override string ToString()
    {
        return PluginInfo.Name;
    }
}

/// <summary>
/// 插件信息类。用于描述插件的基本信息。会被以 json 格式保存到配置文件中，程序在运行时读取这些信息以识别和获取对应插件。
/// </summary>
public class PluginInfo
{
    public string Name { set; get; }
    public string Author { set; get; }
    public string Version { set; get; }
    public string Describe { set; get; }

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
        var matched = PluginManager.Plugins.Where(p => p.PluginInfo == this);
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
        var matched = PluginManager.MusicSourcePlugins.Where(p => p.PluginInfo == this);
        if (!matched.Any())
        {
            if (throwError)
                throw new PluginNotFoundException($"找不到插件：{NameAndAuthor}");
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

    public override int GetHashCode()
    {
        return (string.IsNullOrEmpty(NameAndAuthor) ? StringComparer.InvariantCulture.GetHashCode(NameAndAuthor) : 0);
    }
}

public class PluginLoadException : Exception
{
    public PluginLoadException() : base() { }
    public PluginLoadException(string message) : base(message) { }
    public PluginLoadException(string message, Exception innerException) : base(message, innerException) { }
}

public class PluginNotFoundException : Exception
{
    public PluginNotFoundException() : base() { }
    public PluginNotFoundException(string message) : base(message) { }
    public PluginNotFoundException(string message, Exception innerException) : base(message, innerException) { }
}
