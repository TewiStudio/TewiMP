using System.Collections.Generic;
using TewiMP.DataEditor;
using TewiMP.Media;

namespace TewiMP.Plugin.BuildInPlugins.FFmpegPathSelector
{
    public class Main : Plugin
    {
        public override PluginInfo PluginInfo => new()
        {
            Name = "FFmpeg Path Selector",
            Describe = "自定义 ffmpeg.exe 程序的路径",
            Author = "TewiStudio",
            Version = "1.0.0",
        };
        protected override Dictionary<string, object> PluginSettings { get; set; } = new()
        {
            { "ffmpegPath", "" },
        };

        public override string GetUserViewPluginSettingName(string keyString)
        {
            return "ffmpeg.exe 路径";
        }

        public override string GetUserViewPluginSettingDescribe(string keyString)
        {
            return "设置 ffmpeg.exe 的路径，留空则使用默认路径。";
        }

        protected override void OnSettingsChanged(string key, object value)
        {
            if (key == "ffmpegPath")
            {
                AudioFileReader.FFmpegPath = string.IsNullOrEmpty(value as string) ? DataEditor.DataFolderBase.FFmpegPath : value as string;
            }
            else
            {
                base.OnSettingsChanged(key, value);
            }
        }

        public override void OnEnable()
        {
            var path = GetSetting("ffmpegPath", "");
            AudioFileReader.FFmpegPath = string.IsNullOrEmpty(path) ? DataFolderBase.FFmpegPath : path;
        }
    }
}
