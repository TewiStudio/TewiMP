using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;

namespace TewiMP.Plugin
{
    public static class PluginManager
    {
        public static ObservableCollection<Plugin> Plugins { get; private set; } = [];
        public static ObservableCollection<MusicSourcePlugin> MusicSourcePlugins { get; private set; } = [];

        public static void Init()
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
            EnablePlugin(plugin);
            App.logManager.Log("PluginManager", $"Loaded plugin: {plugin.PluginInfo.Name}.");
        }

        public static void AddPlugin(MusicSourcePlugin plugin)
        {
            MusicSourcePlugins.Add(plugin);
            EnablePlugin(plugin);
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
    }
}
