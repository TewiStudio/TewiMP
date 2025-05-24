using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using TewiMP.DataEditor;

namespace TewiMP.Plugin
{
    public static class PluginManager
    {
        public static List<PluginInfo> PluginInfoSettings = [];
        public static ObservableCollection<Plugin> Plugins { get; private set; } = [];
        public static ObservableCollection<MusicSourcePlugin> MusicSourcePlugins { get; private set; } = [];
        //private static AssemblyLoadContext assemblyLoadContext;

        public static void Init()
        {
            RemoveAllPlugin();
            /*
            assemblyLoadContext?.Unload();
            GC.Collect();
            assemblyLoadContext = new AssemblyLoadContext("Plugins", true);*/

            DirectoryInfo directoryInfo = new(DataFolderBase.PluginFolder);
            var dllFiles = directoryInfo.GetFiles();
            App.logManager.Log("PluginManager", $"Scanned plugins count: {dllFiles.Length}");

            for (int i = 0; i < dllFiles.Length; i++)
            {
                if (dllFiles[i].Extension.ToLower() is not ".dll") continue;
                var fileData = File.ReadAllBytes(dllFiles[i].FullName);

                //Assembly asm = assemblyLoadContext.LoadFromStream(new MemoryStream(fileData));
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
                    MainWindow.AddNotify("加载插件失败", $"\"{manifestModuleName}\" 加载失败：未继承 Plugin 类。");
                    App.logManager.Log("PluginManager", $"Load plugin failed: {manifestModuleName} does not inherit the IPlugin interface.");
                    continue;
                }

                if (isMusicSourcePlugin)
                    AddPlugin(Activator.CreateInstance(type) as MusicSourcePlugin);
                else
                    AddPlugin(Activator.CreateInstance(type) as Plugin);
            }

            Assembly assembly = Assembly.GetExecutingAssembly();

            // 程序自带插件
            string targetNamespace = "TewiMP.Plugin.BuildInPlugins";
            var classes = assembly.GetTypes();
            var result = classes.Where(t => t.Namespace?.Contains(targetNamespace) == true && t.Name.Equals("Main")).ToList();

            foreach (var type in result)
            {
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
                    MainWindow.AddNotify("加载插件失败", $"\"{type}\" 加载失败：未继承 IPlugin 接口。");
                    App.logManager.Log("PluginManager", $"Load plugin failed: {type} does not inherit the IPlugin interface.");
                    continue;
                }
                if (isMusicSourcePlugin)
                    AddPlugin(Activator.CreateInstance(type) as MusicSourcePlugin);
                else
                    AddPlugin(Activator.CreateInstance(type) as Plugin);
            }

            JArray pluginSettingsData = DataFolderBase.PluginSettingsData;
            PluginInfoSettings = pluginSettingsData.ToObject<List<PluginInfo>>() ?? [];
            foreach (var p in Plugins)
            {
                SetPluginSettingsToPlugin(p);
                EnablePlugin(p);
            }
            foreach (var p in MusicSourcePlugins)
            {
                SetPluginSettingsToPlugin(p);
                EnablePlugin(p);
            }
        }

        public static void RemoveAllPlugin()
        {
            foreach (var plugin in Plugins)
            {
                DisablePlugin(plugin);
            }
            foreach (var plugin in MusicSourcePlugins)
            {
                DisablePlugin(plugin);
            }
            Plugins.Clear();
            MusicSourcePlugins.Clear();
        }

        public static void AddPlugin(Plugin plugin)
        {
            Plugins.Add(plugin);
            App.logManager.Log("PluginManager", $"Loaded plugin: {plugin.PluginInfo.Name}.");
        }

        public static void AddPlugin(MusicSourcePlugin plugin)
        {
            MusicSourcePlugins.Add(plugin);
            App.logManager.Log("PluginManager", $"Loaded source plugin: {plugin.PluginInfo.Name}.");
        }

        public static void RemovePlugin(Plugin plugin)
        {
            DisablePlugin(plugin);
            Plugins.Remove(plugin);
            App.logManager.Log("PluginManager", $"Removed plugin: {plugin.PluginInfo.Name}.");
        }

        public static void RemovePlugin(MusicSourcePlugin plugin)
        {
            DisablePlugin(plugin);
            MusicSourcePlugins.Remove(plugin);
            App.logManager.Log("PluginManager", $"Removed source plugin: {plugin.PluginInfo.Name}.");
        }

        public static void EnablePlugin(Plugin plugin)
        {
            plugin.OnEnable();
        }

        public static void DisablePlugin(Plugin plugin)
        {
            plugin.OnDisable();
        }

        public static void UpdatePluginInfoSettings()
        {
            PluginInfoSettings.Clear();
            foreach (var p in MusicSourcePlugins) PluginInfoSettings.Add(p.PluginInfo);
            foreach (var p in Plugins) PluginInfoSettings.Add(p.PluginInfo);
        }

        public static void SavePluginInfoSettings()
        {
            UpdatePluginInfoSettings();
            DataFolderBase.PluginSettingsData = JArray.FromObject(PluginInfoSettings);
        }

        public static void SetPluginSettingsToPlugin(Plugin plugin)
        {
            foreach (var p in PluginInfoSettings)
            {
                if (plugin.PluginInfo == p)
                {
                    plugin.SetPluginSettings(p.PluginSettings);
                }
            }
        }
    }
}
