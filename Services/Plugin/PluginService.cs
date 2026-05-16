namespace TewiMP.Services.Plugin;

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Newtonsoft.Json.Linq;
using TewiMP.Services.Storage;

public static class PluginService
{
    public static Dictionary<PluginInfo, Dictionary<string, object>> PluginInfoSettings { get; set; } = [];
    public static ObservableCollection<Plugin> Plugins { get; private set; } = [];
    public static ObservableCollection<MusicSourcePlugin> MusicSourcePlugins { get; private set; } = [];
    //private static AssemblyLoadContext assemblyLoadContext;

    public static void Init()
    {
        LogService.Log(nameof(PluginService), "初始化 PluginManager.");
        RemoveAllPlugin();

        DirectoryInfo directoryInfo = new(DataFolderBase.PluginFolder);
        var dllFiles = directoryInfo.GetFiles();
        LogService.Log(nameof(PluginService), $"Scanned plugins count: {dllFiles.Length}.");

        for (int i = 0; i < dllFiles.Length; i++)
        {
            if (dllFiles[i].Extension.ToLower() is not ".dll") continue;
            var dllFile = dllFiles[i];
            AddPlugin(dllFile.FullName);
        }

#if DEBUG
        Assembly assembly = Assembly.GetExecutingAssembly();

        // 程序自带插件
        string targetNamespace = "TewiMP.Services.Plugin.BuildInPlugins";
        var classes = assembly.GetTypes();
        var result = classes.Where(t => t.Namespace?.Contains(targetNamespace) == true && t.Name.Equals("Main")).ToList();

        foreach (var type in result)
        {
            AddPlugin(Activator.CreateInstance(type) as IPlugin);
        }
#endif

        LoadPluginInfoSettings();
        foreach (var p in Plugins) EnablePlugin(p);
        foreach (var p in MusicSourcePlugins) EnablePlugin(p);
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

    public static bool AddPlugin(string path)
    {
        var fileData = File.ReadAllBytes(path);
        Assembly asm = Assembly.Load(fileData);
        var manifestModuleName = asm.ManifestModule.ScopeName;
        var classLibraryName = manifestModuleName.Remove(manifestModuleName.LastIndexOf("."), manifestModuleName.Length - manifestModuleName.LastIndexOf("."));
        Type type = asm.GetType(classLibraryName + ".Main");

        if (typeof(IPlugin).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
        {
            var plugin = Activator.CreateInstance(type) as IPlugin;
            plugin.PluginInfo.path = path;
            AddPlugin(plugin);
        }
        else
        {
            App.MainWindowInstance.AddNotify("加载插件失败", $"\"{manifestModuleName}\" 加载失败：未继承 IPlugin 接口。");
            LogService.Log(nameof(PluginService), $"Load plugin failed: {manifestModuleName} does not inherit the IPlugin interface.");
            return false;
        }
        return true;
    }

    public static void AddPlugin(IPlugin plugin)
    {
        if (plugin is MusicSourcePlugin musicSourcePlugin)
        {
            AddPlugin(musicSourcePlugin);
        }
        else if (plugin is Plugin normalPlugin)
        {
            AddPlugin(normalPlugin);
        }
    }

    public static void AddPlugin(Plugin plugin)
    {
        if (string.IsNullOrEmpty(plugin.PluginInfo.GUID))
        {
            App.MainWindowInstance.AddNotify("加载插件失败", $"\"{plugin.PluginInfo.Name}\" 加载失败：插件 GUID 不能为空。");
            LogService.Error(nameof(PluginService), $"Load plugin failed: {plugin.PluginInfo.Name} GUID is null or empty.");
            return;
        }
        Plugins.Add(plugin);
        LogService.Log(nameof(PluginService), $"Loaded plugin: {plugin.PluginInfo.Name}, Guid: {plugin.PluginInfo.GUID}.");
    }

    public static void AddPlugin(MusicSourcePlugin plugin)
    {
        MusicSourcePlugins.Add(plugin);
        LogService.Log(nameof(PluginService), $"Loaded source plugin: {plugin.PluginInfo.Name}, Guid: {plugin.PluginInfo.GUID}.");
    }

    public static void RemovePlugin(Plugin plugin)
    {
        DisablePlugin(plugin);
        Plugins.Remove(plugin);
        LogService.Log(nameof(PluginService), $"Removed plugin: {plugin.PluginInfo.Name}, Guid: {plugin.PluginInfo.GUID}.");
    }

    public static void RemovePlugin(MusicSourcePlugin plugin)
    {
        DisablePlugin(plugin);
        MusicSourcePlugins.Remove(plugin);
        LogService.Log(nameof(PluginService), $"Removed source plugin: {plugin.PluginInfo.Name}, Guid: {plugin.PluginInfo.GUID}.");
    }

    public static void EnablePlugin(Plugin plugin)
    {
        plugin.OnEnable();
        LogService.Log(nameof(PluginService), $"Enabled plugin: {plugin.PluginInfo.Name}, Guid: {plugin.PluginInfo.GUID}.");
    }

    public static void DisablePlugin(Plugin plugin)
    {
        plugin.OnDisable();
        LogService.Log(nameof(PluginService), $"Disabled plugin: {plugin.PluginInfo.Name}, Guid: {plugin.PluginInfo.GUID}.");
    }

    public static void UpdatePluginInfoSettings()
    {
        PluginInfoSettings.Clear();
        foreach (var p in MusicSourcePlugins)
        {
            PluginInfoSettings.Add(p.PluginInfo, p.GetPluginSettings());
        }
        foreach (var p in Plugins)
        {
            PluginInfoSettings.Add(p.PluginInfo, p.GetPluginSettings());
        }
    }

    public static void LoadPluginInfoSettings()
    {
        JObject pluginSettingsData = DataFolderBase.PluginSettingsData;
        foreach (var item in pluginSettingsData)
        {
            var plugins = Plugins.Where(plugin => plugin.PluginInfo.GUID == item.Key);
            if (plugins.Any())
            {
                plugins.First().SetPluginSettings(item.Value.ToObject<Dictionary<string, object>>());
            }

            var musicSourcePlugins = MusicSourcePlugins.Where(plugin => plugin.PluginInfo.GUID == item.Key);
            if (musicSourcePlugins.Any())
            {
                musicSourcePlugins.First().SetPluginSettings(item.Value.ToObject<Dictionary<string, object>>());
            }
        }
        UpdatePluginInfoSettings();
    }

    public static void SavePluginInfoSettings()
    {
        UpdatePluginInfoSettings();
        var j = JObject.FromObject(PluginInfoSettings);
        DataFolderBase.PluginSettingsData = j;
    }

    public static void SetPluginSettingsToPlugin(Plugin plugin)
    {
        foreach (var settings in PluginInfoSettings)
        {
            if (plugin.PluginInfo != settings.Key) continue;
            plugin.SetPluginSettings(settings.Value);
        }
    }
}
