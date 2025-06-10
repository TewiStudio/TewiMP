using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TewiMP.DataEditor;
using TewiMP.Pages.DialogPages;

namespace TewiMP.Plugin
{
    public abstract class Plugin
    {
        [JsonIgnore] public bool IsEnable { get; private set; } = false;
        public abstract PluginInfo PluginInfo { get; }
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
        /// 当插件设置被修改时调用
        /// </summary>
        protected virtual void OnSettingsChanged(string key, object value)
        {

        }

        /// <summary>
        /// 当插件设置字典被修改时调用
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
            PluginManager.UpdatePluginInfoSettings();
            OnSettingsChanged(keyString, value);
        }

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
        /// 通过 Key 返回向用户显示的插件设置名
        /// </summary>
        /// <param name="keyString"></param>
        /// <returns></returns>
        public virtual string GetUserViewPluginSettingName(string keyString)
        {
            return keyString;
        }

        /// <summary>
        /// 通过 Key 返回向用户显示的插件设置描述
        /// </summary>
        /// <param name="keyString"></param>
        /// <returns></returns>
        public virtual string GetUserViewPluginSettingDescribe(string keyString)
        {
            return null;
        }

        public async Task ShowSettingsDialog()
        {
            await MainWindow.ShowDialog(PluginInfo.Name, new PluginSetter() { Plugin = this }, "返回");
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

    public abstract class MusicSourcePlugin : Plugin
    {
        public abstract Task<string> GetUrl(string id, int br);
        public abstract Task<Tuple<string, string>> GetLyric(string id);
        public abstract Task<string> GetPic(string id);
        public abstract Task<string> GetPicFromMusicData(MusicData musicData);
        public abstract Task<object> GetSearch(string keyword, int pageNumber = 1, int pageSize = 30, int type = 0);
        public abstract Task<MusicListData> GetPlayList(string id);
        public abstract Task<Artist> GetArtist(string id);
        public abstract Task<Album> GetAlbum(string id);
        public abstract Task<MusicData> GetMusicData(string id);

        public override string ToString()
        {
            return PluginInfo.Name;
        }
    }

    public class PluginInfo
    {
        public string Name { set; get; }
        public string Author { set; get; }
        public string Version { set; get; }
        public string Describe { set; get; }
        [JsonIgnore] public string NameAndAuthor => $"{Name} - {Author}";

        public Plugin GetPlugin(bool throwError = true)
        {
            var matched = PluginManager.Plugins.Where(p => p.PluginInfo == this);
            if (!matched.Any())
            {
                if (throwError)
                    throw new PluginNotFoundException($"找不到插件：{NameAndAuthor}");
                else return null;
            }
            return matched.First();
        }

        public MusicSourcePlugin GetMusicSourcePlugin(bool throwError = true)
        {
            var matched = PluginManager.MusicSourcePlugins.Where(p => p.PluginInfo == this);
            if (!matched.Any())
            {
                if (throwError)
                    throw new PluginNotFoundException($"找不到插件：{NameAndAuthor}");
                else return null;
            }
            return matched.First();
        }

        public override string ToString()
        {
            return $"{Name}{Author}";
        }

        public static bool operator ==(PluginInfo left, PluginInfo right)
        {
            if (left is null && right is null) return true;
            if (left is null || right is null) return false;
            return left.NameAndAuthor == right.NameAndAuthor;
        }

        public static bool operator !=(PluginInfo left, PluginInfo right)
        {
            if (left is null && right is null) return false;
            if (left is null || right is null) return true;
            return !(left.NameAndAuthor == right.NameAndAuthor);
        }

        public override bool Equals(object other)
        {
            if (!(other is PluginInfo)) return false;
            return string.Equals(NameAndAuthor, (other as PluginInfo).NameAndAuthor, StringComparison.InvariantCulture);
        }

        public override int GetHashCode()
        {
            return (string.IsNullOrEmpty(NameAndAuthor) ? StringComparer.InvariantCulture.GetHashCode(NameAndAuthor) : 0);
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
}
