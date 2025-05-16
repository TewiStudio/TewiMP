namespace TewiMP.Plugin.BuildInPlugins.BuildInPluginSample
{
    public class Main : Plugin
    {
        public override PluginInfo PluginInfo => new()
        {
            Name = "BuildIn Plugin Sample",
            Author = "TewiStudio",
            Version = "justTest"
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
    }
}
