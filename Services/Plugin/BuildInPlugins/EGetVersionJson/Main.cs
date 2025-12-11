using System.Collections.Generic;
using TewiMP.Services.Plugin;

namespace TewiMP.Services.Plugin.BuildInPlugins.EGetVersionJson;

public class Main : Plugin
{
    public override PluginInfo PluginInfo => new()
    {
        Name = "EGetVersionJson",
        Describe = "获取版本信息的 Json 字符串",
        Author = "TewiStudio",
        Version = "1.0.0",
    };
    protected override Dictionary<string, object> PluginSettings { get; set; } = new()
        {
            { "Json", "" },
        };

    public override void OnEnable()
    {
        SetSetting("Json", Newtonsoft.Json.Linq.JObject.FromObject(App.Instance.NowVersion).ToString());
    }
}
