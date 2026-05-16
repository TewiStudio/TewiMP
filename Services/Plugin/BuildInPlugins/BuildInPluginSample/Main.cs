namespace TewiMP.Services.Plugin.BuildInPlugins.BuildInPluginSample; // 使用 namespace 区别不同的插件

using System.Collections.Generic;
using TewiMP.Services;
using TewiMP.Services.Plugin;

// 每个插件的入口，需要继承自 Plugin 类，否则无法被识别为插件
public class Main : Plugin
{
    // 自定义插件信息
    public override PluginInfo PluginInfo => new()
    {
        Name = "BuildIn Plugin Sample",
        Author = "TewiStudio",
        Version = "Test",
        GUID = "tewi.buildin.sample"
    };

    // 自定义插件设置，键值对形式存储，会自动保存和加载，会显示在插件设置界面
    protected override Dictionary<string, object> PluginSettings { get; set; } = new()
    {
        { "Settings Test String", "Test" },
        { "Settings Test Number", 123 },
        { "Settings Test Bool", true },
        { "Settings Test List", new List<string> { "1", "2", "3" } }
    };

    // 当插件被启用时调用
    public override void OnEnable()
    {
        base.OnEnable();
        LogService.Log("BuildIn Plugin Sample", "I have been enabled!");
    }

    // 当插件被禁用时调用
    public override void OnDisable()
    {
        base.OnDisable();
        LogService.Log("BuildIn Plugin Sample", "I have been disabled!");
    }

    // 当插件设置被更改时调用
    protected override void OnSettingsChanged(string key, object value)
    {
        base.OnSettingsChanged(key, value);
        LogService.Log("BuildIn Plugin Sample", $"Settings \"{key}\" has been changed to {value}!");
    }

    // 当插件的所有设置被更改时调用
    protected override void OnPluginSettingsChanged()
    {
        base.OnPluginSettingsChanged();
    }

    // 用户界面通过此方法获取插件设置描述，可以自定义每个设置项的描述
    public override string GetUserViewPluginSettingDescribe(string keyString)
    {
        if (keyString == "Settings Test List")
        {
            return "This is a test list setting.";
        }
        return base.GetUserViewPluginSettingDescribe(keyString);
    }

    // 用户界面通过此方法获取每个插件设置的名称，可以自定义每个设置项的名称
    public override string GetUserViewPluginSettingName(string keyString)
    {
        if (keyString == "Settings Test List") return "列表";
        return base.GetUserViewPluginSettingName(keyString);
    }
}
