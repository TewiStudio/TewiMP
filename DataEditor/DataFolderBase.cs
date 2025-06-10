using System;
using System.IO;
using Microsoft.UI.Xaml;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TewiMP.Windowed;
using TewiMP.Background;
using TewiMP.Background.HotKeys;
using TewiMP.Plugin;

namespace TewiMP.DataEditor
{
    public static class DataFolderBase
    {
        /// <summary>
        /// 插件路径
        /// </summary>
        public static string PluginFolder { get; set; } = Path.Combine(Environment.CurrentDirectory, "Plugins");

        /// <summary>
        /// 程序数据文件夹路径（Roaming）
        /// </summary>
        public static string BaseFolder { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), App.AppName);

        /// <summary>
        /// 程序缓存数据文件夹路径（Local）
        /// </summary>
        public static string BaseLocalFolder { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), App.AppName);

        /// <summary>
        /// 数据文件夹路径
        /// </summary>
        public static string UserDataFolder { get; } = Path.Combine(BaseFolder, "UserData");

        /// <summary>
        /// 歌单数据文件路径
        /// </summary>
        public static string PlayListDataPath { get; } = Path.Combine(UserDataFolder, "PlayList");
        
        /// <summary>
        /// 本地音乐数据文件路径
        /// </summary>
        public static string LocalMusicDataPath { get; } = Path.Combine(UserDataFolder, "LocalMusic");
        
        /// <summary>
        /// 设置数据文件路径
        /// </summary>
        public static string SettingDataPath { get; } = Path.Combine(UserDataFolder, "Setting");

        /// <summary>
        /// 参数均衡器设置数据文件路径
        /// </summary>
        public static string AudioEffectDataPath { get; } = Path.Combine(UserDataFolder, "AudioEffect");

        /// <summary>
        /// 插件设置数据文件路径
        /// </summary>
        public static string PluginSettings{ get; } = Path.Combine(UserDataFolder, "PluginSettings");

        /// <summary>
        /// 历史记录数据文件路径
        /// </summary>
        public static string HistoryDataPath { get; } = Path.Combine(UserDataFolder, "History");
        
        /// <summary>
        /// 日志文件路径
        /// </summary>
        public static string LogDataPath { get; } = Path.Combine(UserDataFolder, "Log");
        
        /// <summary>
        /// 运行日志文件夹路径
        /// </summary>
        public static string RunLogFolder { get; } = Path.Combine(BaseLocalFolder, "Log");

        /// <summary>
        /// 更新程序路径
        /// </summary>
        public static string UpdateFolder { get; set; } = Path.Combine(BaseLocalFolder, "Update");

        public static string DefaultCacheFolder = Path.Combine(BaseLocalFolder, "Cache");
        private static string _cacheFolder = DefaultCacheFolder;
        /// <summary>
        /// 缓存文件夹路径
        /// </summary>
        public static string CacheFolder
        {
            get => _cacheFolder;
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    if (Path.Exists(value))
                    {
                        _cacheFolder = value;
                        InitCacheFolder();
                    }
                }
            }
        }

        /// <summary>
        /// 歌曲缓存文件夹路径
        /// </summary>
        public static string AudioCacheFolder { get; set; } = Path.Combine(CacheFolder, "Audio");

        /// <summary>
        /// 图片缓存文件夹路径
        /// </summary>
        public static string ImageCacheFolder { get; set; } = Path.Combine(CacheFolder, "Image");

        /// <summary>
        /// 歌词缓存文件夹路径
        /// </summary>
        public static string LyricCacheFolder { get; set; } = Path.Combine(CacheFolder, "Lyric");

        /// <summary>
        /// 下载文件夹路径
        /// </summary>
        public static string DownloadFolder { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
        
        /// <summary>
        /// 开机启动路径
        /// </summary>
        public static string StartupFolder { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs", "Startup");

        /// <summary>
        /// 开机启动快捷方式
        /// </summary>
        public static string StartupShortcutPath { get; set; } = Path.Combine(StartupFolder, $"{App.AppName}.lnk");

        /// <summary>
        /// 默认播放列表数据
        /// </summary>
        public static MusicListData PlayListDefault = new("default", "默认播放列表", Path.Combine(Environment.CurrentDirectory, "Images", "icon.png"), MusicFrom.localMusic, listDataType: DataType.本地歌单);

        /// <summary>
        /// 默认设置数据
        /// </summary>
        public static JObject SettingDefault = new()
        {
            { SettingParams.Volume.ToString(), 50f },
            { SettingParams.CacheFolderPath.ToString(), null},
            { SettingParams.DownloadFolderPath.ToString(), DownloadFolder },
            { SettingParams.AudioCacheFolderPath.ToString(), AudioCacheFolder },
            { SettingParams.ImageCacheFolderPath.ToString(), ImageCacheFolder },
            { SettingParams.LyricCacheFolderPath.ToString(), LyricCacheFolder },
            { SettingParams.DownloadOptions.ToString(), new JArray(){ true, false , true, true } },
            { SettingParams.DownloadNamedMethod.ToString(), (int)DownloadNamedMethod.t_ar_al },
            { SettingParams.DownloadQuality.ToString(), (int)DownloadQuality.lossless },
            { SettingParams.DownloadMaximum.ToString(), 3 },
            { SettingParams.PlayBehavior.ToString(), (int)PlayBehavior.循环播放 },
            { SettingParams.PlayPauseWhenPreviousPause.ToString(), false },
            { SettingParams.PlayNextWhenPlayError.ToString(), true },
            { SettingParams.EqualizerEnable.ToString(), false },
            { SettingParams.EqualizerString.ToString(), nameof(Media.AudioEqualizerBands.CustomBands) },
            { SettingParams.EqualizerCustomData.ToString(), "0,0,0,0,0,0,0,0,0,0" },
            { SettingParams.WasapiOnly.ToString(), false },
            { SettingParams.AudioLatency.ToString(), 120 },
            { SettingParams.MusicPageShowLyricPage.ToString(), true },
            { SettingParams.ThemeColorMode.ToString(), (int)ElementTheme.Default },
            { SettingParams.ThemeMusicPageColorMode.ToString(), (int)ElementTheme.Default },
            { SettingParams.ThemeAccentColor.ToString(), null },
            { SettingParams.ThemeBackdropEffect.ToString(), (int)MainWindow.BackdropType.Mica },
            { SettingParams.ThemeBackdropImagePath.ToString(), null },
            { SettingParams.ThemeBackdropImageMassOpacity.ToString(), 0.5 },
            { 
                SettingParams.DesktopLyricOptions.ToString(),
                new JArray() {
                    true, false, false, true
                }
            },
            { 
                SettingParams.DesktopLyricText.ToString(),
                new JArray() {
                    (int)LyricTextBehavior.Exchange,
                    (int)LyricTextPosition.Default
                }
            },
            { 
                SettingParams.DesktopLyricTranslateText.ToString(),
                new JArray() {
                    (int)LyricTranslateTextBehavior.MainLyric,
                    (int)LyricTranslateTextPosition.Center
                }
            },
            { SettingParams.DesktopLyricOpacity.ToString(), 1 },
            { SettingParams.TaskbarShowIcon.ToString(), true },
            { SettingParams.BackgroundRun.ToString(), false },
            { SettingParams.ImageDarkMass.ToString(), false },
            { SettingParams.LoadLastExitPlayingSongAndSongList.ToString(), true },
            { SettingParams.TopNavigationStyle.ToString(), false },
            { SettingParams.LocalMusicPageItemSortBy.ToString(), 0 },
            { SettingParams.HotKeyEnable.ToString(), false },
            { SettingParams.HotKeySettings.ToString(), JArray.FromObject(HotKeyManager.DefaultRegisterHotKeysList) },
            { SettingParams.StartupWithWindows.ToString(), false },
            { SettingParams.UseRomajiLyric.ToString(), true },
        };
        
        /// <summary>
        /// 默认历史记录数据
        /// </summary>
        public static JObject HistoryDefault = new()
        {
            { "Songs", new JObject() },
            { "Search", new JObject() }
        };

        public static JObject AudioEffectDefault = new()
        {
            { AudioEffectFlag.GraphicEqEnable.ToString(), false },
            { AudioEffectFlag.ParametricEqEnable.ToString(), false },
            { AudioEffectFlag.PassFilterEqEnable.ToString(), false },
            { AudioEffectFlag.EffectEnable.ToString(), false },
            { AudioEffectFlag.WasapiOnlyEnable.ToString(), false },
            { AudioEffectFlag.Latency.ToString(), 200 },
            { AudioEffectFlag.AudioEffectDatas.ToString(), new JArray() { 1.0, 1.0, 1.0 } },
            { AudioEffectFlag.GraphicEqString.ToString(), nameof(Media.AudioEqualizerBands.CustomBands) },
            { AudioEffectFlag.GraphicEqDatas.ToString(), "0,0,0,0,0,0,0,0,0,0" },
            { AudioEffectFlag.ParametricEqDatas.ToString(), new JArray() },
            { AudioEffectFlag.PassFilterEqDatas.ToString(), new JArray() },
        };

        public enum DownloadNamedMethod
        {
            t_ar_al = 0,
            t_ar,
            t_al_ar,
            t_al,
            t
        }
        
        public enum DownloadQuality
        {
            lossless = 960, lossy_high = 320, lossy_mid = 192, lossy_low = 128
        }

        /// <summary>
        /// 设置标志
        /// </summary>
        public enum SettingParams { 
            Volume,
            CacheFolderPath,
            AudioCacheFolderPath,
            DownloadFolderPath,
            ImageCacheFolderPath,
            LyricCacheFolderPath,
            DownloadOptions,
            DownloadNamedMethod,
            DownloadQuality,
            DownloadMaximum,
            PlayBehavior,
            PlayPauseWhenPreviousPause,
            PlayNextWhenPlayError,
            EqualizerEnable,
            EqualizerString,
            EqualizerCustomData,
            WasapiOnly,
            AudioLatency,
            MusicPageShowLyricPage,
            ThemeColorMode,
            ThemeMusicPageColorMode,
            ThemeAccentColor,
            ThemeBackdropEffect,
            ThemeBackdropImagePath,
            ThemeBackdropImageMassOpacity,
            DesktopLyricOptions,
            DesktopLyricText,
            DesktopLyricTranslateText,
            DesktopLyricOpacity,
            TaskbarShowIcon,
            BackgroundRun,
            ImageDarkMass,
            LoadLastExitPlayingSongAndSongList,
            TopNavigationStyle,
            LocalMusicPageItemSortBy,
            HotKeyEnable,
            HotKeySettings,
            StartupWithWindows,
            UseRomajiLyric
        }

        public enum AudioEffectFlag
        {
            GraphicEqEnable,
            ParametricEqEnable,
            PassFilterEqEnable,
            EffectEnable,
            WasapiOnlyEnable,
            Latency,
            AudioEffectDatas,
            GraphicEqString,
            GraphicEqDatas,
            ParametricEqDatas, 
            PassFilterEqDatas,
        }

        public enum LocalMusicDataType { LocalMusicFolderPath, AnalyzedDatas }

        /// <summary>
        /// 初始化所有文件夹和文件
        /// </summary>
        public static void InitFiles()
        {
            App.logManager.Log("DataFolderBase", "初始化文件目录中...");
            Directory.CreateDirectory(PluginFolder);
            Directory.CreateDirectory(BaseFolder);
            Directory.CreateDirectory(BaseLocalFolder);
            Directory.CreateDirectory(UserDataFolder);
            Directory.CreateDirectory(RunLogFolder);
            InitCacheFolder();
            Directory.CreateDirectory(UpdateFolder);

            if (!File.Exists(PlayListDataPath))
            {
                File.Create(PlayListDataPath).Close();
                File.WriteAllText(PlayListDataPath, "{}");
                PlayListHelper.AddPlayList(PlayListDefault);
            }
            
            if (!File.Exists(LocalMusicDataPath))
            {
                File.Create(LocalMusicDataPath).Close();
                File.WriteAllText(LocalMusicDataPath,
                    new JObject()
                    {
                        { LocalMusicDataType.LocalMusicFolderPath.ToString(), new JArray() { Environment.GetFolderPath(Environment.SpecialFolder.MyMusic) } },
                        { LocalMusicDataType.AnalyzedDatas.ToString(), new JArray() }
                    }.ToString());
            }
            
            if (!File.Exists(SettingDataPath))
            {
                File.Create(SettingDataPath).Close();
                File.WriteAllText(SettingDataPath, SettingDefault.ToString());
            }
            
            if (!File.Exists(AudioEffectDataPath))
            {
                File.Create(AudioEffectDataPath).Close();
                File.WriteAllText(AudioEffectDataPath, AudioEffectDefault.ToString());
            }
            
            if (!File.Exists(PluginSettings))
            {
                File.Create(PluginSettings).Close();
                File.WriteAllText(PluginSettings, JObject.FromObject(PluginManager.PluginInfoSettings).ToString());
            }
            
            if (!File.Exists(HistoryDataPath))
            {
                File.Create(HistoryDataPath).Close();
                File.WriteAllText(HistoryDataPath, HistoryDefault.ToString());
            }

            if (!File.Exists(LogDataPath))
            {
                File.Create(LogDataPath).Close();
            }
            
            Directory.CreateDirectory(StartupFolder);
            App.logManager.Log("DataFolderBase", "初始化文件目录完成。");
        }

        public static void InitCacheFolder()
        {
            Directory.CreateDirectory(CacheFolder);
            AudioCacheFolder = Path.Combine(CacheFolder, "Audio");
            ImageCacheFolder = Path.Combine(CacheFolder, "Image");
            LyricCacheFolder = Path.Combine(CacheFolder, "Lyric");
            Directory.CreateDirectory(AudioCacheFolder);
            Directory.CreateDirectory(ImageCacheFolder);
            Directory.CreateDirectory(LyricCacheFolder);
        }

        /// <summary>
        /// <list type="table">
        ///     <item>数据文件的实例。</item>
        ///     <item>使用时会读取设置文件，设置时会写入数据文件</item>
        /// </list>
        /// </summary>
        public static JObject JSettingData
        {
            get => JObject.Parse(File.ReadAllText(SettingDataPath));
            set
            {
                File.WriteAllText(SettingDataPath, value.ToString());
            }
        }

        /// <summary>
        /// <list type="table">
        ///     <item>音效数据文件的实例。</item>
        ///     <item>使用时会读取设置文件，设置时会写入数据文件</item>
        /// </list>
        /// </summary>
        public static JObject JAudioEffectData
        {
            get => JObject.Parse(File.ReadAllText(AudioEffectDataPath));
            set
            {
                File.WriteAllText(AudioEffectDataPath, value.ToString());
            }
        }

        /// <summary>
        /// <list type="table">
        ///     <item>插件数据文件的实例。</item>
        ///     <item>使用时会读取设置文件，设置时会写入数据文件</item>
        /// </list>
        /// </summary>
        public static JObject PluginSettingsData
        {
            get => JObject.Parse(File.ReadAllText(PluginSettings));
            set
            {
                File.WriteAllText(PluginSettings, value.ToString());
            }
        }
    }

    public static class JsonNewtonsoft
    {
        /// <summary>
        /// 把对象转换为JSON字符串
        /// </summary>
        /// <param name="o">对象</param>
        /// <returns>JSON字符串</returns>
        public static string ToJSON(this object o)
        {
            if (o is null)
            {
                return null;
            }
            return JsonConvert.SerializeObject(o);
        }
        /// <summary>
        /// 把Json文本转为实体
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        /// <returns></returns>
        public static T FromJSON<T>(this string input)
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(input);
            }
            catch (Exception ex)
            {
                return default(T);
            }
        }
    }
}
