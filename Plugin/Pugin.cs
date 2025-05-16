using Newtonsoft.Json;
using System;
using System.Linq;
using System.Threading.Tasks;
using TewiMP.DataEditor;

namespace TewiMP.Plugin
{
    public abstract class Plugin
    {
        [JsonIgnore]
        public bool IsEnable { get; private set; } = false;
        public abstract PluginInfo PluginInfo { get; }

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
        [JsonIgnore] public string NameAndAuthor => $"{Name} - {Author}";

        public Plugin GetPlugin() => PluginManager.Plugins.First(p => p.PluginInfo == this);

        public MusicSourcePlugin GetMusicSourcePlugin()
        {
            var matched = PluginManager.MusicSourcePlugins.Where(p => p.PluginInfo == this);
            if (!matched.Any()) throw new PluginNotFoundException($"找不到插件：{NameAndAuthor}");
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

    public class PluginNotFoundException : Exception
    {
        public PluginNotFoundException() : base() { }
        public PluginNotFoundException(string message) : base(message) { }
        public PluginNotFoundException(string message, Exception innerException) : base(message, innerException) { }
    }
}
