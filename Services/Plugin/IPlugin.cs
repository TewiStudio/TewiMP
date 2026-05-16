namespace TewiMP.Services.Plugin;

using System.Collections.Generic;
using System.Threading.Tasks;

public interface IPlugin
{
    bool IsEnable { get; }

    /// <summary>
    /// 设置插件信息。
    /// </summary>
    PluginInfo PluginInfo { get; }

    bool Equals(object other);
    int GetHashCode();
    Dictionary<string, object> GetPluginSettings();
    T GetSetting<T>(string keyString, T defaultValue = default);
    string GetUserViewPluginSettingDescribe(string keyString);
    string GetUserViewPluginSettingName(string keyString);
    void OnDisable();
    void OnEnable();
    void SetPluginSettings(Dictionary<string, object> settings);
    void SetSetting<T>(string keyString, T value);
    Task ShowSettingsDialog();
}
