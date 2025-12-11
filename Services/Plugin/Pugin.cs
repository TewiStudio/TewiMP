using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;
using TewiMP.UI.Pages.DialogPages;
using TewiMP;

namespace TewiMP.Services.Plugin;

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
        PluginService.UpdatePluginInfoSettings();
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
    /// 调用 <see cref=nameof(MainWindow)/> 显示此插件的设置对话框。
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
