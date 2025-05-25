using System.Collections.Generic;

namespace TewiMP.Plugin.BuildInPlugins.BuildInPluginSample
{
    public class Main : Plugin
    {
        public override PluginInfo PluginInfo => new()
        {
            Name = "BuildIn Plugin Sample",
            Author = "TewiStudio",
            Version = "justTest",
        };
        protected override Dictionary<string, object> PluginSettings { get; set; } = new()
        {
            { "Settings Test String", "Test" },
            { "Settings Test Number", 123 },
            { "Settings Test Bool", true },
            { "Settings Test List", new List<string> { "1", "2", "3" } }
        };

        public override void OnEnable()
        {
            base.OnEnable();
            App.logManager.Log("BuildIn Plugin Sample", "I have been enabled!");
        }

        public override void OnDisable()
        {
            base.OnDisable();
            App.logManager.Log("BuildIn Plugin Sample", "I have been disabled!");
        }

        protected override void OnSettingsChanged(string key, object value)
        {
            base.OnSettingsChanged(key, value);
            App.logManager.Log("BuildIn Plugin Sample", $"Settings \"{key}\" has been changed to {value}!");
        }
    }
}
