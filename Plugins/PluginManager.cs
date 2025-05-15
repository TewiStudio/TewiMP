using System;
using System.IO;
using System.Reflection;
using System.Collections.ObjectModel;

namespace TewiMP.Plugins
{
    public class PluginManager
    {
        public ObservableCollection<Plugin> Plugins { get; private set; } = [];
        public ObservableCollection<MusicSourcePlugin> MusicSourcePlugins { get; private set; } = [];

        public void Init()
        {
            RemoveAllPlugin();
            DirectoryInfo directoryInfo = new(DataEditor.DataFolderBase.PluginFolder);
            var dllFiles = directoryInfo.GetFiles();
            App.logManager.Log("PluginManager", $"Scanned plugins count: {dllFiles.Length}");

            for (int i = 0; i < dllFiles.Length; i++)
            {
                var fileData = File.ReadAllBytes(dllFiles[i].FullName);
                Assembly asm = Assembly.Load(fileData);
                var manifestModuleName = asm.ManifestModule.ScopeName;
                var classLibraryName = manifestModuleName.Remove(manifestModuleName.LastIndexOf("."), manifestModuleName.Length - manifestModuleName.LastIndexOf("."));
                Type type = asm.GetType(classLibraryName + ".Main");

                bool isMusicSourcePlugin = false;
                if (typeof(MusicSourcePlugin).IsAssignableFrom(type))
                {
                    isMusicSourcePlugin = true;
                }
                else if (typeof(Plugin).IsAssignableFrom(type))
                {

                }
                else
                {
                    MainWindow.AddNotify("加载插件失败", $"\"{manifestModuleName}\" 加载失败：未继承 IPlugin 接口。");
                    App.logManager.Log("PluginManager", $"Load plugin failed: {manifestModuleName} does not inherit the IPlugin interface.");
                    continue;
                }

                AddPlugin(Activator.CreateInstance(type) as Plugin, isMusicSourcePlugin);
            }
        }

        public void RemoveAllPlugin()
        {
            foreach (var plugin in MusicSourcePlugins)
            {
                DisablePlugin(plugin);
            }
            MusicSourcePlugins.Clear();
        }

        public void AddPlugin(Plugin plugin, bool isMusicSourcePlugin = false)
        {
            if (isMusicSourcePlugin) MusicSourcePlugins.Add(plugin as MusicSourcePlugin);
            else Plugins.Add(plugin);

            EnablePlugin(plugin);
            App.logManager.Log("PluginManager", $"Loaded plugin: {plugin.PluginInfo.Name}.");
        }

        public void RemovePlugin(Plugin plugin, bool isMusicSourcePlugin = false)
        {
            DisablePlugin(plugin);
            if (isMusicSourcePlugin) MusicSourcePlugins.Remove(plugin as MusicSourcePlugin);
            else Plugins.Remove(plugin);
            App.logManager.Log("PluginManager", $"Removed plugin: {plugin.PluginInfo.Name}.");
        }

        public void EnablePlugin(Plugin plugin)
        {
            plugin.OnEnable();
        }

        public void DisablePlugin(Plugin plugin)
        {
            plugin.OnDisable();
        }
    }
}
